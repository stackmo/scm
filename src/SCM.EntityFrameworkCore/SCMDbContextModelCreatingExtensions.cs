using Microsoft.EntityFrameworkCore;
using SCM.Domain.B2B;
using SCM.Domain.Billing;
using SCM.Domain.Branches;
using SCM.Domain.Clinical;
using SCM.Domain.Diagnostics;
using SCM.Domain.Diagnostics.Gateway;
using SCM.Domain.Grading;
using SCM.Domain.Identity;
using SCM.Domain.Outbox;
using SCM.Domain.Pharmacy;
using SCM.Domain.Prescription;
using SCM.Domain.Phlebotomy;
using SCM.Domain.Queueing;
using SCM.Domain.Reception;

namespace SCM.EntityFrameworkCore;

public static class SCMDbContextModelCreatingExtensions
{
    public static void ConfigureB2BAndPackages(this ModelBuilder modelBuilder)
    {
        // -------------------------------------------------------------
        // 1. Company (B2B Doanh nghiệp)
        // -------------------------------------------------------------
        modelBuilder.Entity<Company>(b =>
        {
            b.ToTable("AppCompanies");
            b.HasKey(x => x.Id);
            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.Name).IsRequired().HasMaxLength(250);
            b.Property(x => x.TaxCode).HasMaxLength(50);
            b.Property(x => x.Address).HasMaxLength(500);
            b.Property(x => x.ContactPerson).HasMaxLength(150);
            b.Property(x => x.ContactPhone).HasMaxLength(50);
            b.Property(x => x.ContactEmail).HasMaxLength(150);
            b.Property(x => x.CreditLimit).HasPrecision(18, 2).HasDefaultValue(0m);

            b.HasIndex(x => x.Code).IsUnique();
            b.HasIndex(x => x.TaxCode);
        });

