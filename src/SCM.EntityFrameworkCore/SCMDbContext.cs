using Microsoft.EntityFrameworkCore;
using SCM.Domain.B2B;
using SCM.Domain.MasterData;
using SCM.Domain.MasterData.Gateways;
using SCM.Domain.MasterData.NationalRef;
using SCM.Domain.Phlebotomy;

namespace SCM.EntityFrameworkCore;

public class SCMDbContext : DbContext
{
    // ==========================================
    // 1. Core Internal Master Data (VSC Legacy)
    // ==========================================
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<ClinicRoom> ClinicRooms => Set<ClinicRoom>();
    public DbSet<ServiceGroup> ServiceGroups => Set<ServiceGroup>();
    public DbSet<MedicalService> MedicalServices => Set<MedicalService>();
    public DbSet<ActiveIngredient> ActiveIngredients => Set<ActiveIngredient>();
    public DbSet<Drug> Drugs => Set<Drug>();
    public DbSet<Staff> Staffs => Set<Staff>();
    public DbSet<Icd10> Icd10s => Set<Icd10>();
    public DbSet<HealthcareFacility> HealthcareFacilities => Set<HealthcareFacility>();

    // ==============================================================
    // 2. National Gateways & LIS/Drug Mappings (V2 Infrastructural)
    // ==============================================================
    public DbSet<NationalGatewayConfig> NationalGatewayConfigs => Set<NationalGatewayConfig>();
    public DbSet<PractitionerLicense> PractitionerLicenses => Set<PractitionerLicense>();
    public DbSet<ServiceM3Mapping> ServiceM3Mappings => Set<ServiceM3Mapping>();
    public DbSet<DrugMapping> DrugMappings => Set<DrugMapping>();

    // ====================================================================
    // 3. 22 National Reference Physical Tables (M3 Catalogs & BYT Dược)
    // ====================================================================
    public DbSet<AppRef_ICD> AppRef_ICDs => Set<AppRef_ICD>();
    public DbSet<AppRef_M3DoiTuongKham> AppRef_M3DoiTuongKhams => Set<AppRef_M3DoiTuongKham>();
    public DbSet<AppRef_DiaDiemKham> AppRef_DiaDiemKhams => Set<AppRef_DiaDiemKham>();
    public DbSet<AppRef_GioiTinh> AppRef_GioiTinhs => Set<AppRef_GioiTinh>();
    public DbSet<AppRef_DanToc> AppRef_DanTocs => Set<AppRef_DanToc>();
    public DbSet<AppRef_NhomMau> AppRef_NhomMaus => Set<AppRef_NhomMau>();
    public DbSet<AppRef_YeuToNhomMau> AppRef_YeuToNhomMaus => Set<AppRef_YeuToNhomMau>();
    public DbSet<AppRef_TinhThanh> AppRef_TinhThanhs => Set<AppRef_TinhThanh>();
    public DbSet<AppRef_XaPhuong> AppRef_XaPhuongs => Set<AppRef_XaPhuong>();
    public DbSet<AppRef_NgheNghiep> AppRef_NgheNghieps => Set<AppRef_NgheNghiep>();
    public DbSet<AppRef_NoiCongTac> AppRef_NoiCongTacs => Set<AppRef_NoiCongTac>();
    public DbSet<AppRef_NguonChiTra> AppRef_NguonChiTras => Set<AppRef_NguonChiTra>();
    public DbSet<AppRef_ChiTraChiTiet> AppRef_ChiTraChiTiets => Set<AppRef_ChiTraChiTiet>();
    public DbSet<AppRef_TrangThaiXetNghiem> AppRef_TrangThaiXetNghiems => Set<AppRef_TrangThaiXetNghiem>();
    public DbSet<AppRef_BenhGiaDinh> AppRef_BenhGiaDinhs => Set<AppRef_BenhGiaDinh>();
    public DbSet<AppRef_YesNo> AppRef_YesNos => Set<AppRef_YesNo>();
    public DbSet<AppRef_PhanLoaiSucKhoe> AppRef_PhanLoaiSucKhoes => Set<AppRef_PhanLoaiSucKhoe>();
    public DbSet<AppRef_TinhTrangRang> AppRef_TinhTrangRangs => Set<AppRef_TinhTrangRang>();
    public DbSet<AppRef_KetQuaMaTuy> AppRef_KetQuaMaTuys => Set<AppRef_KetQuaMaTuy>();
    public DbSet<AppRef_KetLuanDeNghi> AppRef_KetLuanDeNghis => Set<AppRef_KetLuanDeNghi>();
    public DbSet<AppRef_DanhMucMeta> AppRef_DanhMucMetas => Set<AppRef_DanhMucMeta>();
    public DbSet<AppRef_DuocQuocGia> AppRef_DuocQuocGias => Set<AppRef_DuocQuocGia>();

