using Microsoft.EntityFrameworkCore;
using SCM.Application.Contracts.Diagnostics.Dtos;
using SCM.Application.Diagnostics;
using SCM.Domain.Billing.Exceptions;
using SCM.Domain.Branches;
using SCM.Domain.Clinical;
using SCM.Domain.Diagnostics;
using SCM.Domain.Diagnostics.Exceptions;
using SCM.Domain.Reception;
using SCM.Domain.Shared.Enums;
using SCM.EntityFrameworkCore;
using Xunit;

namespace SCM.Application.Tests;

public class RisPacsWorkstationWorkflowIntegrationTests
{
    private static SCMDbContext CreateContext(string dbName, ICurrentBranch? branch = null)
    {
        var options = new DbContextOptionsBuilder<SCMDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new SCMDbContext(options, branch);
    }

    [Fact]
    public async Task HP03_Ultrasound_GenderTemplates_4Slots_And_Sign()
    {
        var dbName = Guid.NewGuid().ToString();
        var branchId = Guid.NewGuid();
        var branch = new CurrentBranch(branchId, "CNQ1");
        using var db = CreateContext(dbName, branch);

        db.ImagingReportTemplates.AddRange(
            new ImagingReportTemplate(ImagingModality.US, "ABDOMEN", "US_MALE", "SA Bụng Nam", "Tiền liệt tuyến bình thường", "Bình thường", gender: 1, branchId: branchId, branchCode: "CNQ1"),
            new ImagingReportTemplate(ImagingModality.US, "ABDOMEN", "US_FEMALE", "SA Bụng Nữ", "Tử cung phần phụ bình thường", "Bình thường", gender: 2, branchId: branchId, branchCode: "CNQ1"));

        var patient = new Patient("BN-SA", "Trần Thị B", new DateTime(1995, 2, 10), Gender.Female, "079195001234", "0908887776");
        var enc = new Encounter("ENC-SA-01", branchId, "CNQ1", patient.Id, WorkflowRoute.Route1_FastTrack);
        var order = new MedicalOrder(enc.Id, enc.EncounterCode, patient.Id, patient.FullName, Guid.NewGuid(), "BS. An", MedicalOrderType.DiagnosticImaging, isInPackage: true, branchId: branchId, branchCode: "CNQ1");
        order.AddItem(Guid.NewGuid(), "SA-OBUNG", "Siêu âm ổ bụng tổng quát", 220000m, 1);

        db.Patients.Add(patient);
        db.Encounters.Add(enc);
        db.MedicalOrders.Add(order);
        await db.SaveChangesAsync();

        var service = new DiagnosticImagingAppService(db, branch);

        var femaleTemplates = await service.GetReportTemplatesAsync((int)ImagingModality.US, (int)Gender.Female);
        Assert.Single(femaleTemplates);
        Assert.Equal("US_FEMALE", femaleTemplates[0].TemplateCode);

        var draft = await service.SaveDraftWithMediaAsync(new SaveImagingDraftDto
        {
            EncounterId = enc.Id,
            EncounterCode = enc.EncounterCode,
            MedicalOrderId = order.Id,
            MedicalOrderItemId = order.Items[0].Id,
            Modality = ImagingModality.US,
            ServiceCode = "SA-OBUNG",
            ServiceName = "Siêu âm ổ bụng tổng quát",
            Description = femaleTemplates[0].ContentTemplate,
            Conclusion = "Tử cung phần phụ và ổ bụng bình thường",
            LayoutMode = 4,
            CapturedMedia = new List<CapturedMediaDto>
            {
                new() { Position = 1, MediaUrlOrBase64 = "data:image/png;base64,IMG1", Caption = "Gan" },
                new() { Position = 2, MediaUrlOrBase64 = "data:image/png;base64,IMG2", Caption = "Thận" },
                new() { Position = 3, MediaUrlOrBase64 = "data:image/png;base64,IMG3", Caption = "Tử cung" },
                new() { Position = 4, MediaUrlOrBase64 = "data:image/png;base64,IMG4", Caption = "Buồng trứng" }
            }
        });

        Assert.Equal(4, draft.CapturedMedia.Count);

        var signed = await service.SignReportAsync(new SignImagingReportDto
        {
            RecordId = draft.Id,
            DoctorId = Guid.NewGuid(),
            DoctorName = "BS.CKI Trần Minh Triết",
            DoctorLicenseNo = "006540/HCM-CCHN",
            ActorGroupOverride = ClinicalActorGroup.RadiologyDoctor
        });

        Assert.True(signed.IsSigned);
        Assert.True(signed.IsLocked);
        Assert.Equal(ImagingWorkflowStatus.Signed, signed.WorkflowStatus);
    }

