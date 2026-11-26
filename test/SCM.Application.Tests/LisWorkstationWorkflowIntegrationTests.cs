using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SCM.Application.Contracts.Diagnostics;
using SCM.Application.Contracts.Diagnostics.Dtos;
using SCM.Application.Diagnostics;
using SCM.Application.Outbox;
using SCM.Domain.Billing.Exceptions;
using SCM.Domain.Branches;
using SCM.Domain.Clinical;
using SCM.Domain.Diagnostics;
using SCM.Domain.Diagnostics.Exceptions;
using SCM.Domain.Grading;
using SCM.Domain.MasterData;
using SCM.Domain.Phlebotomy;
using SCM.Domain.Reception;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.EntityFrameworkCore;
using Xunit;

namespace SCM.Application.Tests;

public class LisWorkstationWorkflowIntegrationTests
{
    private sealed class FakeDiagnosticsNotifier : IDiagnosticsRealtimeNotifier
    {
        public List<PanicAlertEventDto> PanicAlerts { get; } = new();
        public List<(string Branch, string Type, Guid? EncounterId)> Updates { get; } = new();

        public Task NotifyPanicAlertAsync(PanicAlertEventDto alert)
        {
            PanicAlerts.Add(alert);
            return Task.CompletedTask;
        }

        public Task NotifyWorklistUpdatedAsync(string branchCode, string workstationType, Guid? encounterId = null)
        {
            Updates.Add((branchCode, workstationType, encounterId));
            return Task.CompletedTask;
        }
    }

    private static SCMDbContext CreateContext(string dbName, ICurrentBranch? branch = null)
    {
        var options = new DbContextOptionsBuilder<SCMDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new SCMDbContext(options, branch);
    }

    [Fact]
    public async Task HP01_And_INV_LIS_04_FiveStep_Lis_Workflow_And_M3_Signed_Filter()
    {
        var dbName = Guid.NewGuid().ToString();
        var branchId = Guid.NewGuid();
        var branch = new CurrentBranch(branchId, "CNQ1");
        using var db = CreateContext(dbName, branch);
        var notifier = new FakeDiagnosticsNotifier();

        var patient = new Patient("BN-101", "Nguyễn Văn A", new DateTime(1990, 1, 1), Gender.Male, "079090001234", "0901234567");
        var enc = new Encounter("ENC-2026-101", branchId, "CNQ1", patient.Id, WorkflowRoute.Route1_FastTrack);
        var order = new MedicalOrder(enc.Id, enc.EncounterCode, patient.Id, patient.FullName, Guid.NewGuid(), "BS. An", MedicalOrderType.Laboratory, isInPackage: false, branchId: branchId, branchCode: "CNQ1");
        order.AddItem(Guid.NewGuid(), "XN-GLUCOSE", "Định lượng Glucose", 60000m, 1);
        order.Items[0].MarkPaid(Guid.NewGuid());
        order.UpdatePaymentStatusFromItems();

        db.Patients.Add(patient);
        db.Encounters.Add(enc);
        db.MedicalOrders.Add(order);
        await db.SaveChangesAsync();

        var service = new LabIntegrationAppService(db, branch, realtimeNotifier: notifier);

        // Step 2: Generate & Collect NaF tube
        var tubes = await service.GenerateAndCollectOutpatientTubesAsync(new CollectLabSamplesDto
        {
            MedicalOrderId = order.Id,
            MarkCollectedImmediately = true,
            CollectorName = "ĐD. Hoa"
        });
        var tube = Assert.Single(tubes);
        Assert.Equal(TubeType.NaF, tube.TubeType);
        Assert.Equal(TubeStatus.Collected, tube.Status);

        // Receive at Lab
        var received = await service.ReceiveSpecimenTubeAsync(new ReceiveTubeDto { TubeBarcode = tube.TubeBarcode });
        Assert.Equal(TubeStatus.Received, received.Status);

        // Step 3: Ingest ASTM E1394 message (126 mg/dL -> 7.00 mmol/L, M3 = 18)
        var astm = $"H|\\^&|||Cobas6000\nO|1|{tube.TubeBarcode}||^^^GLU\nR|1|^^^GLU|126.0|mg/dL|70-110|H||F\nL|1|N";
        var ingested = await service.IngestBridgeMessageAsync(new IngestBridgeMessageDto { Protocol = "ASTM", RawMessage = astm });

        var rec = Assert.Single(ingested);
        Assert.Equal(enc.Id, rec.EncounterId); // Bug tube.Id fixed!
        Assert.Equal(7.00m, rec.NumericResult);
        Assert.Equal("mmol/L", rec.Unit);
        Assert.Equal(18, rec.M3FieldMapping);
        Assert.True(rec.IsAbnormal);
        Assert.Equal(LabReviewStatus.PendingReview, rec.ReviewStatus);

        // INV-LIS-04: PendingReview must NOT appear in M3 Payload
        var summary = new MedicalRecordSummary(enc.Id, enc.EncounterCode, patient.Id, patient.FullName, 1, "Loại I", 5172, "OK", "");
        var pendingEntityList = await db.LabResultRecords.Where(r => r.EncounterId == enc.Id).ToListAsync();
        var m3Before = NationalM3PayloadBuilder.BuildPayloadJson("79001", "079090001234", patient.FullName, patient.BirthDate, 1, "", null, null, null, pendingEntityList, null, summary);
        using (var docBefore = JsonDocument.Parse(m3Before))
        {
            Assert.Empty(docBefore.RootElement.GetProperty("can_lam_sang_35").EnumerateArray());
        }

        // Step 4: Level 1 Tech Verify -> Level 2 Doctor Sign
        var techVerified = await service.VerifyLevel1ByTechnicianAsync(new TechVerifyLabDto { LabResultId = rec.Id, TechnicianName = "KTV. Tuấn" });
        Assert.Equal(LabReviewStatus.TechVerified, techVerified.ReviewStatus);

        var signed = await service.ApproveLevel2AndSignAsync(new DoctorSignLabDto
        {
            LabResultId = rec.Id,
            DoctorId = Guid.NewGuid(),
            DoctorName = "BS.CKI Phạm Thanh Hòa",
            DoctorLicenseNo = "005891/HCM-CCHN"
        });
        Assert.Equal(LabReviewStatus.DoctorSigned, signed.ReviewStatus);
        Assert.True(signed.IsLocked);

        // INV-LIS-04: DoctorSigned now appears in M3 Payload
        var signedEntityList = await db.LabResultRecords.Where(r => r.EncounterId == enc.Id).ToListAsync();
        var m3After = NationalM3PayloadBuilder.BuildPayloadJson("79001", "079090001234", patient.FullName, patient.BirthDate, 1, "", null, null, null, signedEntityList, null, summary);
        using (var docAfter = JsonDocument.Parse(m3After))
        {
            var cls = docAfter.RootElement.GetProperty("can_lam_sang_35").EnumerateArray().ToList();
            Assert.Single(cls);
            Assert.Equal(18, cls[0].GetProperty("m3_index").GetInt32());
        }
    }