    // ====================================================================
    // 4. Wave 2: B2B Contracts & Package Management
    // ====================================================================
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyContract> CompanyContracts => Set<CompanyContract>();
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<PackageTargetGroup> PackageTargetGroups => Set<PackageTargetGroup>();
    public DbSet<PackageService> PackageServices => Set<PackageService>();
    public DbSet<CompanyEmployee> CompanyEmployees => Set<CompanyEmployee>();

    // ====================================================================
    // 5. Wave 3: Pre-Exam Phlebotomy & Specimen Management
    // ====================================================================
    public DbSet<PhlebotomyBatch> PhlebotomyBatches => Set<PhlebotomyBatch>();
    public DbSet<BatchEmployeeMapping> BatchEmployeeMappings => Set<BatchEmployeeMapping>();
    public DbSet<SpecimenTube> SpecimenTubes => Set<SpecimenTube>();
    public DbSet<ColdChainManifest> ColdChainManifests => Set<ColdChainManifest>();

    public SCMDbContext(DbContextOptions<SCMDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Wave 2 B2B & Package configurations
        modelBuilder.ConfigureB2BAndPackages();

        // Wave 3 Phlebotomy & Cold Chain configurations
        modelBuilder.ConfigurePhlebotomy();

        // -------------------------------------------------------------
        // 1. Internal Core Master Data Configurations
        // -------------------------------------------------------------
        modelBuilder.Entity<Department>(b =>
        {
            b.ToTable("AppDepartments");
            b.HasKey(x => x.Id);
            b.Property(x => x.DepartmentCode).IsRequired().HasMaxLength(20);
            b.Property(x => x.DepartmentName).IsRequired().HasMaxLength(150);
            b.HasIndex(x => x.DepartmentCode).IsUnique();
        });

        modelBuilder.Entity<ClinicRoom>(b =>
        {
            b.ToTable("AppClinicRooms");
            b.HasKey(x => x.Id);
            b.Property(x => x.RoomCode).IsRequired().HasMaxLength(20);
            b.Property(x => x.RoomName).IsRequired().HasMaxLength(150);
            b.Property(x => x.StandardExamFee).HasPrecision(18, 2);
            b.HasIndex(x => x.RoomCode).IsUnique();

            b.HasOne(x => x.Department)
             .WithMany(d => d.Rooms)
             .HasForeignKey(x => x.DepartmentId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ServiceGroup>(b =>
        {
            b.ToTable("AppServiceGroups");
            b.HasKey(x => x.Id);
            b.Property(x => x.GroupCode).IsRequired().HasMaxLength(20);
            b.Property(x => x.GroupName).IsRequired().HasMaxLength(150);
            b.HasIndex(x => x.GroupCode).IsUnique();
        });

        modelBuilder.Entity<MedicalService>(b =>
        {
            b.ToTable("AppMedicalServices");
            b.HasKey(x => x.Id);
            b.Property(x => x.ServiceCode).IsRequired().HasMaxLength(30);
            b.Property(x => x.ServiceName).IsRequired().HasMaxLength(255);
            b.Property(x => x.Unit).IsRequired().HasMaxLength(30);
            b.Property(x => x.StandardPrice).HasPrecision(18, 2);
            b.Property(x => x.InsurancePrice).HasPrecision(18, 2);
            b.Property(x => x.CorporatePrice).HasPrecision(18, 2);
            b.Property(x => x.DoctorCommissionRate).HasPrecision(5, 4);
            b.Property(x => x.MinistryEquivalentCode).HasMaxLength(50);
            b.Property(x => x.MinistryEquivalentName).HasMaxLength(255);
            b.Property(x => x.DecisionNumber).HasMaxLength(50);
            b.HasIndex(x => x.ServiceCode).IsUnique();

            b.HasOne(x => x.ServiceGroup)
             .WithMany(g => g.Services)
             .HasForeignKey(x => x.ServiceGroupId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ActiveIngredient>(b =>
        {
            b.ToTable("AppActiveIngredients");
            b.HasKey(x => x.Id);
            b.Property(x => x.IngredientCode).IsRequired().HasMaxLength(30);
            b.Property(x => x.IngredientName).IsRequired().HasMaxLength(255);
            b.Property(x => x.AtcCode).HasMaxLength(20);
            b.HasIndex(x => x.IngredientCode).IsUnique();
        });

        modelBuilder.Entity<Drug>(b =>
        {
            b.ToTable("AppDrugs");
            b.HasKey(x => x.Id);
            b.Property(x => x.DrugCode).IsRequired().HasMaxLength(30);
            b.Property(x => x.DrugName).IsRequired().HasMaxLength(255);
            b.Property(x => x.Content).IsRequired().HasMaxLength(100);
            b.Property(x => x.Unit).IsRequired().HasMaxLength(30);
            b.Property(x => x.StandardPrice).HasPrecision(18, 2);
            b.Property(x => x.InsurancePrice).HasPrecision(18, 2);
            b.Property(x => x.CostPrice).HasPrecision(18, 2);
            b.Property(x => x.RegistrationNumber).HasMaxLength(50);
            b.Property(x => x.NationalDrugCode).HasMaxLength(50);
            b.Property(x => x.Manufacturer).HasMaxLength(150);
            b.Property(x => x.CountryOfOrigin).HasMaxLength(100);
            b.HasIndex(x => x.DrugCode).IsUnique();

            b.HasOne(x => x.ActiveIngredient)
             .WithMany(i => i.Drugs)
             .HasForeignKey(x => x.ActiveIngredientId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Staff>(b =>
        {
            b.ToTable("AppStaffs");
            b.HasKey(x => x.Id);
            b.Property(x => x.StaffCode).IsRequired().HasMaxLength(20);
            b.Property(x => x.FullName).IsRequired().HasMaxLength(150);
            b.Property(x => x.PracticeCertificateCode).HasMaxLength(50);
            b.Property(x => x.Specialty).HasMaxLength(100);
            b.Property(x => x.AcademicTitle).HasMaxLength(50);
            b.Property(x => x.PhoneNumber).HasMaxLength(20);
            b.Property(x => x.Email).HasMaxLength(100);
            b.Property(x => x.IdentityNumber).HasMaxLength(20);
            b.HasIndex(x => x.StaffCode).IsUnique();
        });

        modelBuilder.Entity<Icd10>(b =>
        {
            b.ToTable("AppIcd10s");
            b.HasKey(x => x.Id);
            b.Property(x => x.IcdCode).IsRequired().HasMaxLength(10);
            b.Property(x => x.DiseaseNameVi).IsRequired().HasMaxLength(255);
            b.Property(x => x.DiseaseNameEn).HasMaxLength(255);
            b.Property(x => x.ChapterCode).HasMaxLength(10);
            b.HasIndex(x => x.IcdCode).IsUnique();
        });

        modelBuilder.Entity<HealthcareFacility>(b =>
        {
            b.ToTable("AppHealthcareFacilities");
            b.HasKey(x => x.Id);
            b.Property(x => x.FacilityCode).IsRequired().HasMaxLength(10);
            b.Property(x => x.FacilityName).IsRequired().HasMaxLength(255);
            b.Property(x => x.ProvinceCode).IsRequired().HasMaxLength(10);
            b.Property(x => x.TechnicalLevel).HasMaxLength(50);
            b.HasIndex(x => x.FacilityCode).IsUnique();
        });

        // -------------------------------------------------------------
        // 2. National Gateways & Mappings Configurations
        // -------------------------------------------------------------
        modelBuilder.Entity<NationalGatewayConfig>(b =>
        {
            b.ToTable("NationalGatewayConfigs");
            b.HasKey(x => x.Id);
            b.Property(x => x.FacilityCode).IsRequired().HasMaxLength(20);
            b.Property(x => x.BaseUrl).IsRequired().HasMaxLength(255);
            b.Property(x => x.ClientId).IsRequired().HasMaxLength(100);
            b.Property(x => x.ClientSecret).IsRequired().HasMaxLength(255);
            b.Property(x => x.SignCertSerial).HasMaxLength(100);
            b.HasIndex(x => new { x.FacilityCode, x.GatewayType }).IsUnique();
        });

        modelBuilder.Entity<PractitionerLicense>(b =>
        {
            b.ToTable("PractitionerLicenses");
            b.HasKey(x => x.Id);
            b.Property(x => x.LicenseNumber).IsRequired().HasMaxLength(50);
            b.Property(x => x.ScopeOfPractice).IsRequired().HasMaxLength(255);
            b.Property(x => x.DigitalCertSerial).HasMaxLength(100);
            b.Property(x => x.CloudHsmUserId).HasMaxLength(100);
            b.HasIndex(x => x.LicenseNumber).IsUnique();
            b.HasIndex(x => x.StaffId);

            b.HasOne(x => x.Staff)
             .WithMany()
             .HasForeignKey(x => x.StaffId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ServiceM3Mapping>(b =>
        {
            b.ToTable("ServiceM3Mappings");
            b.HasKey(x => x.Id);
            b.Property(x => x.LabParameterCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.M3JsonKey).IsRequired().HasMaxLength(100);
            b.Property(x => x.ConversionFormula).HasMaxLength(100);
            b.Property(x => x.StandardUnit).IsRequired().HasMaxLength(50);
            b.HasIndex(x => new { x.ServiceId, x.LabParameterCode }).IsUnique();
            b.HasIndex(x => x.M3JsonKey);

            b.HasOne(x => x.Service)
             .WithMany()
             .HasForeignKey(x => x.ServiceId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DrugMapping>(b =>
        {
            b.ToTable("DrugMappings");
            b.HasKey(x => x.Id);
            b.Property(x => x.VerifiedBy).HasMaxLength(100);
            b.HasIndex(x => x.InternalDrugId).IsUnique();
            b.HasIndex(x => x.NationalDrugId);

            b.HasOne(x => x.InternalDrug)
             .WithMany()
             .HasForeignKey(x => x.InternalDrugId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.NationalDrug)
             .WithMany()
             .HasForeignKey(x => x.NationalDrugId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // -------------------------------------------------------------
        // 3. 22 National Reference Relational Tables Configurations
        // -------------------------------------------------------------
        modelBuilder.Entity<AppRef_ICD>(b =>
        {
            b.ToTable("AppRef_ICD");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.MaICD).IsRequired().HasMaxLength(20);
            b.Property(x => x.Name).IsRequired().HasMaxLength(500);
            b.HasIndex(x => x.MaICD).IsUnique();
        });

        modelBuilder.Entity<AppRef_M3DoiTuongKham>(b =>
        {
            b.ToTable("AppRef_M3DoiTuongKham");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.Name).IsRequired().HasMaxLength(255);
        });

        modelBuilder.Entity<AppRef_DiaDiemKham>(b =>
        {
            b.ToTable("AppRef_DiaDiemKham");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.Name).IsRequired().HasMaxLength(255);
        });

        modelBuilder.Entity<AppRef_GioiTinh>(b =>
        {
            b.ToTable("AppRef_GioiTinh");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.Name).IsRequired().HasMaxLength(50);
            b.Property(x => x.Code).HasMaxLength(10);
        });

        modelBuilder.Entity<AppRef_DanToc>(b =>
        {
            b.ToTable("AppRef_DanToc");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.Name).IsRequired().HasMaxLength(100);
            b.Property(x => x.Code).HasMaxLength(20);
        });

        modelBuilder.Entity<AppRef_NhomMau>(b =>
        {
            b.ToTable("AppRef_NhomMau");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.Name).IsRequired().HasMaxLength(50);
            b.Property(x => x.Code).HasMaxLength(10);
        });

        modelBuilder.Entity<AppRef_YeuToNhomMau>(b =>
        {
            b.ToTable("AppRef_YeuToNhomMau");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.Name).IsRequired().HasMaxLength(50);
            b.Property(x => x.Code).HasMaxLength(10);
        });

        modelBuilder.Entity<AppRef_TinhThanh>(b =>
        {
            b.ToTable("AppRef_TinhThanh");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.CityCode).IsRequired().HasMaxLength(10);
            b.Property(x => x.Name).IsRequired().HasMaxLength(255);
            b.HasIndex(x => x.CityCode).IsUnique();
        });

        modelBuilder.Entity<AppRef_XaPhuong>(b =>
        {
            b.ToTable("AppRef_XaPhuong");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.WardCode).IsRequired().HasMaxLength(10);
            b.Property(x => x.CityCode).HasMaxLength(10);
            b.Property(x => x.Name).IsRequired().HasMaxLength(255);
            b.HasIndex(x => x.WardCode);
            b.HasIndex(x => x.CityId);

            b.HasOne(x => x.City)
             .WithMany(c => c.Wards)
             .HasForeignKey(x => x.CityId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AppRef_NgheNghiep>(b =>
        {
            b.ToTable("AppRef_NgheNghiep");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.Code).HasMaxLength(20);
            b.Property(x => x.Name).IsRequired().HasMaxLength(255);
            b.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<AppRef_NoiCongTac>(b =>
        {
            b.ToTable("AppRef_NoiCongTac");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.Name).IsRequired().HasMaxLength(500);
        });

        modelBuilder.Entity<AppRef_NguonChiTra>(b =>
        {
            b.ToTable("AppRef_NguonChiTra");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.Name).IsRequired().HasMaxLength(255);
            b.Property(x => x.Code).HasMaxLength(20);
        });

        modelBuilder.Entity<AppRef_ChiTraChiTiet>(b =>
        {
            b.ToTable("AppRef_ChiTraChiTiet");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.Name).IsRequired().HasMaxLength(255);
            b.Property(x => x.Code).HasMaxLength(20);
        });

        modelBuilder.Entity<AppRef_TrangThaiXetNghiem>(b =>
        {
            b.ToTable("AppRef_TrangThaiXetNghiem");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.Name).IsRequired().HasMaxLength(150);
        });

        modelBuilder.Entity<AppRef_BenhGiaDinh>(b =>
        {
            b.ToTable("AppRef_BenhGiaDinh");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.Name).IsRequired().HasMaxLength(255);
        });

        modelBuilder.Entity<AppRef_YesNo>(b =>
        {
            b.ToTable("AppRef_YesNo");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.Name).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<AppRef_PhanLoaiSucKhoe>(b =>
        {
            b.ToTable("AppRef_PhanLoaiSucKhoe");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.Name).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<AppRef_TinhTrangRang>(b =>
        {
            b.ToTable("AppRef_TinhTrangRang");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.Code).HasMaxLength(10);
            b.Property(x => x.Name).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<AppRef_KetQuaMaTuy>(b =>
        {
            b.ToTable("AppRef_KetQuaMaTuy");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.Name).IsRequired().HasMaxLength(50);
            b.Property(x => x.Code).HasMaxLength(10);
        });

        modelBuilder.Entity<AppRef_KetLuanDeNghi>(b =>
        {
            b.ToTable("AppRef_KetLuanDeNghi");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.Name).IsRequired().HasMaxLength(255);
            b.Property(x => x.Code).HasMaxLength(20);
        });

        modelBuilder.Entity<AppRef_DanhMucMeta>(b =>
        {
            b.ToTable("AppRef_DanhMucMeta");
            b.HasKey(x => x.CategoryCode);
            b.Property(x => x.CategoryCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.CategoryName).IsRequired().HasMaxLength(255);
            b.Property(x => x.SourceFile).HasMaxLength(100);
        });

        modelBuilder.Entity<AppRef_DuocQuocGia>(b =>
        {
            b.ToTable("AppRef_DuocQuocGia");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.MaThuoc).IsRequired().HasMaxLength(30);
            b.Property(x => x.TenHoatChat).IsRequired().HasMaxLength(255);
            b.Property(x => x.HamLuong).HasMaxLength(100);
            b.Property(x => x.DuongDung).HasMaxLength(100);
            b.Property(x => x.SoDangKy).HasMaxLength(50);
            b.HasIndex(x => x.MaThuoc).IsUnique();
        });
    }
}