    [Fact]
    public async Task HP04_And_UN05_Endoscopy_Specialty_Boundary_And_VideoClip()
    {
        var dbName = Guid.NewGuid().ToString();
        var branchId = Guid.NewGuid();
        var branch = new CurrentBranch(branchId, "CNQ1");
        using var db = CreateContext(dbName, branch);

        var enc = new Encounter("ENC-NS-01", branchId, "CNQ1", Guid.NewGuid(), WorkflowRoute.Route1_FastTrack);
        db.Encounters.Add(enc);
        await db.SaveChangesAsync();

        var service = new DiagnosticImagingAppService(db, branch);

        // 1. Nội soi TMH: BS Tai Mũi Họng được ký + kèm Video clip
        var entDraft = await service.SaveDraftWithMediaAsync(new SaveImagingDraftDto
        {
            EncounterId = enc.Id,
            EncounterCode = enc.EncounterCode,
            Modality = ImagingModality.Endoscopy,
            EndoscopyGroup = EndoscopySubType.ENT,
            ServiceCode = "NS-TMH",
            ServiceName = "Nội soi Tai Mũi Họng",
            Conclusion = "Niêm mạc mũi họng bình thường",
            CapturedMedia = new List<CapturedMediaDto>
            {
                new() { Position = 1, MediaType = CapturedMediaType.Image, MediaUrlOrBase64 = "data:image/png;base64,ENT1", Caption = "Màng nhĩ" },
                new() { Position = 0, MediaType = CapturedMediaType.Video, MediaUrlOrBase64 = "data:video/webm;base64,CLIP", DurationSeconds = 12, Caption = "Video thanh quản" }
            }
        });

        var signedEnt = await service.SignReportAsync(new SignImagingReportDto
        {
            RecordId = entDraft.Id,
            DoctorId = Guid.NewGuid(),
            DoctorName = "BS.CKI Lê Thị Hương",
            DoctorLicenseNo = "009341/HCM-CCHN",
            ActorGroupOverride = ClinicalActorGroup.SpecialtyDoctor,
            PrimarySpecialtyOverride = "Tai Mũi Họng"
        });
        Assert.True(signedEnt.IsSigned);
        Assert.Contains(signedEnt.CapturedMedia, m => m.MediaType == CapturedMediaType.Video && m.DurationSeconds == 12);

        // 2. Nội soi Tiêu hóa: BS TMH cố ký -> UnauthorizedModalitySignException
        var giDraft = await service.SaveDraftWithMediaAsync(new SaveImagingDraftDto
        {
            EncounterId = enc.Id,
            EncounterCode = enc.EncounterCode,
            Modality = ImagingModality.Endoscopy,
            EndoscopyGroup = EndoscopySubType.GastroIntestinal,
            ServiceCode = "NS-DA-DAY",
            ServiceName = "Nội soi Thực quản - Dạ dày",
            CloTestResult = "Negative",
            Conclusion = "Viêm hang vị nhẹ",
            CapturedMedia = new List<CapturedMediaDto>
            {
                new() { Position = 1, MediaType = CapturedMediaType.Image, MediaUrlOrBase64 = "data:image/png;base64,GI1", Caption = "Hang vị" }
            }
        });

        await Assert.ThrowsAsync<UnauthorizedModalitySignException>(() =>
            service.SignReportAsync(new SignImagingReportDto
            {
                RecordId = giDraft.Id,
                DoctorId = Guid.NewGuid(),
                DoctorName = "BS.CKI Lê Thị Hương",
                DoctorLicenseNo = "009341/HCM-CCHN",
                ActorGroupOverride = ClinicalActorGroup.SpecialtyDoctor,
                PrimarySpecialtyOverride = "Tai Mũi Họng"
            }));
    }

