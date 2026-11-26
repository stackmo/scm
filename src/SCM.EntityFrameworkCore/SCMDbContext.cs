using Microsoft.EntityFrameworkCore;
using SCM.Domain.B2B;
using SCM.Domain.Branches;
using SCM.Domain.Billing;
using SCM.Domain.Clinical;
using SCM.Domain.Diagnostics;
using SCM.Domain.Diagnostics.Gateway;
using SCM.Domain.Grading;
using SCM.Domain.Identity;
using SCM.Domain.Outbox;
using SCM.Domain.Pharmacy;
using SCM.Domain.Prescription;
using SCM.Domain.MasterData;
using SCM.Domain.MasterData.Gateways;
using SCM.Domain.MasterData.NationalRef;
using SCM.Domain.Phlebotomy;
using SCM.Domain.Queueing;
using SCM.Domain.Reception;

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
    public DbSet<MedicalSupply> MedicalSupplies => Set<MedicalSupply>();
    public DbSet<Staff> Staffs => Set<Staff>();
    public DbSet<Icd10> Icd10s => Set<Icd10>();
    public DbSet<HealthcareFacility> HealthcareFacilities => Set<HealthcareFacility>();
    public DbSet<BranchServiceAvailability> BranchServiceAvailabilities => Set<BranchServiceAvailability>();

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

    // ====================================================================
    // 6. Wave 4: Clinic Reception & Smart Queueing (Workflow 03B)
    // ====================================================================
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Encounter> Encounters => Set<Encounter>();
    public DbSet<QueueTicket> QueueTickets => Set<QueueTicket>();
    public DbSet<QueueAssignment> QueueAssignments => Set<QueueAssignment>();
    public DbSet<ClinicRoomLoad> ClinicRoomLoads => Set<ClinicRoomLoad>();
    public DbSet<ClinicRoomQueue> ClinicRoomQueues => Set<ClinicRoomQueue>();
    public DbSet<QueueSequence> QueueSequences => Set<QueueSequence>();

    // ====================================================================
    // 7. Wave 5: Clinical Examination 8 Specialties & Dental 32 (WF 05)
    // ====================================================================
    public DbSet<ClinicalExamination> ClinicalExaminations => Set<ClinicalExamination>();
    public DbSet<VitalSigns> VitalSigns => Set<VitalSigns>();
    public DbSet<SpecialtyExamDetail> SpecialtyExamDetails => Set<SpecialtyExamDetail>();
    public DbSet<Dental32TeethDetail> Dental32TeethDetails => Set<Dental32TeethDetail>();
    public DbSet<MedicalOrder> MedicalOrders => Set<MedicalOrder>();
    public DbSet<MedicalOrderItem> MedicalOrderItems => Set<MedicalOrderItem>();

    // ====================================================================
    // 8. Wave 6: Diagnostics (LIS/PACS) & National E-Prescription (WF 04, 06)
    // ====================================================================
    public DbSet<LabResultRecord> LabResultRecords => Set<LabResultRecord>();
    public DbSet<DiagnosticImagingRecord> DiagnosticImagingRecords => Set<DiagnosticImagingRecord>();
    public DbSet<CapturedMedia> CapturedMedias => Set<CapturedMedia>();
    public DbSet<ImagingReportTemplate> ImagingReportTemplates => Set<ImagingReportTemplate>();
    public DbSet<LabPanicThreshold> LabPanicThresholds => Set<LabPanicThreshold>();
    public DbSet<DiagnosticAuditLog> DiagnosticAuditLogs => Set<DiagnosticAuditLog>();
    public DbSet<LisPartnerConfig> LisPartnerConfigs => Set<LisPartnerConfig>();
    public DbSet<ExternalServiceMapping> ExternalServiceMappings => Set<ExternalServiceMapping>();
    public DbSet<LisIntegrationOutbox> LisIntegrationOutboxes => Set<LisIntegrationOutbox>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<PrescriptionTemplate> PrescriptionTemplates => Set<PrescriptionTemplate>();
    public DbSet<PrescriptionTemplateItem> PrescriptionTemplateItems => Set<PrescriptionTemplateItem>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<StockLotExpiry> StockLotExpiries => Set<StockLotExpiry>();
    public DbSet<MedicationDispense> MedicationDispenses => Set<MedicationDispense>();
    public DbSet<MedicationDispenseItem> MedicationDispenseItems => Set<MedicationDispenseItem>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<GppRetailSale> GppRetailSales => Set<GppRetailSale>();
    public DbSet<GppRetailSaleItem> GppRetailSaleItems => Set<GppRetailSaleItem>();

    // ====================================================================
    // 9. Wave 7: Physical Fitness Auto-Grading & Outbox M3 191 (WF 07, 08)
    // ====================================================================
    public DbSet<MedicalRecordSummary> MedicalRecordSummaries => Set<MedicalRecordSummary>();
    public DbSet<NationalSyncOutbox> NationalSyncOutboxRecords => Set<NationalSyncOutbox>();
    public DbSet<InvoiceSeries> InvoiceSeries => Set<InvoiceSeries>();
    public DbSet<BillingSequence> BillingSequences => Set<BillingSequence>();
    public DbSet<CashierShift> CashierShifts => Set<CashierShift>();
    public DbSet<ElectronicInvoice> ElectronicInvoices => Set<ElectronicInvoice>();
    public DbSet<PatientConsentSignature> PatientConsentSignatures => Set<PatientConsentSignature>();
    public DbSet<ElectronicInvoiceItem> ElectronicInvoiceItems => Set<ElectronicInvoiceItem>();
    public DbSet<ElectronicInvoiceReceiptLink> ElectronicInvoiceReceiptLinks => Set<ElectronicInvoiceReceiptLink>();

    // ====================================================================
    // 10. Wave 8: Cashier, BHYT QĐ 130, B2B Settlement & E-Invoice (WF 09)
    // ====================================================================
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<ReceiptItem> ReceiptItems => Set<ReceiptItem>();
    public DbSet<RefundReceipt> RefundReceipts => Set<RefundReceipt>();
    public DbSet<CompanySettlement> CompanySettlements => Set<CompanySettlement>();

    // ====================================================================
    // 11. Wave 0 & Wave 1: Multi-Branch & RBAC Identity Foundation
    // ====================================================================
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<StaffBranchAssignment> StaffBranchAssignments => Set<StaffBranchAssignment>();
    public DbSet<AuditLogPII> AuditLogPIIs => Set<AuditLogPII>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<RolePermissionRule> RolePermissionRules => Set<RolePermissionRule>();
    public DbSet<UserPermissionOverride> UserPermissionOverrides => Set<UserPermissionOverride>();
    public DbSet<ConclusionAdviceDictionary> ConclusionAdviceDictionaries => Set<ConclusionAdviceDictionary>();

    private readonly ICurrentBranch _currentBranch;

    public SCMDbContext(DbContextOptions<SCMDbContext> options, ICurrentBranch? currentBranch = null) : base(options)
    {
        _currentBranch = currentBranch ?? new NullCurrentBranch();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Wave 0 Multi-Branch & RBAC configurations
        modelBuilder.ConfigureBranches();
        modelBuilder.ConfigureIdentityAndRbac();

        // Wave 2 B2B & Package configurations
        modelBuilder.ConfigureB2BAndPackages();

        // Wave 3 Phlebotomy & Cold Chain configurations
        modelBuilder.ConfigurePhlebotomy();

        // Wave 4 Queueing & Reception configurations
        modelBuilder.ConfigureQueueing();

        // Wave 5 Clinical Examination & Dental configurations
        modelBuilder.ConfigureClinical();

        // Wave 6 Diagnostics & Prescription configurations
        modelBuilder.ConfigureWave6DiagnosticsAndPrescription();
        modelBuilder.ConfigureWave6PharmacyAndDispense();
        modelBuilder.ConfigureWave2LisRisWorkstationAndGateway();

        // Wave 7 Auto-Grading & Outbox M3 configurations
        modelBuilder.ConfigureWave7AutoGradingAndOutboxM3();

        // Wave 8 Billing & Settlement configurations
        modelBuilder.ConfigureWave8BillingAndSettlement();

        // Wave 0: Multi-Branch Global Query Filter Enforcement (Invariant 3 & Zero Cross-Branch Leakage)
        modelBuilder.Entity<Encounter>().HasQueryFilter(e =>
            _currentBranch.IsAllBranches ||
            (_currentBranch.Code != null && e.BranchCode == _currentBranch.Code) ||
            (_currentBranch.Id != null && (Guid?)e.BranchId == _currentBranch.Id));

        modelBuilder.Entity<MedicalOrder>().HasQueryFilter(e =>
            _currentBranch.IsAllBranches ||
            (_currentBranch.Code != null && e.BranchCode == _currentBranch.Code) ||
            (_currentBranch.Id != null && (Guid?)e.BranchId == _currentBranch.Id));

        modelBuilder.Entity<QueueTicket>().HasQueryFilter(e =>
            _currentBranch.IsAllBranches ||
            (_currentBranch.Code != null && e.BranchCode == _currentBranch.Code) ||
            (_currentBranch.Id != null && (Guid?)e.BranchId == _currentBranch.Id));

        modelBuilder.Entity<ClinicalExamination>().HasQueryFilter(e =>
            _currentBranch.IsAllBranches ||
            (_currentBranch.Code != null && e.BranchCode == _currentBranch.Code) ||
            (_currentBranch.Id != null && (Guid?)e.BranchId == _currentBranch.Id));

        modelBuilder.Entity<PatientConsentSignature>().HasQueryFilter(e =>
            _currentBranch.IsAllBranches ||
            (_currentBranch.Code != null && e.BranchCode == _currentBranch.Code) ||
            (_currentBranch.Id != null && (Guid?)e.BranchId == _currentBranch.Id));

        modelBuilder.Entity<LabResultRecord>().HasQueryFilter(e =>
            _currentBranch.IsAllBranches ||
            (_currentBranch.Code != null && e.BranchCode == _currentBranch.Code) ||
            (_currentBranch.Id != null && (Guid?)e.BranchId == _currentBranch.Id));

        modelBuilder.Entity<DiagnosticImagingRecord>().HasQueryFilter(e =>
            _currentBranch.IsAllBranches ||
            (_currentBranch.Code != null && e.BranchCode == _currentBranch.Code) ||
            (_currentBranch.Id != null && (Guid?)e.BranchId == _currentBranch.Id));

        modelBuilder.Entity<Prescription>().HasQueryFilter(e =>
            _currentBranch.IsAllBranches ||
            (_currentBranch.Code != null && e.BranchCode == _currentBranch.Code) ||
            (_currentBranch.Id != null && (Guid?)e.BranchId == _currentBranch.Id));

        modelBuilder.Entity<Warehouse>().HasQueryFilter(e =>
            _currentBranch.IsAllBranches ||
            (_currentBranch.Code != null && e.BranchCode == _currentBranch.Code) ||
            (_currentBranch.Id != null && (Guid?)e.BranchId == _currentBranch.Id));

        modelBuilder.Entity<StockLotExpiry>().HasQueryFilter(e =>
            _currentBranch.IsAllBranches ||
            (_currentBranch.Code != null && e.BranchCode == _currentBranch.Code) ||
            (_currentBranch.Id != null && (Guid?)e.BranchId == _currentBranch.Id));

        modelBuilder.Entity<MedicationDispense>().HasQueryFilter(e =>
            _currentBranch.IsAllBranches ||
            (_currentBranch.Code != null && e.BranchCode == _currentBranch.Code) ||
            (_currentBranch.Id != null && (Guid?)e.BranchId == _currentBranch.Id));

        modelBuilder.Entity<StockTransaction>().HasQueryFilter(e =>
            _currentBranch.IsAllBranches ||
            (_currentBranch.Code != null && e.BranchCode == _currentBranch.Code) ||
            (_currentBranch.Id != null && (Guid?)e.BranchId == _currentBranch.Id));

        modelBuilder.Entity<GppRetailSale>().HasQueryFilter(e =>
            _currentBranch.IsAllBranches ||
            (_currentBranch.Code != null && e.BranchCode == _currentBranch.Code) ||
            (_currentBranch.Id != null && (Guid?)e.BranchId == _currentBranch.Id));

        modelBuilder.Entity<MedicalRecordSummary>().HasQueryFilter(e =>
            _currentBranch.IsAllBranches ||
            (_currentBranch.Code != null && e.BranchCode == _currentBranch.Code) ||
            (_currentBranch.Id != null && (Guid?)e.BranchId == _currentBranch.Id));

        modelBuilder.Entity<NationalSyncOutbox>().HasQueryFilter(e =>
            _currentBranch.IsAllBranches ||
            (_currentBranch.Code != null && e.BranchCode == _currentBranch.Code) ||
            (_currentBranch.Id != null && (Guid?)e.BranchId == _currentBranch.Id));

        modelBuilder.Entity<Receipt>().HasQueryFilter(e =>
            _currentBranch.IsAllBranches ||
            (_currentBranch.Code != null && e.BranchCode == _currentBranch.Code) ||
            (_currentBranch.Id != null && (Guid?)e.BranchId == _currentBranch.Id));

        modelBuilder.Entity<ReceiptItem>().HasQueryFilter(e =>
            _currentBranch.IsAllBranches ||
            (_currentBranch.Code != null && e.BranchCode == _currentBranch.Code) ||
            (_currentBranch.Id != null && (Guid?)e.BranchId == _currentBranch.Id));

        modelBuilder.Entity<CompanySettlement>().HasQueryFilter(e =>
            _currentBranch.IsAllBranches ||
            (_currentBranch.Code != null && e.BranchCode == _currentBranch.Code) ||
            (_currentBranch.Id != null && (Guid?)e.BranchId == _currentBranch.Id));

        modelBuilder.Entity<CompanyContract>().HasQueryFilter(e =>
            _currentBranch.IsAllBranches ||
            (_currentBranch.Code != null && e.BranchCode == _currentBranch.Code) ||
            (_currentBranch.Id != null && (Guid?)e.BranchId == _currentBranch.Id));

        modelBuilder.Entity<PhlebotomyBatch>().HasQueryFilter(e =>
            _currentBranch.IsAllBranches ||
            (_currentBranch.Code != null && e.BranchCode == _currentBranch.Code) ||
            (_currentBranch.Id != null && (Guid?)e.BranchId == _currentBranch.Id));

        modelBuilder.Entity<ColdChainManifest>().HasQueryFilter(e =>
            _currentBranch.IsAllBranches ||
            (_currentBranch.Code != null && e.BranchCode == _currentBranch.Code) ||
            (_currentBranch.Id != null && (Guid?)e.BranchId == _currentBranch.Id));

        modelBuilder.Entity<SpecimenTube>().HasQueryFilter(e =>
            _currentBranch.IsAllBranches ||
            (_currentBranch.Code != null && e.BranchCode == _currentBranch.Code) ||
            (_currentBranch.Id != null && (Guid?)e.BranchId == _currentBranch.Id));

        modelBuilder.Entity<LabPanicThreshold>().HasQueryFilter(e =>
            _currentBranch.IsAllBranches ||
            (_currentBranch.Code != null && e.BranchCode == _currentBranch.Code) ||
            (_currentBranch.Id != null && (Guid?)e.BranchId == _currentBranch.Id));

        modelBuilder.Entity<DiagnosticAuditLog>().HasQueryFilter(e =>
            _currentBranch.IsAllBranches ||
            (_currentBranch.Code != null && e.BranchCode == _currentBranch.Code) ||
            (_currentBranch.Id != null && (Guid?)e.BranchId == _currentBranch.Id));

        modelBuilder.Entity<InvoiceSeries>().HasQueryFilter(e =>
            _currentBranch.IsAllBranches ||
            (_currentBranch.Code != null && e.BranchCode == _currentBranch.Code) ||
            (_currentBranch.Id != null && (Guid?)e.BranchId == _currentBranch.Id));

        modelBuilder.Entity<CashierShift>().HasQueryFilter(e =>
            _currentBranch.IsAllBranches ||
            (_currentBranch.Code != null && e.BranchCode == _currentBranch.Code) ||
            (_currentBranch.Id != null && (Guid?)e.BranchId == _currentBranch.Id));

        modelBuilder.Entity<ElectronicInvoice>().HasQueryFilter(e =>
            _currentBranch.IsAllBranches ||
            (_currentBranch.Code != null && e.BranchCode == _currentBranch.Code) ||
            (_currentBranch.Id != null && (Guid?)e.BranchId == _currentBranch.Id));

        modelBuilder.Entity<LisPartnerConfig>().HasQueryFilter(e =>
            _currentBranch.IsAllBranches ||
            (_currentBranch.Code != null && e.BranchCode == _currentBranch.Code) ||
            (_currentBranch.Id != null && (Guid?)e.BranchId == _currentBranch.Id));

        modelBuilder.Entity<ExternalServiceMapping>().HasQueryFilter(e =>
            _currentBranch.IsAllBranches ||
            (_currentBranch.Code != null && e.BranchCode == _currentBranch.Code) ||
            (_currentBranch.Id != null && (Guid?)e.BranchId == _currentBranch.Id));

        modelBuilder.Entity<LisIntegrationOutbox>().HasQueryFilter(e =>
            _currentBranch.IsAllBranches ||
            (_currentBranch.Code != null && e.BranchCode == _currentBranch.Code) ||
            (_currentBranch.Id != null && (Guid?)e.BranchId == _currentBranch.Id));

        // -------------------------------------------------------------
        // 1. Internal Core Master Data Configurations
        // -------------------------------------------------------------
        modelBuilder.Entity<Department>(b =>
        {
            b.ToTable("AppDepartments");
            b.HasKey(x => x.Id);
            b.Property(x => x.DepartmentCode).IsRequired().HasMaxLength(20);
            b.Property(x => x.DepartmentName).IsRequired().HasMaxLength(150);
            b.Property(x => x.MinistryDepartmentCode).HasMaxLength(20);
            b.HasIndex(x => x.DepartmentCode).IsUnique();
        });

        modelBuilder.Entity<ClinicRoom>(b =>
        {
            b.ToTable("AppClinicRooms");
            b.HasKey(x => x.Id);
            b.Property(x => x.RoomCode).IsRequired().HasMaxLength(20);
            b.Property(x => x.RoomName).IsRequired().HasMaxLength(150);
            b.Property(x => x.StandardExamFee).HasPrecision(18, 2);
            b.HasIndex(x => x.RoomCode);
            b.HasIndex(x => x.BranchId);

            b.HasOne(x => x.Department)
             .WithMany(d => d.Rooms)
             .HasForeignKey(x => x.DepartmentId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Branch)
             .WithMany()
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<BranchServiceAvailability>(b =>
        {
            b.ToTable("AppBranchServiceAvailabilities");
            b.HasKey(x => x.Id);
            b.Property(x => x.CustomPrice).HasPrecision(18, 2);
            b.Property(x => x.CustomFloorPrice).HasPrecision(18, 2);
            b.HasIndex(x => new { x.BranchId, x.MedicalServiceId }).IsUnique();

            b.HasOne(x => x.Branch)
             .WithMany()
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.MedicalService)
             .WithMany()
             .HasForeignKey(x => x.MedicalServiceId)
             .OnDelete(DeleteBehavior.Cascade);
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
            b.Property(x => x.MinistryPriceCode).HasMaxLength(50);
            b.Property(x => x.InsuranceCoverageRate).HasPrecision(5, 4);
            b.Property(x => x.SurgeryType).HasMaxLength(50);
            b.HasIndex(x => x.ServiceCode);

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
            b.Property(x => x.RegistrationNumber).HasMaxLength(150);
            b.Property(x => x.NationalDrugCode).HasMaxLength(100);
            b.Property(x => x.MinistryRouteCode).HasMaxLength(20);
            b.Property(x => x.ActiveIngredientCode).HasMaxLength(50);
            b.Property(x => x.InsuranceCoverageRate).HasPrecision(5, 4);
            b.Property(x => x.TenderDecisionNumber).HasMaxLength(100);
            b.Property(x => x.Manufacturer).HasMaxLength(150);
            b.Property(x => x.CountryOfOrigin).HasMaxLength(100);
            b.HasIndex(x => x.DrugCode);

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
            b.Property(x => x.Gender).HasMaxLength(20);
            b.Property(x => x.Address).HasMaxLength(255);
            b.Property(x => x.AvatarUrl).HasMaxLength(1000);
            b.Property(x => x.NationalStaffId).HasMaxLength(50);
            b.Property(x => x.PrimarySpecialty).HasMaxLength(100);
            b.Property(x => x.SecondarySpecialties).HasMaxLength(500);
            b.Property(x => x.DigitalCertSerial).HasMaxLength(100);
            b.Property(x => x.CertificateTrainingKsk).HasMaxLength(100);
            b.Property(x => x.AssignedRoomIds).HasMaxLength(500);
            b.Property(x => x.ConsultationFee).HasPrecision(18, 2);
            b.HasIndex(x => x.StaffCode).IsUnique();
        });

        modelBuilder.Entity<MedicalSupply>(b =>
        {
            b.ToTable("AppMedicalSupplies");
            b.HasKey(x => x.Id);
            b.Property(x => x.SupplyCode).IsRequired().HasMaxLength(30);
            b.Property(x => x.SupplyName).IsRequired().HasMaxLength(255);
            b.Property(x => x.Unit).IsRequired().HasMaxLength(30);
            b.Property(x => x.Specification).HasMaxLength(255);
            b.Property(x => x.StandardPrice).HasPrecision(18, 2);
            b.Property(x => x.InsurancePrice).HasPrecision(18, 2);
            b.Property(x => x.CostPrice).HasPrecision(18, 2);
            b.Property(x => x.MinistrySupplyCode).HasMaxLength(50);
            b.Property(x => x.MinistryEquivalentName).HasMaxLength(255);
            b.Property(x => x.InsuranceCoverageRate).HasPrecision(5, 4);
            b.Property(x => x.TenderDecisionNumber).HasMaxLength(100);
            b.Property(x => x.Manufacturer).HasMaxLength(150);
            b.Property(x => x.CountryOfOrigin).HasMaxLength(100);
            b.HasIndex(x => x.SupplyCode);
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
            b.Property(x => x.BranchCode).HasMaxLength(50);
            b.Property(x => x.FacilityCode).IsRequired().HasMaxLength(20);
            b.Property(x => x.ProviderCode).HasMaxLength(50);
            b.Property(x => x.TaxCode).HasMaxLength(30);
            b.Property(x => x.BaseUrl).IsRequired().HasMaxLength(255);
            b.Property(x => x.ClientId).IsRequired().HasMaxLength(100);
            b.Property(x => x.ClientSecret).IsRequired().HasMaxLength(255);
            b.Property(x => x.SignCertSerial).HasMaxLength(100);
            b.Property(x => x.DefaultTemplateCode).HasMaxLength(30);
            b.Property(x => x.DefaultSeriesCode).HasMaxLength(30);
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
            b.HasIndex(x => x.MaICD);
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
            b.Property(x => x.Name).IsRequired().HasMaxLength(100);
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
            b.Property(x => x.Name).IsRequired().HasMaxLength(100);
            b.Property(x => x.Code).HasMaxLength(10);
        });

        modelBuilder.Entity<AppRef_YeuToNhomMau>(b =>
        {
            b.ToTable("AppRef_YeuToNhomMau");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.Name).IsRequired().HasMaxLength(100);
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
            b.Property(x => x.Name).IsRequired().HasMaxLength(255);
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
            b.Property(x => x.Name).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<AppRef_PhanLoaiSucKhoe>(b =>
        {
            b.ToTable("AppRef_PhanLoaiSucKhoe");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.Name).IsRequired().HasMaxLength(255);
        });

        modelBuilder.Entity<AppRef_TinhTrangRang>(b =>
        {
            b.ToTable("AppRef_TinhTrangRang");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.Code).HasMaxLength(10);
            b.Property(x => x.Name).IsRequired().HasMaxLength(255);
        });

        modelBuilder.Entity<AppRef_KetQuaMaTuy>(b =>
        {
            b.ToTable("AppRef_KetQuaMaTuy");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.Name).IsRequired().HasMaxLength(255);
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
            b.Property(x => x.MaThuoc).IsRequired().HasMaxLength(100);
            b.Property(x => x.TenHoatChat).IsRequired().HasMaxLength(255);
            b.Property(x => x.HamLuong).HasMaxLength(100);
            b.Property(x => x.DuongDung).HasMaxLength(100);
            b.Property(x => x.SoDangKy).HasMaxLength(255);
            b.HasIndex(x => x.MaThuoc).IsUnique();
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyBranchAuditAndSecurityInvariants();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyBranchAuditAndSecurityInvariants();
        return base.SaveChanges();
    }

    private void ApplyBranchAuditAndSecurityInvariants()
    {
        // Invariant C-0.5: PII Audit Trail Immutability
        var auditEntries = ChangeTracker.Entries<AuditLogPII>();
        foreach (var entry in auditEntries)
        {
            if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
            {
                throw new InvalidOperationException("Bảng AuditLogPII là bất biến (Immutable), nghiêm cấm sửa đổi hoặc xóa bản ghi theo TT 54/2017 và NĐ 13/2023.");
            }
        }

        // Invariant C-0.3: Auto-populate BranchId & BranchCode for new IBranchScopedEntity if empty
        var scopedEntries = ChangeTracker.Entries<IBranchScopedEntity>();
        foreach (var entry in scopedEntries)
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.BranchId == Guid.Empty && _currentBranch.Id.HasValue)
                {
                    entry.Entity.BranchId = _currentBranch.Id.Value;
                }
                if (string.IsNullOrEmpty(entry.Entity.BranchCode) && !string.IsNullOrEmpty(_currentBranch.Code) && _currentBranch.Code != "ALL")
                {
                    entry.Entity.BranchCode = _currentBranch.Code;
                }
            }
        }
    }
}

public class NullCurrentBranch : ICurrentBranch
{
    public Guid? Id => null;
    public string? Code => null;
    public bool IsAllBranches => true;
    public string? CurrentRole => null;
    public void Change(Guid? branchId, string? branchCode, bool isAllBranches = false, string? role = null) { }
}