    [Fact]
    public async Task HP02_And_HL7_PanicThreshold_Triggers_Realtime_Alert_And_Requires_Ack()
    {
        var dbName = Guid.NewGuid().ToString();
        var branchId = Guid.NewGuid();
        var branch = new CurrentBranch(branchId, "CNQ1");
        using var db = CreateContext(dbName, branch);
        var notifier = new FakeDiagnosticsNotifier();

        db.LabPanicThresholds.Add(new LabPanicThreshold("KALI", "Kali máu", "mmol/L", 2.5m, 6.5m, 0, branchId, "CNQ1"));
        var patient = new Patient("BN-102", "Nguyễn Văn B", new DateTime(1985, 5, 5), Gender.Male, "079085009999", "0909888777");
        var enc = new Encounter("ENC-2026-102", branchId, "CNQ1", patient.Id, WorkflowRoute.Route1_FastTrack);
        var tube = SpecimenTube.CreateForOutpatient(enc.Id, enc.EncounterCode, Guid.NewGuid(), null, "CNQ1-LAB-102-S", TubeType.Serum, "Vàng", "KALI, CRE", branchId, "CNQ1", TubeStatus.Collected);
        tube.Receive();

        db.Patients.Add(patient);
        db.Encounters.Add(enc);
        db.SpecimenTubes.Add(tube);
        await db.SaveChangesAsync();

        var service = new LabIntegrationAppService(db, branch, realtimeNotifier: notifier);

        var hl7 = $"MSH|^~\\&|AU480|LAB|SCM|CNQ1|20261125||ORU^R01|MSG01|P|2.5\nOBR|1|{tube.TubeBarcode}||\nOBX|1|NM|KALI^Kali mau||6.9|mmol/L|3.5-5.1|HH|||F\nOBX|2|NM|CRE^Creatinin||1.5|mg/dL|0.6-1.3|H|||F";
        var res = await service.IngestBridgeMessageAsync(new IngestBridgeMessageDto { Protocol = "HL7", RawMessage = hl7 });

        var kali = res.First(r => r.TestCode == "KALI");
        var cre = res.First(r => r.TestCode == "CRE");
        Assert.True(kali.IsPanic);
        Assert.Equal(LisResultStatus.Panic, kali.Status);
        Assert.Equal(132.60m, cre.NumericResult); // 1.5 mg/dL * 88.4 = 132.60 umol/L
        Assert.Equal(20, cre.M3FieldMapping);

        // SignalR Panic Alert pushed!
        Assert.Single(notifier.PanicAlerts);
        Assert.Equal("KALI", notifier.PanicAlerts[0].TestCode);

        // Level 1 verify, then Level 2 blocked until PanicAck
        await service.VerifyLevel1ByTechnicianAsync(new TechVerifyLabDto { LabResultId = kali.Id });
        await Assert.ThrowsAsync<PanicValueUnacknowledgedException>(() =>
            service.ApproveLevel2AndSignAsync(new DoctorSignLabDto { LabResultId = kali.Id, DoctorId = Guid.NewGuid(), DoctorName = "BS. Hòa", DoctorLicenseNo = "005891/HCM-CCHN" }));

        await service.AcknowledgePanicValueAsync(new PanicAckDto { LabResultId = kali.Id, Note = "Đã gọi báo BS.An lúc 09:15" });
        var signed = await service.ApproveLevel2AndSignAsync(new DoctorSignLabDto { LabResultId = kali.Id, DoctorId = Guid.NewGuid(), DoctorName = "BS. Hòa", DoctorLicenseNo = "005891/HCM-CCHN" });
        Assert.Equal(LabReviewStatus.DoctorSigned, signed.ReviewStatus);
    }