        // -------------------------------------------------------------
        // 2. CompanyContract (Hợp đồng KSK đoàn)
        // -------------------------------------------------------------
        modelBuilder.Entity<CompanyContract>(b =>
        {
            b.ToTable("AppCompanyContracts");
            b.HasKey(x => x.Id);
            b.Property(x => x.ContractNumber).IsRequired().HasMaxLength(100);
            b.Property(x => x.TotalEstimatedAmount).HasPrecision(18, 2);
            b.Property(x => x.PaymentTerms).HasMaxLength(500);

            b.HasIndex(x => x.ContractNumber).IsUnique();

            b.HasOne(x => x.Company)
             .WithMany(c => c.Contracts)
             .HasForeignKey(x => x.CompanyId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // -------------------------------------------------------------
        // 3. Package (Gói KSK đoàn & Gói khám Phòng Khám)
        // -------------------------------------------------------------
        modelBuilder.Entity<Package>(b =>
        {
            b.ToTable("AppPackages");
            b.HasKey(x => x.Id);
            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.Name).IsRequired().HasMaxLength(250);
            b.Property(x => x.NationalTemplateCode).HasMaxLength(20);
            b.Property(x => x.TargetPrice).HasPrecision(18, 2);

            b.HasIndex(x => x.Code).IsUnique();
            b.HasIndex(x => new { x.Scope, x.IsActive });

            b.HasOne(x => x.Company)
             .WithMany(c => c.Packages)
             .HasForeignKey(x => x.CompanyId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Contract)
             .WithMany(c => c.Packages)
             .HasForeignKey(x => x.ContractId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // -------------------------------------------------------------
        // 4. PackageTargetGroup (Nhóm đối tượng trong gói)
        // -------------------------------------------------------------
        modelBuilder.Entity<PackageTargetGroup>(b =>
        {
            b.ToTable("AppPackageTargetGroups");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired().HasMaxLength(150);
            b.Property(x => x.Description).HasMaxLength(500);

            b.HasOne(x => x.Package)
             .WithMany(p => p.TargetGroups)
             .HasForeignKey(x => x.PackageId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // -------------------------------------------------------------
        // 5. PackageService (Dịch vụ trong gói khám)
        // -------------------------------------------------------------
        modelBuilder.Entity<PackageService>(b =>
        {
            b.ToTable("AppPackageServices");
            b.HasKey(x => x.Id);
            b.Property(x => x.ServiceCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.ServiceName).IsRequired().HasMaxLength(250);
            b.Property(x => x.OriginalPrice).HasPrecision(18, 2);
            b.Property(x => x.DealPrice).HasPrecision(18, 2);
            b.Property(x => x.FloorPrice).HasPrecision(18, 2);

            b.HasOne(x => x.Package)
             .WithMany(p => p.Services)
             .HasForeignKey(x => x.PackageId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.TargetGroup)
             .WithMany(g => g.Services)
             .HasForeignKey(x => x.TargetGroupId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Service)
             .WithMany()
             .HasForeignKey(x => x.ServiceId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // -------------------------------------------------------------
        // 6. CompanyEmployee (CBNV khám đoàn)
        // -------------------------------------------------------------
        modelBuilder.Entity<CompanyEmployee>(b =>
        {
            b.ToTable("AppCompanyEmployees");
            b.HasKey(x => x.Id);
            b.Property(x => x.FullName).IsRequired().HasMaxLength(150);
            b.Property(x => x.IdentificationNumber).IsRequired().HasMaxLength(20);
            b.Property(x => x.Department).HasMaxLength(150);
            b.Property(x => x.EmployeeCode).HasMaxLength(50);
            b.Property(x => x.PhoneNumber).HasMaxLength(50);
            b.Property(x => x.Email).HasMaxLength(150);
            b.Property(x => x.HealthInsuranceCode).HasMaxLength(50);
            b.Property(x => x.Notes).HasMaxLength(500);

            b.HasIndex(x => x.IdentificationNumber);
            b.HasIndex(x => new { x.PackageId, x.IdentificationNumber }).IsUnique();
            b.HasIndex(x => x.IsSampleCollected);
            b.HasIndex(x => x.CompanyId);
            b.HasIndex(x => new { x.CompanyId, x.FullName });
            b.HasIndex(x => new { x.PackageId, x.Department });

            b.HasOne(x => x.Company)
             .WithMany(c => c.Employees)
             .HasForeignKey(x => x.CompanyId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Contract)
             .WithMany(c => c.Employees)
             .HasForeignKey(x => x.ContractId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Package)
             .WithMany(p => p.Employees)
             .HasForeignKey(x => x.PackageId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.TargetGroup)
             .WithMany(g => g.Employees)
             .HasForeignKey(x => x.TargetGroupId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }

    public static void ConfigurePhlebotomy(this ModelBuilder modelBuilder)
    {
        // -------------------------------------------------------------
        // 1. PhlebotomyBatch (Đợt lấy mẫu ngoại viện)
        // -------------------------------------------------------------
        modelBuilder.Entity<PhlebotomyBatch>(b =>
        {
            b.ToTable("AppPhlebotomyBatches");
            b.HasKey(x => x.Id);
            b.Property(x => x.BatchCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.BatchName).IsRequired().HasMaxLength(250);
            b.Property(x => x.SamplingLocation).HasMaxLength(500);
            b.Property(x => x.Notes).HasMaxLength(1000);

            b.HasIndex(x => x.BatchCode).IsUnique();
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.SamplingDate);

            b.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Contract)
                .WithMany()
                .HasForeignKey(x => x.ContractId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Package)
                .WithMany()
                .HasForeignKey(x => x.PackageId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // -------------------------------------------------------------
        // 2. BatchEmployeeMapping (Phân bổ CBNV vào ca/đợt lấy mẫu)
        // -------------------------------------------------------------
        modelBuilder.Entity<BatchEmployeeMapping>(b =>
        {
            b.ToTable("AppBatchEmployeeMappings");
            b.HasKey(x => x.Id);
            b.Property(x => x.CollectorStaff).HasMaxLength(150);
            b.Property(x => x.SamplingNotes).HasMaxLength(500);

            b.HasIndex(x => new { x.BatchId, x.EmployeeId }).IsUnique();
            b.HasIndex(x => x.IsCollected);

            b.HasOne(x => x.Batch)
                .WithMany(p => p.EmployeeMappings)
                .HasForeignKey(x => x.BatchId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // -------------------------------------------------------------
        // 3. SpecimenTube (Ống nghiệm lấy mẫu ngoại viện & ngoại trú)
        // -------------------------------------------------------------
        modelBuilder.Entity<SpecimenTube>(b =>
        {
            b.ToTable("AppSpecimenTubes");
            b.HasKey(x => x.Id);
            b.Property(x => x.BranchCode).HasMaxLength(50);
            b.Property(x => x.EncounterCode).HasMaxLength(50);
            b.Property(x => x.TubeBarcode).IsRequired().HasMaxLength(100);
            b.Property(x => x.CapColor).HasMaxLength(50);
            b.Property(x => x.TargetTests).HasMaxLength(500);
            b.Property(x => x.CollectorId).HasMaxLength(100);
            b.Property(x => x.RejectionReason).HasMaxLength(500);

            b.HasIndex(x => x.TubeBarcode).IsUnique();
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.TubeType);
            b.HasIndex(x => x.EncounterId);
            b.HasIndex(x => x.MedicalOrderId);
            b.HasIndex(x => x.BranchId);

            b.HasOne(x => x.BatchEmployeeMapping)
                .WithMany(m => m.SpecimenTubes)
                .HasForeignKey(x => x.BatchEmployeeMappingId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Manifest)
                .WithMany(m => m.SpecimenTubes)
                .HasForeignKey(x => x.ManifestId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // -------------------------------------------------------------
        // 4. ColdChainManifest (Biên bản bàn giao chuỗi lạnh 2°C - 8°C)
        // -------------------------------------------------------------
        modelBuilder.Entity<ColdChainManifest>(b =>
        {
            b.ToTable("AppColdChainManifests");
            b.HasKey(x => x.Id);
            b.Property(x => x.BranchCode).HasMaxLength(50);
            b.Property(x => x.ManifestCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.HandoverStaff).IsRequired().HasMaxLength(150);
            b.Property(x => x.CourierStaff).IsRequired().HasMaxLength(150);
            b.Property(x => x.LabReceiverStaff).HasMaxLength(150);
            b.Property(x => x.DepartureTemperature).HasPrecision(18, 2);
            b.Property(x => x.ArrivalTemperature).HasPrecision(18, 2);
            b.Property(x => x.DiscrepancyNote).HasMaxLength(500);
            b.Property(x => x.Notes).HasMaxLength(1000);

            b.HasIndex(x => x.ManifestCode).IsUnique();
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.IsDegraded);
            b.HasIndex(x => x.BranchId);

            b.HasOne(x => x.Batch)
                .WithMany(b => b.Manifests)
                .HasForeignKey(x => x.BatchId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    public static void ConfigureQueueing(this ModelBuilder modelBuilder)
    {
        // -------------------------------------------------------------
        // 1. QueueTicket (Phiếu thứ tự & phân luồng tiếp đón)
        // -------------------------------------------------------------
        modelBuilder.Entity<QueueTicket>(b =>
        {
            b.ToTable("AppQueueTickets");
            b.HasKey(x => x.Id);
            b.Property(x => x.TicketNumber).IsRequired().HasMaxLength(50);
            b.Property(x => x.PatientName).IsRequired().HasMaxLength(200);
            b.Property(x => x.Cccd).HasMaxLength(50);
            b.Property(x => x.Notes).HasMaxLength(500);

            b.HasIndex(x => x.TicketNumber);
            b.HasIndex(x => x.CheckinTime);
            b.HasIndex(x => x.PatientId);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.StreamType);

            b.HasOne(x => x.Contract)
                .WithMany()
                .HasForeignKey(x => x.ContractId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasMany(x => x.Assignments)
                .WithOne(a => a.Ticket)
                .HasForeignKey(a => a.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // -------------------------------------------------------------
        // 2. QueueAssignment (Phân bổ phòng khám chi tiết)
        // -------------------------------------------------------------
        modelBuilder.Entity<QueueAssignment>(b =>
        {
            b.ToTable("AppQueueAssignments");
            b.HasKey(x => x.Id);
            b.Property(x => x.RoomCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.RoomName).IsRequired().HasMaxLength(150);
            b.Property(x => x.DoctorStaffName).HasMaxLength(150);
            b.Property(x => x.Notes).HasMaxLength(500);

            b.HasIndex(x => new { x.RoomId, x.Status });
            b.HasIndex(x => new { x.RoomCode, x.Status });
            b.HasIndex(x => new { x.TicketId, x.OrderIndex });
        });

        // -------------------------------------------------------------
        // 3. ClinicRoomLoad (Theo dõi tải phòng khám thời gian thực)
        // -------------------------------------------------------------
        modelBuilder.Entity<ClinicRoomLoad>(b =>
        {
            b.ToTable("AppClinicRoomLoads");
            b.HasKey(x => x.Id);
            b.Property(x => x.RoomCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.RoomName).IsRequired().HasMaxLength(150);
            b.Property(x => x.CurrentCallingTicketNumber).HasMaxLength(50);
            b.Property(x => x.CurrentServingPatientName).HasMaxLength(200);

            b.HasIndex(x => x.RoomId).IsUnique();
            b.HasIndex(x => x.RoomCode);
            b.HasIndex(x => x.RoomType);
            b.HasIndex(x => x.IsActive);
        });

        // -------------------------------------------------------------
        // 4. ClinicRoomQueue (Hàng đợi cấp số buồng khám chống xung đột)
        // -------------------------------------------------------------
        modelBuilder.Entity<ClinicRoomQueue>(b =>
        {
            b.ToTable("AppClinicRoomQueues");
            b.HasKey(x => x.Id);
            b.Property(x => x.ConcurrencyStamp).IsRequired().HasMaxLength(40);

            b.HasIndex(x => new { x.TenantId, x.RoomId, x.QueueDate }).IsUnique();
        });

        // -------------------------------------------------------------
        // 5. Patient (Hồ sơ Bệnh nhân / MPI + M3 + QĐ 130 BHYT)
        // -------------------------------------------------------------
        modelBuilder.Entity<Patient>(b =>
        {
            b.ToTable("AppPatients");
            b.HasKey(x => x.Id);
            b.Property(x => x.PatientCode).IsRequired().HasMaxLength(20);
            b.Property(x => x.FullName).IsRequired().HasMaxLength(150);
            b.Property(x => x.NormalizedFullName).IsRequired().HasMaxLength(150);
            b.Property(x => x.IdentityCardNumber).HasMaxLength(12);
            b.Property(x => x.PhoneNumber).HasMaxLength(20);
            b.Property(x => x.Address).HasMaxLength(500);
            b.Property(x => x.InsuranceCardNumber).HasMaxLength(50);

            b.Property(x => x.NationalityCode).HasMaxLength(10);
            b.Property(x => x.NgheNghiepCode).HasMaxLength(20);
            b.Property(x => x.WorkPlaceName).HasMaxLength(255);
            b.Property(x => x.WorkPlaceWardCode).HasMaxLength(20);
            b.Property(x => x.CityCode).HasMaxLength(20);
            b.Property(x => x.WardCode).HasMaxLength(20);
            b.Property(x => x.AvatarUrl).HasMaxLength(1000);
            b.Property(x => x.FamilyHistoryDiseaseIds).HasMaxLength(250);
            b.Property(x => x.FamilyHistoryIcdCodes).HasMaxLength(250);
            b.Property(x => x.BhytInitialFacilityCode).HasMaxLength(10);
            b.Property(x => x.BhytAreaCode).HasMaxLength(10);
            b.Property(x => x.BhytBenefitRate).HasPrecision(5, 4);
            b.Property(x => x.BhytAddress).HasMaxLength(500);

            b.HasIndex(x => x.PatientCode).IsUnique();
            b.HasIndex(x => x.IdentityCardNumber);
            b.HasIndex(x => x.InsuranceCardNumber);
            b.HasIndex(x => x.NormalizedFullName);
        });

        // -------------------------------------------------------------
        // 6. Encounter (Lượt khám / Đợt khám - Branch Scoped)
        // -------------------------------------------------------------
        modelBuilder.Entity<Encounter>(b =>
        {
            b.ToTable("AppEncounters");
            b.HasKey(x => x.Id);
            b.Property(x => x.EncounterCode).IsRequired().HasMaxLength(20);
            b.Property(x => x.BranchCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.DepositAmount).HasPrecision(18, 2);
            b.Property(x => x.VisitReason).HasMaxLength(500);
            b.Property(x => x.BhytReferralCode).HasMaxLength(50);

            b.HasIndex(x => x.EncounterCode).IsUnique();
            b.HasIndex(x => new { x.BranchId, x.EncounterDate });
            b.HasIndex(x => new { x.BranchId, x.Status, x.EncounterDate });
            b.HasIndex(x => new { x.PatientId, x.EncounterDate });
            b.HasIndex(x => new { x.BranchCode, x.EncounterDate });
            b.HasIndex(x => x.PatientId);
            b.HasIndex(x => x.QueueTicketId);
            b.HasIndex(x => x.Status);

            b.HasOne(x => x.Patient)
                .WithMany()
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // -------------------------------------------------------------
        // 7. QueueSequence (Cấp số STT nguyên tử theo Chi nhánh và Phòng)
        // -------------------------------------------------------------
        modelBuilder.Entity<QueueSequence>(b =>
        {
            b.ToTable("AppQueueSequences");
            b.HasKey(x => x.Id);
            b.Property(x => x.RowVersion).IsRowVersion();

            b.HasIndex(x => new { x.BranchId, x.ClinicRoomId, x.Date }).IsUnique();
        });
    }

    public static void ConfigureClinical(this ModelBuilder modelBuilder)
    {
        // -------------------------------------------------------------
        // 1. ClinicalExamination (Hồ sơ khám lâm sàng)
        // -------------------------------------------------------------
        modelBuilder.Entity<ClinicalExamination>(b =>
        {
            b.ToTable("AppClinicalExaminations");
            b.HasKey(x => x.Id);
            b.Property(x => x.EncounterCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.PatientName).IsRequired().HasMaxLength(200);

            b.HasIndex(x => x.EncounterId).IsUnique();
            b.HasIndex(x => x.EncounterCode);
            b.HasIndex(x => x.PatientId);
            b.HasIndex(x => x.IsCompleted);

            b.HasOne(x => x.VitalSigns)
                .WithOne(v => v.ClinicalExam)
                .HasForeignKey<VitalSigns>(v => v.ClinicalExamId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(x => x.SpecialtyDetails)
                .WithOne(s => s.ClinicalExam)
                .HasForeignKey(s => s.ClinicalExamId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // -------------------------------------------------------------
        // 2. VitalSigns (Sinh hiệu)
        // -------------------------------------------------------------
        modelBuilder.Entity<VitalSigns>(b =>
        {
            b.ToTable("AppVitalSigns");
            b.HasKey(x => x.Id);
            b.Property(x => x.Bmi).HasPrecision(5, 2);
            b.Property(x => x.HeightCm).HasPrecision(5, 1);
            b.Property(x => x.WeightKg).HasPrecision(5, 1);
            b.Property(x => x.Temperature).HasPrecision(4, 1);
            b.Property(x => x.SpO2).HasPrecision(5, 2);
            b.Property(x => x.Notes).HasMaxLength(500);

            b.HasIndex(x => x.ClinicalExamId).IsUnique();
        });

        // -------------------------------------------------------------
        // 3. SpecialtyExamDetail (Chi tiết chuyên khoa)
        // -------------------------------------------------------------
        modelBuilder.Entity<SpecialtyExamDetail>(b =>
        {
            b.ToTable("AppSpecialtyExamDetails");
            b.HasKey(x => x.Id);
            b.Property(x => x.IcdCode).HasMaxLength(20);
            b.Property(x => x.IcdDiseaseName).HasMaxLength(250);
            b.Property(x => x.ObGynRefusalReason).HasMaxLength(250);
            b.Property(x => x.Advice).HasMaxLength(1000);
            b.Property(x => x.ClinicalDescription).HasMaxLength(2000);

            b.Property(x => x.VisualAcuityRightWithoutGlasses).HasMaxLength(20);
            b.Property(x => x.VisualAcuityLeftWithoutGlasses).HasMaxLength(20);
            b.Property(x => x.VisualAcuityRightWithGlasses).HasMaxLength(20);
            b.Property(x => x.VisualAcuityLeftWithGlasses).HasMaxLength(20);

            b.Property(x => x.HearingRightNormalVoice).HasMaxLength(20);
            b.Property(x => x.HearingLeftNormalVoice).HasMaxLength(20);
            b.Property(x => x.HearingRightWhisper).HasMaxLength(20);
            b.Property(x => x.HearingLeftWhisper).HasMaxLength(20);

            b.HasIndex(x => new { x.ClinicalExamId, x.SpecialtyType }).IsUnique();
            b.HasIndex(x => x.DoctorStaffId);
            b.HasIndex(x => x.HealthClassificationRefId);

            b.HasOne(x => x.DoctorStaff)
                .WithMany()
                .HasForeignKey(x => x.DoctorStaffId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.HealthClassificationRef)
                .WithMany()
                .HasForeignKey(x => x.HealthClassificationRefId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasMany(x => x.DentalTeethDetails)
                .WithOne(t => t.SpecialtyDetail)
                .HasForeignKey(t => t.SpecialtyDetailId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // -------------------------------------------------------------
        // 4. Dental32TeethDetail (Ma trận 32 răng)
        // -------------------------------------------------------------
        modelBuilder.Entity<Dental32TeethDetail>(b =>
        {
            b.ToTable("AppDental32TeethDetails");
            b.HasKey(x => x.Id);
            b.Property(x => x.ToothPosition).IsRequired().HasMaxLength(10);
            b.Property(x => x.Notes).HasMaxLength(250);

            b.HasIndex(x => new { x.SpecialtyDetailId, x.ToothPosition }).IsUnique();
            b.HasIndex(x => x.ToothConditionRefId);

            b.HasOne(x => x.ToothConditionRef)
                .WithMany()
                .HasForeignKey(x => x.ToothConditionRefId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // -------------------------------------------------------------
        // 5. MedicalOrder (Chỉ định Cận lâm sàng)
        // -------------------------------------------------------------
        modelBuilder.Entity<MedicalOrder>(b =>
        {
            b.ToTable("AppMedicalOrders");
            b.HasKey(x => x.Id);
            b.Property(x => x.OrderCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.EncounterCode).HasMaxLength(50);
            b.Property(x => x.PatientName).HasMaxLength(200);
            b.Property(x => x.DoctorName).HasMaxLength(150);
            b.Property(x => x.PackageName).HasMaxLength(250);
            b.Property(x => x.TotalAmount).HasPrecision(18, 2);
            b.Property(x => x.DiagnosisIcd10).HasMaxLength(50);
            b.Property(x => x.ClinicalNotes).HasMaxLength(1000);
            b.Property(x => x.BranchCode).HasMaxLength(50);

            b.HasIndex(x => x.OrderCode).IsUnique();
            b.HasIndex(x => x.EncounterId);
            b.HasIndex(x => new { x.BranchId, x.CreatedAt });
            b.HasIndex(x => new { x.EncounterId, x.Status });
            b.HasIndex(x => x.PatientId);
            b.HasIndex(x => x.DoctorId);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.OrderType);

            b.HasMany(x => x.Items)
                .WithOne()
                .HasForeignKey(i => i.MedicalOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // -------------------------------------------------------------
        // 6. MedicalOrderItem (Dịch vụ chỉ định CLS chi tiết)
        // -------------------------------------------------------------
        modelBuilder.Entity<MedicalOrderItem>(b =>
        {
            b.ToTable("AppMedicalOrderItems");
            b.HasKey(x => x.Id);
            b.Property(x => x.ServiceCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.ServiceName).IsRequired().HasMaxLength(250);
            b.Property(x => x.UnitPrice).HasPrecision(18, 2);
            b.Property(x => x.TotalPrice).HasPrecision(18, 2);
            b.Property(x => x.Notes).HasMaxLength(500);

            b.HasIndex(x => x.MedicalOrderId);
            b.HasIndex(x => x.ServiceId);
        });
    }

    public static void ConfigureBranches(this ModelBuilder modelBuilder)
    {
        // -------------------------------------------------------------
        // Wave 0: Multi-Branch & RBAC Identity Foundation Configurations
        // -------------------------------------------------------------
        modelBuilder.Entity<Branch>(b =>
        {
            b.ToTable("AppBranches");
            b.HasKey(x => x.Id);
            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.Name).IsRequired().HasMaxLength(250);
            b.Property(x => x.MinistryFacilityCode).HasMaxLength(50);
            b.Property(x => x.LicenseNo).HasMaxLength(100);
            b.Property(x => x.Address).HasMaxLength(500);
            b.Property(x => x.PhoneNumber).HasMaxLength(50);
            b.Property(x => x.DoctorInCharge).HasMaxLength(150);
            b.Property(x => x.BankAccountDetails).HasMaxLength(500);
            b.Property(x => x.TaxCode).HasMaxLength(30);
            b.Property(x => x.BankBin).HasMaxLength(20);
            b.Property(x => x.BankAccountNo).HasMaxLength(50);
            b.Property(x => x.BankAccountName).HasMaxLength(150);
            b.Property(x => x.EnableDepositManagement).HasDefaultValue(true);
            b.Property(x => x.DigitalCertConfig).HasMaxLength(2000);

            b.HasIndex(x => x.Code).IsUnique();
            b.HasIndex(x => x.MinistryFacilityCode);
            b.HasIndex(x => x.LicenseNo);
        });

        modelBuilder.Entity<StaffBranchAssignment>(b =>
        {
            b.ToTable("AppStaffBranchAssignments");
            b.HasKey(x => x.Id);
            b.Property(x => x.RolesInBranch).HasMaxLength(250);
            b.Property(x => x.LicenseNo).HasMaxLength(100);

            b.HasIndex(x => new { x.StaffId, x.BranchId });

            b.HasOne(x => x.Branch)
             .WithMany(br => br.StaffAssignments)
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLogPII>(b =>
        {
            b.ToTable("AppAuditLogPIIs");
            b.HasKey(x => x.Id);
            b.Property(x => x.UserName).HasMaxLength(150);
            b.Property(x => x.BranchCode).HasMaxLength(50);
            b.Property(x => x.Action).IsRequired().HasMaxLength(50);
            b.Property(x => x.EntityName).IsRequired().HasMaxLength(150);
            b.Property(x => x.EntityId).HasMaxLength(100);
            b.Property(x => x.MaskedDetails).HasMaxLength(2000);
            b.Property(x => x.ClientIp).HasMaxLength(50);
            b.Property(x => x.UserAgent).HasMaxLength(500);
            b.Property(x => x.Details).HasMaxLength(2000);

            b.HasIndex(x => x.BranchId);
            b.HasIndex(x => x.TimestampUtc);
            b.HasIndex(x => x.Action);
        });
    }

    public static void ConfigureWave6DiagnosticsAndPrescription(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LabResultRecord>(b =>
        {
            b.ToTable("AppLabResultRecords");
            b.HasKey(x => x.Id);
            b.Property(x => x.EncounterCode).HasMaxLength(50);
            b.Property(x => x.TubeBarcode).HasMaxLength(50);
            b.Property(x => x.TestCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.TestName).IsRequired().HasMaxLength(250);
            b.Property(x => x.NumericResult).HasPrecision(18, 4);
            b.Property(x => x.StringResult).HasMaxLength(250);
            b.Property(x => x.Unit).HasMaxLength(50);
            b.Property(x => x.ReferenceRange).HasMaxLength(100);
            b.Property(x => x.InstrumentId).HasMaxLength(100);
            b.Property(x => x.BridgePayloadHash).HasMaxLength(128);
            b.Property(x => x.TechVerifiedByName).HasMaxLength(150);
            b.Property(x => x.DoctorApprovedByName).HasMaxLength(150);
            b.Property(x => x.DoctorLicenseNo).HasMaxLength(50);
            b.Property(x => x.SignatureHash).HasMaxLength(500);
            b.Property(x => x.PanicAcknowledgedByName).HasMaxLength(150);
            b.Property(x => x.PanicAckNote).HasMaxLength(500);
            b.Property(x => x.UnlockReason).HasMaxLength(1000);
            b.Property(x => x.BranchCode).HasMaxLength(50);
            b.Property(x => x.Version).IsConcurrencyToken();

            b.HasIndex(x => x.EncounterId);
            b.HasIndex(x => x.MedicalOrderId);
            b.HasIndex(x => x.TubeBarcode);
            b.HasIndex(x => x.TestCode);
            b.HasIndex(x => x.M3FieldMapping);
            b.HasIndex(x => x.ReviewStatus);
            b.HasIndex(x => x.BridgePayloadHash);
            b.HasIndex(x => x.IsPanic);
            b.HasIndex(x => x.BranchId);
        });

        modelBuilder.Entity<DiagnosticImagingRecord>(b =>
        {
            b.ToTable("AppDiagnosticImagingRecords");
            b.HasKey(x => x.Id);
            b.Property(x => x.EncounterCode).HasMaxLength(50);
            b.Property(x => x.RoomCode).HasMaxLength(30);
            b.Property(x => x.ServiceCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.ServiceName).IsRequired().HasMaxLength(250);
            b.Property(x => x.DicomAccessionNumber).HasMaxLength(100);
            b.Property(x => x.StudyInstanceUid).HasMaxLength(150);
            b.Property(x => x.DicomWebUrl).HasMaxLength(500);
            b.Property(x => x.Description).HasMaxLength(4000);
            b.Property(x => x.Conclusion).IsRequired().HasMaxLength(1000);
            b.Property(x => x.Recommendation).HasMaxLength(1000);
            b.Property(x => x.CloTestResult).HasMaxLength(50);
            b.Property(x => x.TechnicianName).HasMaxLength(150);
            b.Property(x => x.TechNotes).HasMaxLength(500);
            b.Property(x => x.RadiologistDoctorName).HasMaxLength(150);
            b.Property(x => x.DoctorLicenseNo).HasMaxLength(50);
            b.Property(x => x.SignatureHash).HasMaxLength(500);
            b.Property(x => x.FilmDeliveredByName).HasMaxLength(150);
            b.Property(x => x.UnlockReason).HasMaxLength(1000);
            b.Property(x => x.BranchCode).HasMaxLength(50);
            b.Property(x => x.Version).IsConcurrencyToken();

            b.HasIndex(x => x.EncounterId);
            b.HasIndex(x => x.MedicalOrderId);
            b.HasIndex(x => x.RoomCode);
            b.HasIndex(x => x.WorkflowStatus);
            b.HasIndex(x => x.DicomAccessionNumber);
            b.HasIndex(x => x.StudyInstanceUid);
            b.HasIndex(x => x.ServiceCode);
            b.HasIndex(x => x.BranchId);
        });

        modelBuilder.Entity<Prescription>(b =>
        {
            b.ToTable("AppPrescriptions");
            b.HasKey(x => x.Id);
            b.Property(x => x.EncounterCode).HasMaxLength(50);
            b.Property(x => x.PatientName).IsRequired().HasMaxLength(150);
            b.Property(x => x.DoctorName).IsRequired().HasMaxLength(150);
            b.Property(x => x.DoctorLicenseNo).IsRequired().HasMaxLength(50);
            b.Property(x => x.DiagnosisIcd10).IsRequired().HasMaxLength(50);
            b.Property(x => x.DiagnosisText).IsRequired().HasMaxLength(500);
            b.Property(x => x.Instructions).HasMaxLength(1000);
            b.Property(x => x.Advices).HasMaxLength(1000);
            b.Property(x => x.SignatureHash).HasMaxLength(500);
            b.Property(x => x.NationalPrescriptionCode).HasMaxLength(100);
            b.Property(x => x.NationalQrCodeUrl).HasMaxLength(500);
            b.Property(x => x.BranchCode).HasMaxLength(50);

            b.HasIndex(x => x.EncounterId);
            b.HasIndex(x => x.PatientId);
            b.HasIndex(x => x.DoctorId);
            b.HasIndex(x => x.NationalPrescriptionCode);
            b.HasIndex(x => x.BranchId);

            b.HasMany(x => x.Items)
             .WithOne()
             .HasForeignKey(i => i.PrescriptionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PrescriptionItem>(b =>
        {
            b.ToTable("AppPrescriptionItems");
            b.HasKey(x => x.Id);
            b.Property(x => x.DrugName).IsRequired().HasMaxLength(250);
            b.Property(x => x.NationalDrugCode).HasMaxLength(100);
            b.Property(x => x.Quantity).HasPrecision(18, 2);
            b.Property(x => x.Unit).IsRequired().HasMaxLength(50);
            b.Property(x => x.Route).IsRequired().HasMaxLength(50);
            b.Property(x => x.MorningDose).HasPrecision(18, 2);
            b.Property(x => x.NoonDose).HasPrecision(18, 2);
            b.Property(x => x.EveningDose).HasPrecision(18, 2);
            b.Property(x => x.NightDose).HasPrecision(18, 2);
            b.Property(x => x.Instructions).HasMaxLength(500);

            b.HasIndex(x => x.PrescriptionId);
            b.HasIndex(x => x.DrugId);
            b.HasIndex(x => x.NationalDrugCode);
        });
    }

    public static void ConfigureWave7AutoGradingAndOutboxM3(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MedicalRecordSummary>(b =>
        {
            b.ToTable("AppMedicalRecordSummaries");
            b.HasKey(x => x.Id);
            b.Property(x => x.EncounterCode).HasMaxLength(50);
            b.Property(x => x.PatientName).IsRequired().HasMaxLength(150);
            b.Property(x => x.OverallGradeText).HasMaxLength(50);
            b.Property(x => x.ConclusionText).HasMaxLength(500);
            b.Property(x => x.Recommendations).HasMaxLength(1000);
            b.Property(x => x.LeadDoctorName).HasMaxLength(150);
            b.Property(x => x.LeadDoctorLicenseNo).HasMaxLength(50);
            b.Property(x => x.LeadDoctorSignatureHash).HasMaxLength(500);
            b.Property(x => x.FacilityRepresentativeName).HasMaxLength(150);
            b.Property(x => x.FacilityStampSignatureHash).HasMaxLength(500);
            b.Property(x => x.BranchCode).HasMaxLength(50);

            b.HasIndex(x => x.EncounterId).IsUnique();
            b.HasIndex(x => x.PatientId);
            b.HasIndex(x => x.OverallGrade);
            b.HasIndex(x => x.IsCompleted);
            b.HasIndex(x => x.BranchId);
        });

        modelBuilder.Entity<NationalSyncOutbox>(b =>
        {
            b.ToTable("AppNationalSyncOutbox");
            b.HasKey(x => x.Id);
            b.Property(x => x.EncounterCode).HasMaxLength(50);
            b.Property(x => x.ServiceCode).IsRequired().HasMaxLength(100);
            b.Property(x => x.PayloadJson).IsRequired();
            b.Property(x => x.LastError).HasMaxLength(2000);
            b.Property(x => x.TransactionId).HasMaxLength(100);
            b.Property(x => x.BranchCode).HasMaxLength(50);

            b.HasIndex(x => x.EncounterId);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.NextRetryTime);
            b.HasIndex(x => x.BranchId);
        });
    }

    public static void ConfigureWave8BillingAndSettlement(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Receipt>(b =>
        {
            b.ToTable("AppReceipts");
            b.HasKey(x => x.Id);
            b.Property(x => x.ReceiptNumber).IsRequired().HasMaxLength(50);
            b.Property(x => x.EncounterCode).HasMaxLength(50);
            b.Property(x => x.PatientName).IsRequired().HasMaxLength(150);
            b.Property(x => x.TotalAmount).HasPrecision(18, 2);
            b.Property(x => x.DepositAppliedAmount).HasPrecision(18, 2);
            b.Property(x => x.CashierName).HasMaxLength(150);
            b.Property(x => x.VietQrPayload).HasMaxLength(1000);
            b.Property(x => x.VietQrTransactionId).HasMaxLength(100);
            b.Property(x => x.BranchCode).HasMaxLength(50);

            b.HasIndex(x => x.ReceiptNumber).IsUnique();
            b.HasIndex(x => x.EncounterId);
            b.HasIndex(x => x.PatientId);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.BranchId);

            b.HasOne(x => x.RefundRecord)
             .WithOne()
             .HasForeignKey<RefundReceipt>(r => r.ReceiptId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefundReceipt>(b =>
        {
            b.ToTable("AppRefundReceipts");
            b.HasKey(x => x.Id);
            b.Property(x => x.ApprovedByAdminName).IsRequired().HasMaxLength(150);
            b.Property(x => x.RefundAmount).HasPrecision(18, 2);
            b.Property(x => x.RefundReason).IsRequired().HasMaxLength(500);

            b.HasIndex(x => x.ReceiptId).IsUnique();
            b.HasIndex(x => x.ApprovedByAdminId);
        });

        modelBuilder.Entity<CompanySettlement>(b =>
        {
            b.ToTable("AppCompanySettlements");
            b.HasKey(x => x.Id);
            b.Property(x => x.ContractNumber).IsRequired().HasMaxLength(100);
            b.Property(x => x.CompanyName).IsRequired().HasMaxLength(250);
            b.Property(x => x.UnitPricePerEmployee).HasPrecision(18, 2);
            b.Property(x => x.GrossAmount).HasPrecision(18, 2);
            b.Property(x => x.DiscountPercentage).HasPrecision(5, 2);
            b.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            b.Property(x => x.NetAmount).HasPrecision(18, 2);
            b.Property(x => x.InvoiceNumber).HasMaxLength(50);
            b.Property(x => x.InvoiceSerial).HasMaxLength(50);
            b.Property(x => x.InvoiceLookupCode).HasMaxLength(100);
            b.Property(x => x.BranchCode).HasMaxLength(50);

            b.HasIndex(x => x.CompanyContractId);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.BranchId);
        });

        modelBuilder.Entity<BillingSequence>(b =>
        {
            b.ToTable("AppBillingSequences");
            b.HasKey(x => x.Id);
            b.Property(x => x.BranchCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.Prefix).IsRequired().HasMaxLength(20);
            b.Property(x => x.DatePeriod).IsRequired().HasMaxLength(20);
            b.HasIndex(x => new { x.BranchId, x.Prefix, x.DatePeriod }).IsUnique();
        });

        modelBuilder.Entity<CashierShift>(b =>
        {
            b.ToTable("AppCashierShifts");
            b.HasKey(x => x.Id);
            b.Property(x => x.BranchCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.CashierName).IsRequired().HasMaxLength(150);
            b.Property(x => x.OpeningCash).HasPrecision(18, 2);
            b.Property(x => x.CashCollected).HasPrecision(18, 2);
            b.Property(x => x.CashRefunded).HasPrecision(18, 2);
            b.Property(x => x.ExpectedCash).HasPrecision(18, 2);
            b.Property(x => x.CountedCash).HasPrecision(18, 2);
            b.Property(x => x.CashVariance).HasPrecision(18, 2);
            b.Property(x => x.ExpectedTransfer).HasPrecision(18, 2);
            b.Property(x => x.ExpectedPos).HasPrecision(18, 2);
            b.Property(x => x.CloseNotes).HasMaxLength(500);
            b.Property(x => x.ConcurrencyStamp).IsConcurrencyToken();
            b.HasIndex(x => new { x.BranchId, x.CashierStaffId, x.Status });
        });

        modelBuilder.Entity<InvoiceSeries>(b =>
        {
            b.ToTable("AppInvoiceSeries");
            b.HasKey(x => x.Id);
            b.Property(x => x.BranchCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.TaxCode).IsRequired().HasMaxLength(30);
            b.Property(x => x.TemplateCode).IsRequired().HasMaxLength(30);
            b.Property(x => x.SeriesCode).IsRequired().HasMaxLength(30);
            b.Property(x => x.Version).IsConcurrencyToken();
            b.HasIndex(x => new { x.BranchId, x.TemplateCode, x.SeriesCode }).IsUnique();
        });

        modelBuilder.Entity<ElectronicInvoice>(b =>
        {
            b.ToTable("AppElectronicInvoices");
            b.HasKey(x => x.Id);
            b.Property(x => x.BranchCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.SupplierTaxCode).IsRequired().HasMaxLength(30);
            b.Property(x => x.TemplateCode).IsRequired().HasMaxLength(30);
            b.Property(x => x.SeriesCode).IsRequired().HasMaxLength(30);
            b.Property(x => x.InvoiceNumber).IsRequired().HasMaxLength(30);
            b.Property(x => x.ProviderCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.ProviderTransactionId).HasMaxLength(150);
            b.Property(x => x.ReservationCode).HasMaxLength(100);
            b.Property(x => x.LookupCode).HasMaxLength(100);
            b.Property(x => x.BuyerName).IsRequired().HasMaxLength(250);
            b.Property(x => x.BuyerTaxCode).HasMaxLength(30);
            b.Property(x => x.BuyerAddress).HasMaxLength(500);
            b.Property(x => x.BuyerEmail).HasMaxLength(150);
            b.Property(x => x.PaymentMethodName).HasMaxLength(50);
            b.Property(x => x.TotalAmountBeforeVat).HasPrecision(18, 2);
            b.Property(x => x.VatRate).HasPrecision(5, 2);
            b.Property(x => x.VatAmount).HasPrecision(18, 2);
            b.Property(x => x.TotalAmount).HasPrecision(18, 2);
            b.Property(x => x.CancelOrAdjustReason).HasMaxLength(500);
            b.Property(x => x.PdfUrl).HasMaxLength(1000);
            b.Property(x => x.IssuedByName).HasMaxLength(150);
            b.Property(x => x.Version).IsConcurrencyToken();

            b.HasIndex(x => new { x.BranchId, x.TemplateCode, x.SeriesCode, x.InvoiceNumber }).IsUnique();
            b.HasIndex(x => x.CompanySettlementId);
            b.HasIndex(x => x.Status);

            b.HasMany(x => x.Items)
             .WithOne()
             .HasForeignKey(i => i.ElectronicInvoiceId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(x => x.ReceiptLinks)
             .WithOne()
             .HasForeignKey(l => l.ElectronicInvoiceId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ElectronicInvoiceItem>(b =>
        {
            b.ToTable("AppElectronicInvoiceItems");
            b.HasKey(x => x.Id);
            b.Property(x => x.ItemCode).HasMaxLength(50);
            b.Property(x => x.ItemName).IsRequired().HasMaxLength(250);
            b.Property(x => x.Unit).HasMaxLength(50);
            b.Property(x => x.Quantity).HasPrecision(18, 2);
            b.Property(x => x.UnitPrice).HasPrecision(18, 2);
            b.Property(x => x.VatRate).HasPrecision(5, 2);
            b.Property(x => x.LineTotal).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ElectronicInvoiceReceiptLink>(b =>
        {
            b.ToTable("AppElectronicInvoiceReceiptLinks");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.ReceiptId);
        });

        modelBuilder.Entity<PatientConsentSignature>(b =>
        {
            b.ToTable("AppPatientConsentSignatures");
            b.HasKey(x => x.Id);
            b.Property(x => x.BranchCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.EncounterCode).HasMaxLength(50);
            b.Property(x => x.PatientName).HasMaxLength(200);
            b.Property(x => x.FormCode).IsRequired().HasMaxLength(60);
            b.Property(x => x.FormTitle).IsRequired().HasMaxLength(250);
            b.Property(x => x.SignerName).IsRequired().HasMaxLength(200);
            b.Property(x => x.SignerRelation).HasMaxLength(80);
            b.Property(x => x.SignatureHash).IsRequired().HasMaxLength(128);
            b.Property(x => x.CollectedByStaffName).HasMaxLength(150);

            b.HasIndex(x => new { x.BranchId, x.EncounterId, x.FormType });
            b.HasIndex(x => new { x.BranchId, x.SignedAt });
        });
    }

    public static void ConfigureWave6PharmacyAndDispense(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Warehouse>(b =>
        {
            b.ToTable("AppWarehouses");
            b.HasKey(x => x.Id);
            b.Property(x => x.WarehouseCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.WarehouseName).IsRequired().HasMaxLength(250);
            b.Property(x => x.Location).HasMaxLength(250);
            b.Property(x => x.BranchCode).HasMaxLength(50);

            b.HasIndex(x => x.WarehouseCode);
            b.HasIndex(x => x.BranchId);
        });

        modelBuilder.Entity<StockLotExpiry>(b =>
        {
            b.ToTable("AppStockLotExpiries");
            b.HasKey(x => x.Id);
            b.Property(x => x.DrugCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.DrugName).IsRequired().HasMaxLength(250);
            b.Property(x => x.LotNumber).IsRequired().HasMaxLength(50);
            b.Property(x => x.QuantityOnHand).HasPrecision(18, 2);
            b.Property(x => x.QuantityReserved).HasPrecision(18, 2);
            b.Property(x => x.Unit).IsRequired().HasMaxLength(50);
            b.Property(x => x.InwardUnitPrice).HasPrecision(18, 2);
            b.Property(x => x.SellingUnitPrice).HasPrecision(18, 2);
            b.Property(x => x.BranchCode).HasMaxLength(50);

            b.HasIndex(x => x.WarehouseId);
            b.HasIndex(x => x.DrugId);
            b.HasIndex(x => x.LotNumber);
            b.HasIndex(x => x.ExpiryDate);
            b.HasIndex(x => x.BranchId);

            b.HasOne(x => x.Warehouse)
             .WithMany()
             .HasForeignKey(x => x.WarehouseId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MedicationDispense>(b =>
        {
            b.ToTable("AppMedicationDispenses");
            b.HasKey(x => x.Id);
            b.Property(x => x.DispenseCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.EncounterCode).HasMaxLength(50);
            b.Property(x => x.PatientName).IsRequired().HasMaxLength(150);
            b.Property(x => x.PharmacistName).IsRequired().HasMaxLength(150);
            b.Property(x => x.TotalAmount).HasPrecision(18, 2);
            b.Property(x => x.Notes).HasMaxLength(500);
            b.Property(x => x.BranchCode).HasMaxLength(50);

            b.HasIndex(x => x.PrescriptionId);
            b.HasIndex(x => x.EncounterId);
            b.HasIndex(x => x.PatientId);
            b.HasIndex(x => x.PharmacistId);
            b.HasIndex(x => x.BranchId);

            b.HasMany(x => x.Items)
             .WithOne(i => i.MedicationDispense)
             .HasForeignKey(i => i.MedicationDispenseId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MedicationDispenseItem>(b =>
        {
            b.ToTable("AppMedicationDispenseItems");
            b.HasKey(x => x.Id);
            b.Property(x => x.DrugName).IsRequired().HasMaxLength(250);
            b.Property(x => x.LotNumber).IsRequired().HasMaxLength(50);
            b.Property(x => x.QuantityDispensed).HasPrecision(18, 2);
            b.Property(x => x.Unit).IsRequired().HasMaxLength(50);
            b.Property(x => x.UnitPrice).HasPrecision(18, 2);
            b.Property(x => x.TotalPrice).HasPrecision(18, 2);

            b.HasIndex(x => x.MedicationDispenseId);
            b.HasIndex(x => x.PrescriptionItemId);
            b.HasIndex(x => x.DrugId);
            b.HasIndex(x => x.StockLotExpiryId);
        });

        modelBuilder.Entity<StockTransaction>(b =>
        {
            b.ToTable("AppStockTransactions");
            b.HasKey(x => x.Id);
            b.Property(x => x.DrugName).IsRequired().HasMaxLength(250);
            b.Property(x => x.LotNumber).IsRequired().HasMaxLength(50);
            b.Property(x => x.Quantity).HasPrecision(18, 2);
            b.Property(x => x.UnitPrice).HasPrecision(18, 2);
            b.Property(x => x.BalanceAfter).HasPrecision(18, 2);
            b.Property(x => x.ReferenceDocNo).IsRequired().HasMaxLength(100);
            b.Property(x => x.BranchCode).HasMaxLength(50);

            b.HasIndex(x => x.WarehouseId);
            b.HasIndex(x => x.DrugId);
            b.HasIndex(x => x.StockLotExpiryId);
            b.HasIndex(x => x.TransactionType);
            b.HasIndex(x => x.CreatedAt);
            b.HasIndex(x => x.BranchId);
        });

        modelBuilder.Entity<GppRetailSale>(b =>
        {
            b.ToTable("AppGppRetailSales");
            b.HasKey(x => x.Id);
            b.Property(x => x.InvoiceCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.CustomerName).HasMaxLength(150);
            b.Property(x => x.CustomerPhone).HasMaxLength(50);
            b.Property(x => x.PharmacistName).IsRequired().HasMaxLength(150);
            b.Property(x => x.TotalAmount).HasPrecision(18, 2);
            b.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            b.Property(x => x.FinalAmount).HasPrecision(18, 2);
            b.Property(x => x.NationalSyncCode).HasMaxLength(100);
            b.Property(x => x.ElectronicInvoiceNumber).HasMaxLength(50);
            b.Property(x => x.BranchCode).HasMaxLength(50);

            b.HasIndex(x => x.ReceiptId);
            b.HasIndex(x => x.ElectronicInvoiceId);
            b.Property(x => x.ElectronicInvoiceNumber).HasMaxLength(30);

            b.HasIndex(x => x.ReceiptId);
            b.HasIndex(x => x.ElectronicInvoiceId);

            b.HasIndex(x => x.WarehouseId);
            b.HasIndex(x => x.PharmacistId);
            b.HasIndex(x => x.SaleDate);
            b.HasIndex(x => x.BranchId);

            b.HasMany(x => x.Items)
             .WithOne(i => i.GppRetailSale)
             .HasForeignKey(i => i.GppRetailSaleId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GppRetailSaleItem>(b =>
        {
            b.ToTable("AppGppRetailSaleItems");
            b.HasKey(x => x.Id);
            b.Property(x => x.DrugName).IsRequired().HasMaxLength(250);
            b.Property(x => x.LotNumber).IsRequired().HasMaxLength(50);
            b.Property(x => x.Quantity).HasPrecision(18, 2);
            b.Property(x => x.Unit).IsRequired().HasMaxLength(50);
            b.Property(x => x.UnitPrice).HasPrecision(18, 2);
            b.Property(x => x.TotalPrice).HasPrecision(18, 2);

            b.HasIndex(x => x.GppRetailSaleId);
            b.HasIndex(x => x.DrugId);
            b.HasIndex(x => x.StockLotExpiryId);
        });
    }

    public static void ConfigureIdentityAndRbac(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>(b =>
        {
            b.ToTable("AppUsers");
            b.HasKey(x => x.Id);
            b.Property(x => x.BranchCode).HasMaxLength(50);
            b.Property(x => x.UserName).IsRequired().HasMaxLength(150);
            b.Property(x => x.FullName).IsRequired().HasMaxLength(150);
            b.Property(x => x.Role).IsRequired().HasMaxLength(50);
            b.Property(x => x.PasswordHash).HasMaxLength(256);

            b.HasIndex(x => x.UserName);
            b.HasIndex(x => x.StaffId);
            b.HasOne(x => x.Staff)
             .WithMany()
             .HasForeignKey(x => x.StaffId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RolePermissionRule>(b =>
        {
            b.ToTable("AppRolePermissionRules");
            b.HasKey(x => x.Id);
            b.Property(x => x.RoleCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.ModuleKey).IsRequired().HasMaxLength(80);
            b.Property(x => x.TabKey).IsRequired().HasMaxLength(80);

            b.HasIndex(x => new { x.BranchId, x.RoleCode, x.ActorGroup, x.TabKey }).IsUnique();
        });

        modelBuilder.Entity<UserPermissionOverride>(b =>
        {
            b.ToTable("AppUserPermissionOverrides");
            b.HasKey(x => x.Id);
            b.Property(x => x.TabKey).IsRequired().HasMaxLength(80);
            b.Property(x => x.Reason).HasMaxLength(500);

            b.HasIndex(x => new { x.UserId, x.BranchId, x.TabKey }).IsUnique();
        });

        modelBuilder.Entity<ConclusionAdviceDictionary>(b =>
        {
            b.ToTable("AppConclusionAdviceDictionaries");
            b.HasKey(x => x.Id);
            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.ContentVi).IsRequired().HasMaxLength(1000);
            b.Property(x => x.ContentEn).HasMaxLength(1000);
            b.Property(x => x.SpecialtyCode).HasMaxLength(50);

            b.HasIndex(x => new { x.ItemType, x.Code }).IsUnique();
        });
    }

    public static void ConfigureWave2LisRisWorkstationAndGateway(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LabPanicThreshold>(b =>
        {
            b.ToTable("AppLabPanicThresholds");
            b.HasKey(x => x.Id);
            b.Property(x => x.BranchCode).HasMaxLength(50);
            b.Property(x => x.TestCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.TestName).IsRequired().HasMaxLength(200);
            b.Property(x => x.Unit).HasMaxLength(50);
            b.Property(x => x.PanicLow).HasPrecision(18, 4);
            b.Property(x => x.PanicHigh).HasPrecision(18, 4);

            b.HasIndex(x => new { x.BranchId, x.TestCode, x.Gender });
            b.HasIndex(x => x.BranchId);
        });

        modelBuilder.Entity<DiagnosticAuditLog>(b =>
        {
            b.ToTable("AppDiagnosticAuditLogs");
            b.HasKey(x => x.Id);
            b.Property(x => x.BranchCode).HasMaxLength(50);
            b.Property(x => x.EntityType).IsRequired().HasMaxLength(80);
            b.Property(x => x.ActorName).IsRequired().HasMaxLength(150);
            b.Property(x => x.ActorRole).IsRequired().HasMaxLength(80);
            b.Property(x => x.ApprovedByName).HasMaxLength(150);
            b.Property(x => x.Reason).HasMaxLength(1000);
            b.Property(x => x.MaskedPiiSummary).HasMaxLength(1000);

            b.HasIndex(x => x.EncounterId);
            b.HasIndex(x => x.EntityId);
            b.HasIndex(x => x.Action);
            b.HasIndex(x => x.TimestampUtc);
            b.HasIndex(x => x.BranchId);
        });

        modelBuilder.Entity<LisPartnerConfig>(b =>
        {
            b.ToTable("AppLisPartnerConfigs");
            b.HasKey(x => x.Id);
            b.Property(x => x.BranchCode).HasMaxLength(50);
            b.Property(x => x.PartnerCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.PartnerName).IsRequired().HasMaxLength(200);
            b.Property(x => x.ConnectionConfigEncrypted).HasMaxLength(2000);

            b.HasIndex(x => new { x.BranchId, x.PartnerCode }).IsUnique();
            b.HasIndex(x => x.BranchId);
        });

        modelBuilder.Entity<ExternalServiceMapping>(b =>
        {
            b.ToTable("AppExternalServiceMappings");
            b.HasKey(x => x.Id);
            b.Property(x => x.BranchCode).HasMaxLength(50);
            b.Property(x => x.ScmServiceCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.PartnerOrderCode).IsRequired().HasMaxLength(80);
            b.Property(x => x.PartnerTestCode).IsRequired().HasMaxLength(80);
            b.Property(x => x.PartnerSampleType).HasMaxLength(50);
            b.Property(x => x.StandardUnit).HasMaxLength(50);
            b.Property(x => x.ConversionFactor).HasPrecision(18, 6);

            b.HasIndex(x => new { x.PartnerId, x.ScmServiceCode });
            b.HasIndex(x => new { x.PartnerId, x.PartnerTestCode });
            b.HasIndex(x => x.BranchId);
        });

        modelBuilder.Entity<LisIntegrationOutbox>(b =>
        {
            b.ToTable("AppLisIntegrationOutbox");
            b.HasKey(x => x.Id);
            b.Property(x => x.BranchCode).HasMaxLength(50);
            b.Property(x => x.IdempotencyKey).IsRequired().HasMaxLength(150);
            b.Property(x => x.PayloadJson).IsRequired();
            b.Property(x => x.LastError).HasMaxLength(2000);

            b.HasIndex(x => x.IdempotencyKey).IsUnique();
            b.HasIndex(x => new { x.Status, x.NextRetryAt });
            b.HasIndex(x => x.MedicalOrderItemId);
            b.HasIndex(x => x.BranchId);
        });
    }
}