    [Fact]
    public async Task HP05_UN04_SEC03_XRay_Technician_Radiologist_FilmDelivery_And_Addendum()
    {
        var dbName = Guid.NewGuid().ToString();
        var branchId = Guid.NewGuid();
        var branch = new CurrentBranch(branchId, "CNQ1");
        using var db = CreateContext(dbName, branch);

        var patient = new Patient("BN-XQ", "Hoàng Thị E", new DateTime(1989, 7, 7), Gender.Female, "079189001234", "0905556667");
        var enc = new Encounter("ENC-XQ-01", branchId, "CNQ1", patient.Id, WorkflowRoute.Route1_FastTrack);
        var order = new MedicalOrder(enc.Id, enc.EncounterCode, patient.Id, patient.FullName, Guid.NewGuid(), "BS. An", MedicalOrderType.DiagnosticImaging, isInPackage: false, branchId: branchId, branchCode: "CNQ1");
        order.AddItem(Guid.NewGuid(), "XQ-NGUC", "X-Quang ngực thẳng DR", 180000m, 1);
        order.Items[0].MarkPaid(Guid.NewGuid());
        order.UpdatePaymentStatusFromItems();

        db.Patients.Add(patient);
        db.Encounters.Add(enc);
        db.MedicalOrders.Add(order);
        await db.SaveChangesAsync();

        var service = new DiagnosticImagingAppService(db, branch);

        // KTV pushes MWL & marks Captured
        var mwlRec = await service.PushDicomWorklistAsync(new PushMwlDto
        {
            EncounterId = enc.Id,
            EncounterCode = enc.EncounterCode,
            MedicalOrderId = order.Id,
            MedicalOrderItemId = order.Items[0].Id,
            Modality = ImagingModality.DR,
            ServiceCode = "XQ-NGUC",
            ServiceName = "X-Quang ngực thẳng DR"
        });
        Assert.Equal(ImagingWorkflowStatus.InProgress, mwlRec.WorkflowStatus);
        Assert.False(string.IsNullOrEmpty(mwlRec.DicomAccessionNumber));

        var captured = await service.MarkCapturedByTechnicianAsync(new MarkImagingCapturedDto
        {
            RecordId = mwlRec.Id,
            TechnicianId = Guid.NewGuid(),
            TechnicianName = "KTV. Tuấn",
            StudyInstanceUid = "1.2.840.10008.999"
        });
        Assert.Equal(ImagingWorkflowStatus.Captured, captured.WorkflowStatus);

        // UN-04: KTV blocked from signing
        await Assert.ThrowsAsync<UnauthorizedModalitySignException>(() =>
            service.SignReportAsync(new SignImagingReportDto
            {
                RecordId = captured.Id,
                DoctorId = Guid.NewGuid(),
                DoctorName = "KTV. Tuấn",
                ActorGroupOverride = ClinicalActorGroup.ParaclinicalTechnician,
                Conclusion = "Phổi bình thường"
            }));

        // BS CĐHA signs -> ConfirmFilmDelivery -> Locked -> Addendum
        var signed = await service.SignReportAsync(new SignImagingReportDto
        {
            RecordId = captured.Id,
            DoctorId = Guid.NewGuid(),
            DoctorName = "BS.CKI Trần Minh Triết",
            DoctorLicenseNo = "006540/HCM-CCHN",
            ActorGroupOverride = ClinicalActorGroup.RadiologyDoctor,
            Conclusion = "Hình tim phổi bình thường"
        });
        Assert.True(signed.IsSigned);

        var delivered = await service.ConfirmFilmDeliveryAsync(new ConfirmFilmDeliveryDto
        {
            RecordId = signed.Id,
            StaffName = "ĐD. Quầy CĐHA"
        });
        Assert.True(delivered.IsFilmDelivered);
        Assert.True(delivered.IsLocked);

        await Assert.ThrowsAsync<ImagingRecordImmutableException>(() =>
            service.SaveDraftWithMediaAsync(new SaveImagingDraftDto
            {
                RecordId = delivered.Id,
                EncounterId = enc.Id,
                EncounterCode = enc.EncounterCode,
                Modality = ImagingModality.DR,
                ServiceCode = "XQ-NGUC",
                ServiceName = "X-Quang ngực thẳng DR",
                Conclusion = "Sửa trái phép"
            }));

        var withAddendum = await service.AddAddendumAsync(new AddAddendumDto
        {
            RecordId = delivered.Id,
            AddendumText = "Bổ sung: Theo dõi nốt mờ nhỏ 4mm thùy trên phổi phải",
            ActorName = "BS.CKI Trần Minh Triết"
        });
        Assert.Equal("Hình tim phổi bình thường", withAddendum.Conclusion);
        Assert.Equal(1, withAddendum.AddendumCount);

        // SEC-04: PII masked in audit log
        var logs = await service.GetAuditLogsAsync(delivered.Id);
        Assert.All(logs, l => Assert.DoesNotContain("079189001234", l.MaskedPiiSummary));
    }

    [Fact]
    public async Task CC03_Refund_Race_Blocks_Unpaid_Execution_On_Save()
    {
        var dbName = Guid.NewGuid().ToString();
        var branchId = Guid.NewGuid();
        var branch = new CurrentBranch(branchId, "CNQ1");
        using var db = CreateContext(dbName, branch);

        var enc = new Encounter("ENC-RACE-01", branchId, "CNQ1", Guid.NewGuid(), WorkflowRoute.Route1_FastTrack);
        var order = new MedicalOrder(enc.Id, enc.EncounterCode, enc.PatientId, "BN Race", Guid.NewGuid(), "BS. An", MedicalOrderType.DiagnosticImaging, isInPackage: false, branchId: branchId, branchCode: "CNQ1");
        order.AddItem(Guid.NewGuid(), "SA-THYROID", "Siêu âm tuyến giáp", 200000m, 1);

        db.Encounters.Add(enc);
        db.MedicalOrders.Add(order);
        await db.SaveChangesAsync();

        var service = new DiagnosticImagingAppService(db, branch);

        // PendingPayment -> INV-GATE-01 blocks SaveDraftWithMediaAsync & PushDicomWorklistAsync
        await Assert.ThrowsAsync<UnpaidServiceExecutionBlockedException>(() =>
            service.SaveDraftWithMediaAsync(new SaveImagingDraftDto
            {
                EncounterId = enc.Id,
                EncounterCode = enc.EncounterCode,
                MedicalOrderId = order.Id,
                MedicalOrderItemId = order.Items[0].Id,
                Modality = ImagingModality.US,
                ServiceCode = "SA-THYROID",
                ServiceName = "Siêu âm tuyến giáp",
                Conclusion = "Bình thường"
            }));
    }
}