    [Fact]
    public async Task UN01_CC01_CC02_CC04_SEC04_Unpaid_Idempotency_Concurrency_Lock_And_PiiMasking()
    {
        var dbName = Guid.NewGuid().ToString();
        var branchId = Guid.NewGuid();
        var branch = new CurrentBranch(branchId, "CNQ1");
        using var db = CreateContext(dbName, branch);

        var patient = new Patient("BN-200", "Trần Thị Minh", new DateTime(1992, 3, 12), Gender.Female, "079090001234", "0903334445");
        var enc = new Encounter("ENC-2026-200", branchId, "CNQ1", patient.Id, WorkflowRoute.Route1_FastTrack);

        // Unpaid Order -> UN-01
        var unpaidOrder = new MedicalOrder(enc.Id, enc.EncounterCode, patient.Id, patient.FullName, Guid.NewGuid(), "BS. An", MedicalOrderType.Laboratory, isInPackage: false, branchId: branchId, branchCode: "CNQ1");
        unpaidOrder.AddItem(Guid.NewGuid(), "XN-UREA", "Ure máu", 50000m, 1);

        // Paid Order -> CC-01, CC-02, CC-04, SEC-04
        var paidOrder = new MedicalOrder(enc.Id, enc.EncounterCode, patient.Id, patient.FullName, Guid.NewGuid(), "BS. An", MedicalOrderType.Laboratory, isInPackage: false, branchId: branchId, branchCode: "CNQ1");
        paidOrder.AddItem(Guid.NewGuid(), "XN-GLUCOSE", "Glucose", 60000m, 1);
        paidOrder.Items[0].MarkPaid(Guid.NewGuid());
        paidOrder.UpdatePaymentStatusFromItems();

        db.Patients.Add(patient);
        db.Encounters.Add(enc);
        db.MedicalOrders.AddRange(unpaidOrder, paidOrder);
        await db.SaveChangesAsync();

        var service = new LabIntegrationAppService(db, branch);

        // UN-01: Blocked unpaid sample collection
        await Assert.ThrowsAsync<UnpaidServiceExecutionBlockedException>(() =>
            service.GenerateAndCollectOutpatientTubesAsync(new CollectLabSamplesDto { MedicalOrderId = unpaidOrder.Id }));

        // Paid tube + ASTM ingestion
        var tubes = await service.GenerateAndCollectOutpatientTubesAsync(new CollectLabSamplesDto { MedicalOrderId = paidOrder.Id, MarkCollectedImmediately = true });
        var barcode = tubes[0].TubeBarcode;
        await service.ReceiveSpecimenTubeAsync(new ReceiveTubeDto { TubeBarcode = barcode });

        var astm = $"O|1|{barcode}||^^^GLU\nR|1|^^^GLU|5.5|mmol/L|3.9-6.4|N||F";
        var first = await service.IngestBridgeMessageAsync(new IngestBridgeMessageDto { Protocol = "ASTM", RawMessage = astm });
        var second = await service.IngestBridgeMessageAsync(new IngestBridgeMessageDto { Protocol = "ASTM", RawMessage = astm });

        // CC-01: No duplicate LabResultRecord created
        Assert.Equal(first[0].Id, second[0].Id);
        Assert.Equal(1, await db.LabResultRecords.CountAsync(r => r.EncounterId == enc.Id));

        // CC-02: Optimistic concurrency conflict when tech modifies while doctor holds stale version
        var recId = first[0].Id;
        var initialVersion = first[0].Version;
        var verified = await service.VerifyLevel1ByTechnicianAsync(new TechVerifyLabDto { LabResultId = recId, CorrectedNumericResult = 5.8m });
        Assert.True(verified.Version > initialVersion);

        await Assert.ThrowsAsync<WorkstationConcurrencyConflictException>(() =>
            service.ApproveLevel2AndSignAsync(new DoctorSignLabDto { LabResultId = recId, ExpectedVersion = initialVersion, DoctorId = Guid.NewGuid(), DoctorName = "BS. Hòa", DoctorLicenseNo = "005891/HCM-CCHN" }));

        // Sign with current version -> Locked
        var signed = await service.ApproveLevel2AndSignAsync(new DoctorSignLabDto { LabResultId = recId, ExpectedVersion = verified.Version, DoctorId = Guid.NewGuid(), DoctorName = "BS. Hòa", DoctorLicenseNo = "005891/HCM-CCHN" });
        Assert.True(signed.IsLocked);

        // CC-04: Late bridge message does NOT overwrite locked value 5.8
        var astmLate = $"O|1|{barcode}||^^^GLU\nR|1|^^^GLU|9.9|mmol/L|3.9-6.4|H||F";
        await service.IngestBridgeMessageAsync(new IngestBridgeMessageDto { Protocol = "ASTM", RawMessage = astmLate });
        var afterLate = await db.LabResultRecords.FirstAsync(r => r.Id == recId);
        Assert.Equal(5.8m, afterLate.NumericResult);

        // SEC-04: PII Masked in DiagnosticAuditLog (079090001234 -> 079******234, no plain CCCD)
        var audits = await service.GetAuditLogsAsync(enc.Id);
        Assert.Contains(audits, a => a.Action == DiagnosticAuditAction.ResultOverwriteBlocked);
        Assert.All(audits, a =>
        {
            Assert.DoesNotContain("079090001234", a.MaskedPiiSummary);
            Assert.Contains("079******234", a.MaskedPiiSummary);
        });
    }

