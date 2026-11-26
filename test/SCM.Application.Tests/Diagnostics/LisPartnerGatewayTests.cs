using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SCM.Application.Billing;
using SCM.Application.Contracts.Billing.Dtos;
using SCM.Application.Contracts.Diagnostics.Dtos;
using SCM.Application.Diagnostics;
using SCM.Application.Diagnostics.Gateway;
using SCM.Domain.Branches;
using SCM.Domain.Clinical;
using SCM.Domain.Diagnostics;
using SCM.Domain.Diagnostics.Gateway;
using SCM.Domain.Phlebotomy;
using SCM.Domain.Reception;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;
using SCM.EntityFrameworkCore;
using Xunit;

namespace SCM.Application.Tests.Diagnostics;

public class LisPartnerGatewayTests
{
    private sealed class MockLabconnSqlClient : ILabconnStagingSqlClient
    {
        public List<LabconnOrderRecord> PushedOrders { get; } = new();
        public List<(string OrderCode, string TestCodeHis)> DeletedOrders { get; } = new();
        public List<LabconnUploadedResultRow> RowsToReturn { get; } = new();

        public Task ExecuteUpdOrderAsync(string connectionString, LabconnOrderRecord order, CancellationToken ct = default)
        {
            PushedOrders.Add(order);
            return Task.CompletedTask;
        }

        public Task ExecuteDelOrderAsync(string connectionString, string orderCode, string testCodeHis, CancellationToken ct = default)
        {
            DeletedOrders.Add((orderCode, testCodeHis));
            return Task.CompletedTask;
        }

        public Task<List<LabconnUploadedResultRow>> QueryResultUploadAsync(string connectionString, DateTime sinceUtc, CancellationToken ct = default)
            => Task.FromResult(RowsToReturn);
    }

    private static SCMDbContext CreateContext(string dbName, ICurrentBranch? branch = null)
    {
        var options = new DbContextOptionsBuilder<SCMDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new SCMDbContext(options, branch);
    }

    [Fact]
    public async Task GW01_Paid_LabOrder_Enqueues_Outbox_And_Calls_Labconn_SpUpdOrder()
    {
        var dbName = Guid.NewGuid().ToString();
        var branchId = Guid.NewGuid();
        var branch = new CurrentBranch(branchId, "CNQ1");
        using var db = CreateContext(dbName, branch);

        var partner = new LisPartnerConfig("LABCONN_SQL", "Labconn HIS_LIS", LisProviderType.LabconnStagingDb, "Server=sql;Database=HIS_LIS;", isDefaultForBranch: true, branchId: branchId, branchCode: "CNQ1");
        db.LisPartnerConfigs.Add(partner);
        db.ExternalServiceMappings.Add(new ExternalServiceMapping(partner.Id, "XN-CTM", "CBC01", "CBC01", "EDTA", "G/L", 1m, branchId, "CNQ1"));

        var patient = new Patient("BN-GW1", "Lê Văn C", new DateTime(1991, 4, 4), Gender.Male, "079091001122", "0901112233");
        var enc = new Encounter("ENC-GW-01", branchId, "CNQ1", patient.Id, WorkflowRoute.Route1_FastTrack);
        var order = new MedicalOrder(enc.Id, enc.EncounterCode, patient.Id, patient.FullName, Guid.NewGuid(), "BS. An", MedicalOrderType.Laboratory, isInPackage: false, branchId: branchId, branchCode: "CNQ1")
        {
            OrderCode = "ORD-LAB-20"
        };
        order.AddItem(Guid.NewGuid(), "XN-CTM", "Tổng phân tích tế bào máu", 120000m, 1);

        db.Patients.Add(patient);
        db.Encounters.Add(enc);
        db.MedicalOrders.Add(order);
        await db.SaveChangesAsync();

        var mockSql = new MockLabconnSqlClient();
        var labService = new LabIntegrationAppService(db, branch);
        var gateway = new LisPartnerGatewayAppService(db, labService, branch, new ILisProviderAdapter[]
        {
            new LabconnStagingDbAdapter(mockSql),
            new PartnerRestWebhookAdapter(),
            new DirectDeviceBridgeAdapter()
        });
        var billing = new BillingAppService(db, branch, gateway);

        var receipt = await billing.CreateSelectiveReceiptAsync(new CreateSelectiveReceiptDto
        {
            EncounterId = enc.Id,
            EncounterCode = enc.EncounterCode,
            PatientId = patient.Id,
            PatientName = patient.FullName,
            PaymentMethod = 1,
            CashierId = Guid.NewGuid(),
            CashierName = "Thu ngân 1",
            SelectedItems = new List<SelectiveReceiptItemRequestDto>
            {
                new()
                {
                    ItemType = ReceiptItemType.ParaclinicalService,
                    MedicalOrderId = order.Id,
                    MedicalOrderItemId = order.Items[0].Id,
                    ItemCode = "XN-CTM",
                    ItemName = "Tổng phân tích tế bào máu",
                    UnitPrice = 120000m,
                    Quantity = 1
                }
            }
        });

        await billing.ConfirmPaymentAsync(receipt.Id, "TX-CASH-01");

        var outbox = await db.LisIntegrationOutboxes.FirstOrDefaultAsync(o => o.MedicalOrderItemId == order.Items[0].Id);
        Assert.NotNull(outbox);
        Assert.Equal(LisOutboxStatus.Pending, outbox!.Status);

        // Idempotent enqueue call must not duplicate
        await gateway.EnqueueOrderItemsAsync(order.Id);
        Assert.Equal(1, await db.LisIntegrationOutboxes.CountAsync());

        var synced = await gateway.ProcessPendingOutboxAsync();
        Assert.Equal(1, synced);
        Assert.Single(mockSql.PushedOrders);
        Assert.Equal("CBC01", mockSql.PushedOrders[0].TestCodeHIS);

        var refreshed = await db.LisIntegrationOutboxes.FirstAsync(o => o.Id == outbox.Id);
        Assert.Equal(LisOutboxStatus.Synced, refreshed.Status);
    }

