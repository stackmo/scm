using Microsoft.EntityFrameworkCore;
using SCM.Domain.B2B;
using SCM.Domain.Phlebotomy;
using SCM.Domain.Queueing;

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
        // 3. Package (Gói KSK đoàn)
        // -------------------------------------------------------------
        modelBuilder.Entity<Package>(b =>
        {
            b.ToTable("AppPackages");
            b.HasKey(x => x.Id);
            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.Name).IsRequired().HasMaxLength(250);
            b.Property(x => x.NationalTemplateCode).HasMaxLength(20);

            b.HasIndex(x => x.Code).IsUnique();

            b.HasOne(x => x.Company)
             .WithMany(c => c.Packages)
             .HasForeignKey(x => x.CompanyId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Contract)
             .WithMany(c => c.Packages)
             .HasForeignKey(x => x.ContractId)
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
        // 3. SpecimenTube (Ống nghiệm lấy mẫu ngoại viện)
        // -------------------------------------------------------------
        modelBuilder.Entity<SpecimenTube>(b =>
        {
            b.ToTable("AppSpecimenTubes");
            b.HasKey(x => x.Id);
            b.Property(x => x.TubeBarcode).IsRequired().HasMaxLength(100);
            b.Property(x => x.CapColor).HasMaxLength(50);
            b.Property(x => x.TargetTests).HasMaxLength(500);
            b.Property(x => x.CollectorId).HasMaxLength(100);
            b.Property(x => x.RejectionReason).HasMaxLength(500);

            b.HasIndex(x => x.TubeBarcode).IsUnique();
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.TubeType);

            b.HasOne(x => x.BatchEmployeeMapping)
                .WithMany(m => m.SpecimenTubes)
                .HasForeignKey(x => x.BatchEmployeeMappingId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
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
            b.Property(x => x.ManifestCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.HandoverStaff).IsRequired().HasMaxLength(150);
            b.Property(x => x.CourierStaff).IsRequired().HasMaxLength(150);
            b.Property(x => x.LabReceiverStaff).HasMaxLength(150);
            b.Property(x => x.DepartureTemperature).HasPrecision(18, 2);
            b.Property(x => x.ArrivalTemperature).HasPrecision(18, 2);
            b.Property(x => x.Notes).HasMaxLength(1000);

            b.HasIndex(x => x.ManifestCode).IsUnique();
            b.HasIndex(x => x.Status);

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
    }
}