    [Fact]
    public async Task SEC01_CrossBranch_Isolation_Filters_Out_Other_Branch_Data()
    {
        var dbName = Guid.NewGuid().ToString();
        var branch1Id = Guid.NewGuid();
        var branch3Id = Guid.NewGuid();

        using (var seedDb = CreateContext(dbName, new CurrentBranch(branch3Id, "CNQ3")))
        {
            var enc3 = new Encounter("ENC-CNQ3-01", branch3Id, "CNQ3", Guid.NewGuid(), WorkflowRoute.Route1_FastTrack);
            var order3 = new MedicalOrder(enc3.Id, enc3.EncounterCode, enc3.PatientId, "BN CNQ3", Guid.NewGuid(), "BS Q3", MedicalOrderType.Laboratory, true, branchId: branch3Id, branchCode: "CNQ3");
            order3.AddItem(Guid.NewGuid(), "XN-GLUCOSE", "Glucose", 60000m);
            seedDb.Encounters.Add(enc3);
            seedDb.MedicalOrders.Add(order3);
            await seedDb.SaveChangesAsync();
        }

        using var dbQ1 = CreateContext(dbName, new CurrentBranch(branch1Id, "CNQ1"));
        var serviceQ1 = new LabIntegrationAppService(dbQ1, new CurrentBranch(branch1Id, "CNQ1"));
        var worklistQ1 = await serviceQ1.GetWorklistAsync(new LabWorklistFilterDto());
        Assert.Empty(worklistQ1);
    }
}