    [Fact]
    public async Task INV_GW_01_CancelOrderItem_Blocked_When_Sample_Already_Collected()
    {
        var dbName = Guid.NewGuid().ToString();
        var branchId = Guid.NewGuid();
        var branch = new CurrentBranch(branchId, "CNQ1");
        using var db = CreateContext(dbName, branch);

        var enc = new Encounter("ENC-GW-CANCEL", branchId, "CNQ1", Guid.NewGuid(), WorkflowRoute.Route1_FastTrack);
        var order = new MedicalOrder(enc.Id, enc.EncounterCode, enc.PatientId, "BN Cancel", Guid.NewGuid(), "BS. An", MedicalOrderType.Laboratory, true, branchId: branchId, branchCode: "CNQ1");
        order.AddItem(Guid.NewGuid(), "XN-CTM", "Tổng phân tích máu", 120000m, 1);

        var tube = SpecimenTube.CreateForOutpatient(enc.Id, enc.EncounterCode, order.Id, order.Items[0].Id, "CNQ1-LAB-CAN-E", TubeType.EDTA, "Tím", "XN-CTM", branchId, "CNQ1", TubeStatus.Printed);
        tube.MarkCollected("ĐD. Hoa");

        db.Encounters.Add(enc);
        db.MedicalOrders.Add(order);
        db.SpecimenTubes.Add(tube);
        await db.SaveChangesAsync();

        var gateway = new LisPartnerGatewayAppService(db, new LabIntegrationAppService(db, branch), branch);
        var ex = await Assert.ThrowsAsync<BusinessException>(() => gateway.CancelOrderItemAsync(order.Items[0].Id));
        Assert.Equal(SCMErrorCodes.LisGatewayCancelBlocked, ex.Code);
    }

    [Fact]
    public async Task GW02_And_GW03_PartnerWebhook_Verifies_Hmac_And_Blocks_Invalid_Signature()
    {
        var dbName = Guid.NewGuid().ToString();
        var branchId = Guid.NewGuid();
        var branch = new CurrentBranch(branchId, "CNQ1");
        using var db = CreateContext(dbName, branch);

        var partner = new LisPartnerConfig("MEDLATEC_API", "Medlatec Lab", LisProviderType.PartnerRestWebhook, "{\"webhookSecret\":\"SECRET_MED_2026\"}", false, branchId, "CNQ1");
        db.LisPartnerConfigs.Add(partner);
        db.ExternalServiceMappings.Add(new ExternalServiceMapping(partner.Id, "XN-GEN-NIPTS", "NIPT", "PARTNER_NIPT", "EDTA", "Qualitative", 1m, branchId, "CNQ1"));

        var enc = new Encounter("ENC-GW-02", branchId, "CNQ1", Guid.NewGuid(), WorkflowRoute.Route1_FastTrack);
        var tube = SpecimenTube.CreateForOutpatient(enc.Id, enc.EncounterCode, Guid.NewGuid(), null, "CNQ1-NIPT-01", TubeType.EDTA, "Tím", "XN-GEN-NIPTS", branchId, "CNQ1", TubeStatus.Collected);
        tube.Receive();
        db.Encounters.Add(enc);
        db.SpecimenTubes.Add(tube);
        await db.SaveChangesAsync();

        var gateway = new LisPartnerGatewayAppService(db, new LabIntegrationAppService(db, branch), branch);
        var payload = new PartnerWebhookPayloadDto
        {
            TubeBarcode = tube.TubeBarcode,
            Nonce = "NONCE-001",
            Results = new List<PartnerWebhookResultItemDto>
            {
                new() { PartnerTestCode = "PARTNER_NIPT", TestName = "Sàng lọc NIPT", StringValue = "Nguy cơ thấp", AbnormalFlag = "N" }
            }
        };
        var rawBody = JsonSerializer.Serialize(payload);

        // GW-03: Invalid signature -> UnauthorizedAccessException + AuditLog
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            gateway.HandlePartnerWebhookAsync("MEDLATEC_API", rawBody, "INVALID_SIG", payload));
        Assert.True(await db.DiagnosticAuditLogs.AnyAsync(a => a.Action == DiagnosticAuditAction.WebhookSignatureInvalid));
        Assert.Equal(0, await db.LabResultRecords.CountAsync());

        // GW-02: Valid HMAC-SHA256 -> Saves PendingReview result
        var validSig = PartnerRestWebhookAdapter.ComputeHmacSha256("SECRET_MED_2026", rawBody);
        var saved = await gateway.HandlePartnerWebhookAsync("MEDLATEC_API", rawBody, validSig, payload);
        var item = Assert.Single(saved);
        Assert.Equal("XN-GEN-NIPTS", item.TestCode);
        Assert.Equal(LabReviewStatus.PendingReview, item.ReviewStatus);
    }

    [Fact]
    public async Task GW04_Orthanc_OnStableStudy_Webhook_Transitions_Imaging_To_Captured()
    {
        var dbName = Guid.NewGuid().ToString();
        var branchId = Guid.NewGuid();
        var branch = new CurrentBranch(branchId, "CNQ1");
        using var db = CreateContext(dbName, branch);

        var enc = new Encounter("ENC-XQ-88", branchId, "CNQ1", Guid.NewGuid(), WorkflowRoute.Route1_FastTrack);
        var rec = new DiagnosticImagingRecord(enc.Id, enc.EncounterCode, ImagingModality.DR, "XQ-NGUC", "X-Quang ngực thẳng", string.Empty, branchId: branchId, branchCode: "CNQ1");
        rec.PushWorklist("ACC-2026-088", "P.XQ01");

        db.Encounters.Add(enc);
        db.DiagnosticImagingRecords.Add(rec);
        await db.SaveChangesAsync();

        var imagingService = new DiagnosticImagingAppService(db, branch);
        var updated = await imagingService.HandlePacsStableStudyWebhookAsync(new PacsStableStudyWebhookDto
        {
            AccessionNumber = "ACC-2026-088",
            StudyInstanceUid = "1.2.840.10008.8888",
            Modality = "DX"
        });

        Assert.NotNull(updated);
        Assert.Equal(ImagingWorkflowStatus.Captured, updated!.WorkflowStatus);
        Assert.Equal("1.2.840.10008.8888", updated.StudyInstanceUid);
        Assert.Contains("1.2.840.10008.8888", updated.DicomWebUrl);
    }
}
