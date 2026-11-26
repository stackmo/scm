using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class Initial_PostgreSql_Schema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppActiveIngredients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IngredientName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    AtcCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppActiveIngredients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppAuditLogPIIs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MaskedDetails = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ClientIp = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Details = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppAuditLogPIIs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppBranches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    MinistryFacilityCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LicenseNo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DoctorInCharge = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ActiveStatus = table.Column<bool>(type: "boolean", nullable: false),
                    BankAccountDetails = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DigitalCertConfig = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppBranches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppClinicalExaminations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "text", nullable: false),
                    EncounterId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncounterCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppClinicalExaminations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppClinicRoomLoads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RoomName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    RoomType = table.Column<int>(type: "integer", nullable: false),
                    MaxCapacity = table.Column<int>(type: "integer", nullable: false),
                    CurrentQueueLength = table.Column<int>(type: "integer", nullable: false),
                    EstimatedWaitMinutes = table.Column<int>(type: "integer", nullable: false),
                    AvgConsultationMinutes = table.Column<int>(type: "integer", nullable: false),
                    FloorNumber = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentCallingTicketNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CurrentServingPatientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppClinicRoomLoads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppClinicRoomQueues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    QueueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentMaxNumber = table.Column<int>(type: "integer", nullable: false),
                    CurrentServingNumber = table.Column<int>(type: "integer", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppClinicRoomQueues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppCompanies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    TaxCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContactPerson = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ContactPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CreditLimit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppCompanies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppCompanySettlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CompanyContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    TotalContractEmployees = table.Column<int>(type: "integer", nullable: false),
                    ActualCompletedEmployees = table.Column<int>(type: "integer", nullable: false),
                    AbsentEmployees = table.Column<int>(type: "integer", nullable: false),
                    UnitPricePerEmployee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    InvoiceSerial = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    InvoiceLookupCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    InvoicedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppCompanySettlements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppDepartments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    DepartmentCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DepartmentName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    IsClinical = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    MinistryDepartmentCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LegacyId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppDepartments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppDiagnosticImagingRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EncounterId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncounterCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Modality = table.Column<int>(type: "integer", nullable: false),
                    ServiceCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    StudyInstanceUid = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    DicomWebUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Conclusion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Recommendation = table.Column<string>(type: "text", nullable: true),
                    ReportTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    RadiologistDoctorId = table.Column<Guid>(type: "uuid", nullable: true),
                    RadiologistDoctorName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    DoctorLicenseNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsSigned = table.Column<bool>(type: "boolean", nullable: false),
                    SignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppDiagnosticImagingRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppHealthcareFacilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FacilityCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    FacilityName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ProvinceCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DistrictCode = table.Column<string>(type: "text", nullable: true),
                    TechnicalLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppHealthcareFacilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppIcd10s",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IcdCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DiseaseNameVi = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DiseaseNameEn = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ChapterCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    IsChronic = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppIcd10s", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppLabResultRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EncounterId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncounterCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TubeBarcode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TestCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TestName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    NumericResult = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    StringResult = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ReferenceRange = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsAbnormal = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    M3FieldMapping = table.Column<int>(type: "integer", nullable: true),
                    InstrumentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ResultTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppLabResultRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppMedicalOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OrderCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EncounterId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncounterCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                    DoctorName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    OrderType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsInPackage = table.Column<bool>(type: "boolean", nullable: false),
                    PackageId = table.Column<Guid>(type: "uuid", nullable: true),
                    PackageName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    PaymentStatus = table.Column<int>(type: "integer", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DiagnosisIcd10 = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ClinicalNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppMedicalOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppMedicalRecordSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EncounterId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncounterCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    OverallGrade = table.Column<int>(type: "integer", nullable: false),
                    OverallGradeText = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConclusionCode = table.Column<int>(type: "integer", nullable: false),
                    ConclusionText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Recommendations = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    LeadDoctorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LeadDoctorName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    LeadDoctorLicenseNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LeadDoctorSignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LeadDoctorSignatureHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FacilityRepresentativeId = table.Column<Guid>(type: "uuid", nullable: true),
                    FacilityRepresentativeName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FacilitySignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FacilityStampSignatureHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppMedicalRecordSummaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppMedicalSupplies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplyCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SupplyName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Specification = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    StandardPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InsurancePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CostPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MinistrySupplyCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MinistryEquivalentName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    MinistryCostGroupId = table.Column<int>(type: "integer", nullable: false),
                    InsuranceCoverageRate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    IsCoveredByInsurance = table.Column<bool>(type: "boolean", nullable: false),
                    TenderDecisionNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Manufacturer = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CountryOfOrigin = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MinStockLevel = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LegacyId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppMedicalSupplies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppNationalSyncOutbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EncounterId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncounterCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ServiceCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    MaxRetries = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TransactionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NextRetryTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SyncedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppNationalSyncOutbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppPatients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    PatientCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NormalizedFullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    IdentityCardNumber = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BirthDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Gender = table.Column<int>(type: "integer", nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InsuranceCardNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppPatients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppPrescriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EncounterId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncounterCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PatientAgeMonths = table.Column<int>(type: "integer", nullable: true),
                    PatientWeightKg = table.Column<decimal>(type: "numeric", nullable: true),
                    GuardianName = table.Column<string>(type: "text", nullable: true),
                    GuardianPhone = table.Column<string>(type: "text", nullable: true),
                    GuardianCccd = table.Column<string>(type: "text", nullable: true),
                    PrescriptionTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    DoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                    DoctorName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DoctorLicenseNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DiagnosisIcd10 = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DiagnosisText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Instructions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Advices = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsSigned = table.Column<bool>(type: "boolean", nullable: false),
                    SignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SignatureHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NationalPrescriptionCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NationalQrCodeUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppPrescriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppQueueSequences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicRoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentNumber = table.Column<int>(type: "integer", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppQueueSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EncounterId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncounterCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ReceiptNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CashierId = table.Column<Guid>(type: "uuid", nullable: false),
                    CashierName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    VietQrPayload = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    VietQrTransactionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppReceipts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRef_BenhGiaDinh",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRef_BenhGiaDinh", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRef_ChiTraChiTiet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRef_ChiTraChiTiet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRef_DanhMucMeta",
                columns: table => new
                {
                    CategoryCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CategoryName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TotalRows = table.Column<int>(type: "integer", nullable: false),
                    SourceFile = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRef_DanhMucMeta", x => x.CategoryCode);
                });

            migrationBuilder.CreateTable(
                name: "AppRef_DanToc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRef_DanToc", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRef_DiaDiemKham",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRef_DiaDiemKham", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRef_DuocQuocGia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    MaThuoc = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TenHoatChat = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    HamLuong = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DuongDung = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SoDangKy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRef_DuocQuocGia", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRef_GioiTinh",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRef_GioiTinh", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRef_ICD",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    MaICD = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRef_ICD", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRef_KetLuanDeNghi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRef_KetLuanDeNghi", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRef_KetQuaMaTuy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRef_KetQuaMaTuy", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRef_M3DoiTuongKham",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRef_M3DoiTuongKham", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRef_NgheNghiep",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRef_NgheNghiep", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRef_NguonChiTra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRef_NguonChiTra", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRef_NhomMau",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRef_NhomMau", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRef_NoiCongTac",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRef_NoiCongTac", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRef_PhanLoaiSucKhoe",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRef_PhanLoaiSucKhoe", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRef_TinhThanh",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    CityCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRef_TinhThanh", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRef_TinhTrangRang",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRef_TinhTrangRang", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRef_TrangThaiXetNghiem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRef_TrangThaiXetNghiem", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRef_YesNo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Value = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRef_YesNo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRef_YeuToNhomMau",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRef_YeuToNhomMau", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppServiceGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GroupName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ServiceType = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppServiceGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppStaffs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    StaffRole = table.Column<int>(type: "integer", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    PracticeCertificateCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Specialty = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AcademicTitle = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IdentityNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DigitalSignatureBlob = table.Column<byte[]>(type: "bytea", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LegacyId = table.Column<int>(type: "integer", nullable: true),
                    Gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    AvatarUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LicenseIssueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LicenseExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NationalStaffId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PrimarySpecialty = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SecondarySpecialties = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CanFinalizeHealthCheckup = table.Column<bool>(type: "boolean", nullable: false),
                    CertificateTrainingKsk = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DigitalCertSerial = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AssignedRoomIds = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ConsultationFee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppStaffs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppWarehouses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    WarehouseCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    WarehouseName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    WarehouseType = table.Column<int>(type: "integer", nullable: false),
                    Location = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppWarehouses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImagingReportTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "text", nullable: false),
                    Modality = table.Column<int>(type: "integer", nullable: false),
                    SpecialtyCode = table.Column<string>(type: "text", nullable: false),
                    TemplateCode = table.Column<string>(type: "text", nullable: false),
                    TemplateName = table.Column<string>(type: "text", nullable: false),
                    Gender = table.Column<int>(type: "integer", nullable: false),
                    ContentTemplate = table.Column<string>(type: "text", nullable: false),
                    DefaultConclusion = table.Column<string>(type: "text", nullable: false),
                    DefaultRecommendation = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImagingReportTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NationalGatewayConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FacilityCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GatewayType = table.Column<int>(type: "integer", nullable: false),
                    BaseUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ClientId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ClientSecret = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SignCertSerial = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NationalGatewayConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "text", nullable: false),
                    TemplateCode = table.Column<string>(type: "text", nullable: false),
                    TemplateName = table.Column<string>(type: "text", nullable: false),
                    DiagnosisIcd10 = table.Column<string>(type: "text", nullable: false),
                    DiagnosisText = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    DoctorId = table.Column<Guid>(type: "uuid", nullable: true),
                    DoctorName = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppDrugs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DrugCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DrugName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ActiveIngredientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PackagingSpecification = table.Column<string>(type: "text", nullable: true),
                    UsageRoute = table.Column<int>(type: "integer", nullable: false),
                    StandardPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InsurancePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CostPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RegistrationNumber = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    NationalDrugCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MinistryRouteCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ActiveIngredientCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    InsuranceCoverageRate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    IsInsuranceCovered = table.Column<bool>(type: "boolean", nullable: false),
                    TenderDecisionNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Manufacturer = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CountryOfOrigin = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsPrescriptionRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsHighAlert = table.Column<bool>(type: "boolean", nullable: false),
                    MinStockLevel = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LegacyId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppDrugs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppDrugs_AppActiveIngredients_ActiveIngredientId",
                        column: x => x.ActiveIngredientId,
                        principalTable: "AppActiveIngredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppStaffBranchAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    RolesInBranch = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    LicenseNo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppStaffBranchAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppStaffBranchAssignments_AppBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "AppBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppVitalSigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicalExamId = table.Column<Guid>(type: "uuid", nullable: false),
                    HeightCm = table.Column<double>(type: "double precision", precision: 5, scale: 1, nullable: false),
                    WeightKg = table.Column<double>(type: "double precision", precision: 5, scale: 1, nullable: false),
                    Bmi = table.Column<double>(type: "double precision", precision: 5, scale: 2, nullable: false),
                    SystolicBp = table.Column<int>(type: "integer", nullable: false),
                    DiastolicBp = table.Column<int>(type: "integer", nullable: false),
                    Pulse = table.Column<int>(type: "integer", nullable: false),
                    Temperature = table.Column<double>(type: "double precision", precision: 4, scale: 1, nullable: true),
                    RespiratoryRate = table.Column<int>(type: "integer", nullable: true),
                    SpO2 = table.Column<double>(type: "double precision", precision: 5, scale: 2, nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RecordedByStaffId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppVitalSigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppVitalSigns_AppClinicalExaminations_ClinicalExamId",
                        column: x => x.ClinicalExamId,
                        principalTable: "AppClinicalExaminations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppCompanyContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "text", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SignDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalEstimatedEmployees = table.Column<int>(type: "integer", nullable: false),
                    TotalEstimatedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentTerms = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppCompanyContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppCompanyContracts_AppCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "AppCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppClinicRooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoomCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RoomName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomType = table.Column<int>(type: "integer", nullable: false),
                    DefaultDoctorId = table.Column<Guid>(type: "uuid", nullable: true),
                    StandardExamFee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrentCapacity = table.Column<int>(type: "integer", nullable: false),
                    MaxQueueLimit = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LegacyId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppClinicRooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppClinicRooms_AppBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "AppBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AppClinicRooms_AppDepartments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "AppDepartments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CapturedMedias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiagnosticImagingRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    MediaUrlOrBase64 = table.Column<string>(type: "text", nullable: false),
                    Caption = table.Column<string>(type: "text", nullable: true),
                    CapturedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapturedMedias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CapturedMedias_AppDiagnosticImagingRecords_DiagnosticImagin~",
                        column: x => x.DiagnosticImagingRecordId,
                        principalTable: "AppDiagnosticImagingRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppMedicalOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicalOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsInPackage = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppMedicalOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppMedicalOrderItems_AppMedicalOrders_MedicalOrderId",
                        column: x => x.MedicalOrderId,
                        principalTable: "AppMedicalOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppEncounters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EncounterCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyContractId = table.Column<Guid>(type: "uuid", nullable: true),
                    PackageId = table.Column<Guid>(type: "uuid", nullable: true),
                    EncounterDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ShiftId = table.Column<int>(type: "integer", nullable: true),
                    EncounterType = table.Column<int>(type: "integer", nullable: false),
                    WorkflowRoute = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CheckInTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InitialRoomId = table.Column<Guid>(type: "uuid", nullable: true),
                    QueueNumber = table.Column<int>(type: "integer", nullable: false),
                    DepositAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppEncounters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppEncounters_AppPatients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "AppPatients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppMedicationDispenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DispenseCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PrescriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncounterId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncounterCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PharmacistId = table.Column<Guid>(type: "uuid", nullable: false),
                    PharmacistName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DispensedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppMedicationDispenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppMedicationDispenses_AppPrescriptions_PrescriptionId",
                        column: x => x.PrescriptionId,
                        principalTable: "AppPrescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppPrescriptionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrescriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DrugId = table.Column<Guid>(type: "uuid", nullable: false),
                    DrugName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    NationalDrugCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Route = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UsageDays = table.Column<int>(type: "integer", nullable: false),
                    MorningDose = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NoonDose = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    EveningDose = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NightDose = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Instructions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppPrescriptionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppPrescriptionItems_AppPrescriptions_PrescriptionId",
                        column: x => x.PrescriptionId,
                        principalTable: "AppPrescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppRefundReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedByAdminId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedByAdminName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    RefundAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RefundedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRefundReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppRefundReceipts_AppReceipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalTable: "AppReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppRef_XaPhuong",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    WardCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CityId = table.Column<int>(type: "integer", nullable: false),
                    CityCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRef_XaPhuong", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppRef_XaPhuong_AppRef_TinhThanh_CityId",
                        column: x => x.CityId,
                        principalTable: "AppRef_TinhThanh",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppMedicalServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ServiceGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    StandardPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InsurancePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CorporatePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DoctorCommissionRate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    MinistryEquivalentCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MinistryEquivalentName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DecisionNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MinistryCostGroupId = table.Column<int>(type: "integer", nullable: true),
                    MinistryPriceCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    InsuranceCoverageRate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    IsCoveredByInsurance = table.Column<bool>(type: "boolean", nullable: false),
                    SurgeryType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ExecutionRoomId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LegacyId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppMedicalServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppMedicalServices_AppServiceGroups_ServiceGroupId",
                        column: x => x.ServiceGroupId,
                        principalTable: "AppServiceGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppSpecialtyExamDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicalExamId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecialtyType = table.Column<int>(type: "integer", nullable: false),
                    DoctorStaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsNormal = table.Column<bool>(type: "boolean", nullable: false),
                    ClinicalDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IcdCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IcdDiseaseName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    HealthClassificationRefId = table.Column<int>(type: "integer", nullable: false),
                    IsObGynRefused = table.Column<bool>(type: "boolean", nullable: false),
                    ObGynRefusalReason = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    VisualAcuityRightWithoutGlasses = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    VisualAcuityLeftWithoutGlasses = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    VisualAcuityRightWithGlasses = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    VisualAcuityLeftWithGlasses = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    HearingRightNormalVoice = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    HearingLeftNormalVoice = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    HearingRightWhisper = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    HearingLeftWhisper = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Advice = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ExaminedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSpecialtyExamDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppSpecialtyExamDetails_AppClinicalExaminations_ClinicalExa~",
                        column: x => x.ClinicalExamId,
                        principalTable: "AppClinicalExaminations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppSpecialtyExamDetails_AppRef_PhanLoaiSucKhoe_HealthClassi~",
                        column: x => x.HealthClassificationRefId,
                        principalTable: "AppRef_PhanLoaiSucKhoe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppSpecialtyExamDetails_AppStaffs_DoctorStaffId",
                        column: x => x.DoctorStaffId,
                        principalTable: "AppStaffs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PractitionerLicenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    LicenseNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScopeOfPractice = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    NationalApprovedStatus = table.Column<int>(type: "integer", nullable: false),
                    DigitalCertSerial = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CloudHsmUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PractitionerLicenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PractitionerLicenses_AppStaffs_StaffId",
                        column: x => x.StaffId,
                        principalTable: "AppStaffs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppGppRetailSales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CustomerPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PharmacistId = table.Column<Guid>(type: "uuid", nullable: false),
                    PharmacistName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SaleDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FinalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    NationalSyncCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppGppRetailSales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppGppRetailSales_AppWarehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "AppWarehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppStockTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    DrugId = table.Column<Guid>(type: "uuid", nullable: false),
                    DrugName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    StockLotExpiryId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TransactionType = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ReferenceDocNo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppStockTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppStockTransactions_AppWarehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "AppWarehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionTemplateItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrescriptionTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    DrugId = table.Column<Guid>(type: "uuid", nullable: false),
                    DrugName = table.Column<string>(type: "text", nullable: false),
                    NationalDrugCode = table.Column<string>(type: "text", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    Route = table.Column<string>(type: "text", nullable: false),
                    UsageDays = table.Column<int>(type: "integer", nullable: false),
                    MorningDose = table.Column<decimal>(type: "numeric", nullable: false),
                    NoonDose = table.Column<decimal>(type: "numeric", nullable: false),
                    EveningDose = table.Column<decimal>(type: "numeric", nullable: false),
                    NightDose = table.Column<decimal>(type: "numeric", nullable: false),
                    Instructions = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionTemplateItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrescriptionTemplateItems_PrescriptionTemplates_Prescriptio~",
                        column: x => x.PrescriptionTemplateId,
                        principalTable: "PrescriptionTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppStockLotExpiries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    DrugId = table.Column<Guid>(type: "uuid", nullable: false),
                    DrugCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DrugName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ManufactureDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    QuantityOnHand = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    QuantityReserved = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InwardUnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SellingUnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppStockLotExpiries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppStockLotExpiries_AppDrugs_DrugId",
                        column: x => x.DrugId,
                        principalTable: "AppDrugs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppStockLotExpiries_AppWarehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "AppWarehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DrugMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InternalDrugId = table.Column<Guid>(type: "uuid", nullable: false),
                    NationalDrugId = table.Column<int>(type: "integer", nullable: false),
                    MappingStatus = table.Column<int>(type: "integer", nullable: false),
                    VerifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    VerifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrugMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DrugMappings_AppDrugs_InternalDrugId",
                        column: x => x.InternalDrugId,
                        principalTable: "AppDrugs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DrugMappings_AppRef_DuocQuocGia_NationalDrugId",
                        column: x => x.NationalDrugId,
                        principalTable: "AppRef_DuocQuocGia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppPackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsNationalSyncRequired = table.Column<bool>(type: "boolean", nullable: false),
                    NationalTemplateCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppPackages_AppCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "AppCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppPackages_AppCompanyContracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "AppCompanyContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppQueueTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "text", nullable: false),
                    TicketNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StreamType = table.Column<int>(type: "integer", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Cccd = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BirthYear = table.Column<int>(type: "integer", nullable: true),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: true),
                    CheckinTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PriorityLevel = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsPregnantOrSuspected = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppQueueTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppQueueTickets_AppCompanyContracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "AppCompanyContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppMedicationDispenseItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicationDispenseId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrescriptionItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    DrugId = table.Column<Guid>(type: "uuid", nullable: false),
                    DrugName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    StockLotExpiryId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    QuantityDispensed = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppMedicationDispenseItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppMedicationDispenseItems_AppMedicationDispenses_Medicatio~",
                        column: x => x.MedicationDispenseId,
                        principalTable: "AppMedicationDispenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppBranchServiceAvailabilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicalServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    CustomPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CustomFloorPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppBranchServiceAvailabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppBranchServiceAvailabilities_AppBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "AppBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppBranchServiceAvailabilities_AppMedicalServices_MedicalSe~",
                        column: x => x.MedicalServiceId,
                        principalTable: "AppMedicalServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceM3Mappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    LabParameterCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    M3JsonKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConversionFormula = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StandardUnit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceM3Mappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceM3Mappings_AppMedicalServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "AppMedicalServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppDental32TeethDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecialtyDetailId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToothPosition = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ToothConditionRefId = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppDental32TeethDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppDental32TeethDetails_AppRef_TinhTrangRang_ToothCondition~",
                        column: x => x.ToothConditionRefId,
                        principalTable: "AppRef_TinhTrangRang",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppDental32TeethDetails_AppSpecialtyExamDetails_SpecialtyDe~",
                        column: x => x.SpecialtyDetailId,
                        principalTable: "AppSpecialtyExamDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppGppRetailSaleItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GppRetailSaleId = table.Column<Guid>(type: "uuid", nullable: false),
                    DrugId = table.Column<Guid>(type: "uuid", nullable: false),
                    DrugName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    StockLotExpiryId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppGppRetailSaleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppGppRetailSaleItems_AppGppRetailSales_GppRetailSaleId",
                        column: x => x.GppRetailSaleId,
                        principalTable: "AppGppRetailSales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppPackageTargetGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TargetGender = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppPackageTargetGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppPackageTargetGroups_AppPackages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "AppPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppPhlebotomyBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "text", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BatchName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    SamplingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SamplingLocation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PlannedCount = table.Column<int>(type: "integer", nullable: false),
                    ActualCollectedCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppPhlebotomyBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppPhlebotomyBatches_AppCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "AppCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppPhlebotomyBatches_AppCompanyContracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "AppCompanyContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppPhlebotomyBatches_AppPackages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "AppPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppQueueAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RoomName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    RoomType = table.Column<int>(type: "integer", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CalledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DoctorStaffId = table.Column<Guid>(type: "uuid", nullable: true),
                    DoctorStaffName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppQueueAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppQueueAssignments_AppQueueTickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "AppQueueTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppCompanyEmployees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    IdentificationNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Gender = table.Column<int>(type: "integer", nullable: false),
                    Department = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    EmployeeCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    HealthInsuranceCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsSampleCollected = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppCompanyEmployees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppCompanyEmployees_AppCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "AppCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppCompanyEmployees_AppCompanyContracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "AppCompanyContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppCompanyEmployees_AppPackageTargetGroups_TargetGroupId",
                        column: x => x.TargetGroupId,
                        principalTable: "AppPackageTargetGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppCompanyEmployees_AppPackages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "AppPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppPackageServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    OriginalPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DealPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FloorPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsSpecialApproved = table.Column<bool>(type: "boolean", nullable: false),
                    RegisterType = table.Column<int>(type: "integer", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    GroupChoiceCount = table.Column<int>(type: "integer", nullable: false),
                    IsSpecimenOutsource = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppPackageServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppPackageServices_AppMedicalServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "AppMedicalServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppPackageServices_AppPackageTargetGroups_TargetGroupId",
                        column: x => x.TargetGroupId,
                        principalTable: "AppPackageTargetGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppPackageServices_AppPackages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "AppPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppColdChainManifests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ManifestCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    HandoverStaff = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CourierStaff = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    LabReceiverStaff = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DepartureTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ArrivalTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DepartureTemperature = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ArrivalTemperature = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    TotalTubesHandedOver = table.Column<int>(type: "integer", nullable: false),
                    TotalTubesAccepted = table.Column<int>(type: "integer", nullable: false),
                    TotalTubesRejected = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsDegraded = table.Column<bool>(type: "boolean", nullable: false),
                    HasDiscrepancy = table.Column<bool>(type: "boolean", nullable: false),
                    DiscrepancyCount = table.Column<int>(type: "integer", nullable: false),
                    DiscrepancyNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppColdChainManifests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppColdChainManifests_AppPhlebotomyBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "AppPhlebotomyBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppBatchEmployeeMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShiftOrder = table.Column<int>(type: "integer", nullable: false),
                    CheckinTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsCollected = table.Column<bool>(type: "boolean", nullable: false),
                    CollectorStaff = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SamplingNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppBatchEmployeeMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppBatchEmployeeMappings_AppCompanyEmployees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "AppCompanyEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppBatchEmployeeMappings_AppPhlebotomyBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "AppPhlebotomyBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppSpecimenTubes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchEmployeeMappingId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TubeBarcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TubeType = table.Column<int>(type: "integer", nullable: false),
                    CapColor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetTests = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PrintedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CollectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CollectorId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ManifestId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResultedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSpecimenTubes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppSpecimenTubes_AppBatchEmployeeMappings_BatchEmployeeMapp~",
                        column: x => x.BatchEmployeeMappingId,
                        principalTable: "AppBatchEmployeeMappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppSpecimenTubes_AppColdChainManifests_ManifestId",
                        column: x => x.ManifestId,
                        principalTable: "AppColdChainManifests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppSpecimenTubes_AppCompanyEmployees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "AppCompanyEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppActiveIngredients_IngredientCode",
                table: "AppActiveIngredients",
                column: "IngredientCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppAuditLogPIIs_Action",
                table: "AppAuditLogPIIs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AppAuditLogPIIs_BranchId",
                table: "AppAuditLogPIIs",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppAuditLogPIIs_TimestampUtc",
                table: "AppAuditLogPIIs",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AppBatchEmployeeMappings_BatchId_EmployeeId",
                table: "AppBatchEmployeeMappings",
                columns: new[] { "BatchId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppBatchEmployeeMappings_EmployeeId",
                table: "AppBatchEmployeeMappings",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AppBatchEmployeeMappings_IsCollected",
                table: "AppBatchEmployeeMappings",
                column: "IsCollected");

            migrationBuilder.CreateIndex(
                name: "IX_AppBranches_Code",
                table: "AppBranches",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppBranches_LicenseNo",
                table: "AppBranches",
                column: "LicenseNo");

            migrationBuilder.CreateIndex(
                name: "IX_AppBranches_MinistryFacilityCode",
                table: "AppBranches",
                column: "MinistryFacilityCode");

            migrationBuilder.CreateIndex(
                name: "IX_AppBranchServiceAvailabilities_BranchId_MedicalServiceId",
                table: "AppBranchServiceAvailabilities",
                columns: new[] { "BranchId", "MedicalServiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppBranchServiceAvailabilities_MedicalServiceId",
                table: "AppBranchServiceAvailabilities",
                column: "MedicalServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_AppClinicalExaminations_EncounterCode",
                table: "AppClinicalExaminations",
                column: "EncounterCode");

            migrationBuilder.CreateIndex(
                name: "IX_AppClinicalExaminations_EncounterId",
                table: "AppClinicalExaminations",
                column: "EncounterId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppClinicalExaminations_IsCompleted",
                table: "AppClinicalExaminations",
                column: "IsCompleted");

            migrationBuilder.CreateIndex(
                name: "IX_AppClinicalExaminations_PatientId",
                table: "AppClinicalExaminations",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_AppClinicRoomLoads_IsActive",
                table: "AppClinicRoomLoads",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AppClinicRoomLoads_RoomCode",
                table: "AppClinicRoomLoads",
                column: "RoomCode");

            migrationBuilder.CreateIndex(
                name: "IX_AppClinicRoomLoads_RoomId",
                table: "AppClinicRoomLoads",
                column: "RoomId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppClinicRoomLoads_RoomType",
                table: "AppClinicRoomLoads",
                column: "RoomType");

            migrationBuilder.CreateIndex(
                name: "IX_AppClinicRoomQueues_TenantId_RoomId_QueueDate",
                table: "AppClinicRoomQueues",
                columns: new[] { "TenantId", "RoomId", "QueueDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppClinicRooms_BranchId",
                table: "AppClinicRooms",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppClinicRooms_DepartmentId",
                table: "AppClinicRooms",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppClinicRooms_RoomCode",
                table: "AppClinicRooms",
                column: "RoomCode");

            migrationBuilder.CreateIndex(
                name: "IX_AppColdChainManifests_BatchId",
                table: "AppColdChainManifests",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppColdChainManifests_BranchId",
                table: "AppColdChainManifests",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppColdChainManifests_IsDegraded",
                table: "AppColdChainManifests",
                column: "IsDegraded");

            migrationBuilder.CreateIndex(
                name: "IX_AppColdChainManifests_ManifestCode",
                table: "AppColdChainManifests",
                column: "ManifestCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppColdChainManifests_Status",
                table: "AppColdChainManifests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppCompanies_Code",
                table: "AppCompanies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppCompanies_TaxCode",
                table: "AppCompanies",
                column: "TaxCode");

            migrationBuilder.CreateIndex(
                name: "IX_AppCompanyContracts_CompanyId",
                table: "AppCompanyContracts",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCompanyContracts_ContractNumber",
                table: "AppCompanyContracts",
                column: "ContractNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppCompanyEmployees_CompanyId",
                table: "AppCompanyEmployees",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCompanyEmployees_CompanyId_FullName",
                table: "AppCompanyEmployees",
                columns: new[] { "CompanyId", "FullName" });

            migrationBuilder.CreateIndex(
                name: "IX_AppCompanyEmployees_ContractId",
                table: "AppCompanyEmployees",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCompanyEmployees_IdentificationNumber",
                table: "AppCompanyEmployees",
                column: "IdentificationNumber");

            migrationBuilder.CreateIndex(
                name: "IX_AppCompanyEmployees_IsSampleCollected",
                table: "AppCompanyEmployees",
                column: "IsSampleCollected");

            migrationBuilder.CreateIndex(
                name: "IX_AppCompanyEmployees_PackageId_Department",
                table: "AppCompanyEmployees",
                columns: new[] { "PackageId", "Department" });

            migrationBuilder.CreateIndex(
                name: "IX_AppCompanyEmployees_PackageId_IdentificationNumber",
                table: "AppCompanyEmployees",
                columns: new[] { "PackageId", "IdentificationNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppCompanyEmployees_TargetGroupId",
                table: "AppCompanyEmployees",
                column: "TargetGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCompanySettlements_BranchId",
                table: "AppCompanySettlements",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCompanySettlements_CompanyContractId",
                table: "AppCompanySettlements",
                column: "CompanyContractId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCompanySettlements_Status",
                table: "AppCompanySettlements",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppDental32TeethDetails_SpecialtyDetailId_ToothPosition",
                table: "AppDental32TeethDetails",
                columns: new[] { "SpecialtyDetailId", "ToothPosition" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppDental32TeethDetails_ToothConditionRefId",
                table: "AppDental32TeethDetails",
                column: "ToothConditionRefId");

            migrationBuilder.CreateIndex(
                name: "IX_AppDepartments_DepartmentCode",
                table: "AppDepartments",
                column: "DepartmentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppDiagnosticImagingRecords_BranchId",
                table: "AppDiagnosticImagingRecords",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppDiagnosticImagingRecords_EncounterId",
                table: "AppDiagnosticImagingRecords",
                column: "EncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_AppDiagnosticImagingRecords_ServiceCode",
                table: "AppDiagnosticImagingRecords",
                column: "ServiceCode");

            migrationBuilder.CreateIndex(
                name: "IX_AppDiagnosticImagingRecords_StudyInstanceUid",
                table: "AppDiagnosticImagingRecords",
                column: "StudyInstanceUid");

            migrationBuilder.CreateIndex(
                name: "IX_AppDrugs_ActiveIngredientId",
                table: "AppDrugs",
                column: "ActiveIngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_AppDrugs_DrugCode",
                table: "AppDrugs",
                column: "DrugCode");

            migrationBuilder.CreateIndex(
                name: "IX_AppEncounters_BranchCode_EncounterDate",
                table: "AppEncounters",
                columns: new[] { "BranchCode", "EncounterDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AppEncounters_BranchId_EncounterDate",
                table: "AppEncounters",
                columns: new[] { "BranchId", "EncounterDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AppEncounters_BranchId_Status_EncounterDate",
                table: "AppEncounters",
                columns: new[] { "BranchId", "Status", "EncounterDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AppEncounters_EncounterCode",
                table: "AppEncounters",
                column: "EncounterCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppEncounters_PatientId",
                table: "AppEncounters",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_AppEncounters_PatientId_EncounterDate",
                table: "AppEncounters",
                columns: new[] { "PatientId", "EncounterDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AppEncounters_Status",
                table: "AppEncounters",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppGppRetailSaleItems_DrugId",
                table: "AppGppRetailSaleItems",
                column: "DrugId");

            migrationBuilder.CreateIndex(
                name: "IX_AppGppRetailSaleItems_GppRetailSaleId",
                table: "AppGppRetailSaleItems",
                column: "GppRetailSaleId");

            migrationBuilder.CreateIndex(
                name: "IX_AppGppRetailSaleItems_StockLotExpiryId",
                table: "AppGppRetailSaleItems",
                column: "StockLotExpiryId");

            migrationBuilder.CreateIndex(
                name: "IX_AppGppRetailSales_BranchId",
                table: "AppGppRetailSales",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppGppRetailSales_PharmacistId",
                table: "AppGppRetailSales",
                column: "PharmacistId");

            migrationBuilder.CreateIndex(
                name: "IX_AppGppRetailSales_SaleDate",
                table: "AppGppRetailSales",
                column: "SaleDate");

            migrationBuilder.CreateIndex(
                name: "IX_AppGppRetailSales_WarehouseId",
                table: "AppGppRetailSales",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_AppHealthcareFacilities_FacilityCode",
                table: "AppHealthcareFacilities",
                column: "FacilityCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppIcd10s_IcdCode",
                table: "AppIcd10s",
                column: "IcdCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppLabResultRecords_BranchId",
                table: "AppLabResultRecords",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLabResultRecords_EncounterId",
                table: "AppLabResultRecords",
                column: "EncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLabResultRecords_M3FieldMapping",
                table: "AppLabResultRecords",
                column: "M3FieldMapping");

            migrationBuilder.CreateIndex(
                name: "IX_AppLabResultRecords_TestCode",
                table: "AppLabResultRecords",
                column: "TestCode");

            migrationBuilder.CreateIndex(
                name: "IX_AppLabResultRecords_TubeBarcode",
                table: "AppLabResultRecords",
                column: "TubeBarcode");

            migrationBuilder.CreateIndex(
                name: "IX_AppMedicalOrderItems_MedicalOrderId",
                table: "AppMedicalOrderItems",
                column: "MedicalOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_AppMedicalOrderItems_ServiceId",
                table: "AppMedicalOrderItems",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_AppMedicalOrders_BranchId_CreatedAt",
                table: "AppMedicalOrders",
                columns: new[] { "BranchId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AppMedicalOrders_DoctorId",
                table: "AppMedicalOrders",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppMedicalOrders_EncounterId",
                table: "AppMedicalOrders",
                column: "EncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_AppMedicalOrders_EncounterId_Status",
                table: "AppMedicalOrders",
                columns: new[] { "EncounterId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AppMedicalOrders_OrderCode",
                table: "AppMedicalOrders",
                column: "OrderCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppMedicalOrders_OrderType",
                table: "AppMedicalOrders",
                column: "OrderType");

            migrationBuilder.CreateIndex(
                name: "IX_AppMedicalOrders_PatientId",
                table: "AppMedicalOrders",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_AppMedicalOrders_Status",
                table: "AppMedicalOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppMedicalRecordSummaries_BranchId",
                table: "AppMedicalRecordSummaries",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppMedicalRecordSummaries_EncounterId",
                table: "AppMedicalRecordSummaries",
                column: "EncounterId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppMedicalRecordSummaries_IsCompleted",
                table: "AppMedicalRecordSummaries",
                column: "IsCompleted");

            migrationBuilder.CreateIndex(
                name: "IX_AppMedicalRecordSummaries_OverallGrade",
                table: "AppMedicalRecordSummaries",
                column: "OverallGrade");

            migrationBuilder.CreateIndex(
                name: "IX_AppMedicalRecordSummaries_PatientId",
                table: "AppMedicalRecordSummaries",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_AppMedicalServices_ServiceCode",
                table: "AppMedicalServices",
                column: "ServiceCode");

            migrationBuilder.CreateIndex(
                name: "IX_AppMedicalServices_ServiceGroupId",
                table: "AppMedicalServices",
                column: "ServiceGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AppMedicalSupplies_SupplyCode",
                table: "AppMedicalSupplies",
                column: "SupplyCode");

            migrationBuilder.CreateIndex(
                name: "IX_AppMedicationDispenseItems_DrugId",
                table: "AppMedicationDispenseItems",
                column: "DrugId");

            migrationBuilder.CreateIndex(
                name: "IX_AppMedicationDispenseItems_MedicationDispenseId",
                table: "AppMedicationDispenseItems",
                column: "MedicationDispenseId");

            migrationBuilder.CreateIndex(
                name: "IX_AppMedicationDispenseItems_PrescriptionItemId",
                table: "AppMedicationDispenseItems",
                column: "PrescriptionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AppMedicationDispenseItems_StockLotExpiryId",
                table: "AppMedicationDispenseItems",
                column: "StockLotExpiryId");

            migrationBuilder.CreateIndex(
                name: "IX_AppMedicationDispenses_BranchId",
                table: "AppMedicationDispenses",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppMedicationDispenses_EncounterId",
                table: "AppMedicationDispenses",
                column: "EncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_AppMedicationDispenses_PatientId",
                table: "AppMedicationDispenses",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_AppMedicationDispenses_PharmacistId",
                table: "AppMedicationDispenses",
                column: "PharmacistId");

            migrationBuilder.CreateIndex(
                name: "IX_AppMedicationDispenses_PrescriptionId",
                table: "AppMedicationDispenses",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_AppNationalSyncOutbox_BranchId",
                table: "AppNationalSyncOutbox",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppNationalSyncOutbox_EncounterId",
                table: "AppNationalSyncOutbox",
                column: "EncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_AppNationalSyncOutbox_NextRetryTime",
                table: "AppNationalSyncOutbox",
                column: "NextRetryTime");

            migrationBuilder.CreateIndex(
                name: "IX_AppNationalSyncOutbox_Status",
                table: "AppNationalSyncOutbox",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppPackages_Code",
                table: "AppPackages",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppPackages_CompanyId",
                table: "AppPackages",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPackages_ContractId",
                table: "AppPackages",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPackageServices_PackageId",
                table: "AppPackageServices",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPackageServices_ServiceId",
                table: "AppPackageServices",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPackageServices_TargetGroupId",
                table: "AppPackageServices",
                column: "TargetGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPackageTargetGroups_PackageId",
                table: "AppPackageTargetGroups",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPatients_IdentityCardNumber",
                table: "AppPatients",
                column: "IdentityCardNumber");

            migrationBuilder.CreateIndex(
                name: "IX_AppPatients_NormalizedFullName",
                table: "AppPatients",
                column: "NormalizedFullName");

            migrationBuilder.CreateIndex(
                name: "IX_AppPatients_PatientCode",
                table: "AppPatients",
                column: "PatientCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppPhlebotomyBatches_BatchCode",
                table: "AppPhlebotomyBatches",
                column: "BatchCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppPhlebotomyBatches_CompanyId",
                table: "AppPhlebotomyBatches",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPhlebotomyBatches_ContractId",
                table: "AppPhlebotomyBatches",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPhlebotomyBatches_PackageId",
                table: "AppPhlebotomyBatches",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPhlebotomyBatches_SamplingDate",
                table: "AppPhlebotomyBatches",
                column: "SamplingDate");

            migrationBuilder.CreateIndex(
                name: "IX_AppPhlebotomyBatches_Status",
                table: "AppPhlebotomyBatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppPrescriptionItems_DrugId",
                table: "AppPrescriptionItems",
                column: "DrugId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPrescriptionItems_NationalDrugCode",
                table: "AppPrescriptionItems",
                column: "NationalDrugCode");

            migrationBuilder.CreateIndex(
                name: "IX_AppPrescriptionItems_PrescriptionId",
                table: "AppPrescriptionItems",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPrescriptions_BranchId",
                table: "AppPrescriptions",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPrescriptions_DoctorId",
                table: "AppPrescriptions",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPrescriptions_EncounterId",
                table: "AppPrescriptions",
                column: "EncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPrescriptions_NationalPrescriptionCode",
                table: "AppPrescriptions",
                column: "NationalPrescriptionCode");

            migrationBuilder.CreateIndex(
                name: "IX_AppPrescriptions_PatientId",
                table: "AppPrescriptions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_AppQueueAssignments_RoomCode_Status",
                table: "AppQueueAssignments",
                columns: new[] { "RoomCode", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AppQueueAssignments_RoomId_Status",
                table: "AppQueueAssignments",
                columns: new[] { "RoomId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AppQueueAssignments_TicketId_OrderIndex",
                table: "AppQueueAssignments",
                columns: new[] { "TicketId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_AppQueueSequences_BranchId_ClinicRoomId_Date",
                table: "AppQueueSequences",
                columns: new[] { "BranchId", "ClinicRoomId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppQueueTickets_CheckinTime",
                table: "AppQueueTickets",
                column: "CheckinTime");

            migrationBuilder.CreateIndex(
                name: "IX_AppQueueTickets_ContractId",
                table: "AppQueueTickets",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_AppQueueTickets_PatientId",
                table: "AppQueueTickets",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_AppQueueTickets_Status",
                table: "AppQueueTickets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppQueueTickets_StreamType",
                table: "AppQueueTickets",
                column: "StreamType");

            migrationBuilder.CreateIndex(
                name: "IX_AppQueueTickets_TicketNumber",
                table: "AppQueueTickets",
                column: "TicketNumber");

            migrationBuilder.CreateIndex(
                name: "IX_AppReceipts_BranchId",
                table: "AppReceipts",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppReceipts_EncounterId",
                table: "AppReceipts",
                column: "EncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_AppReceipts_PatientId",
                table: "AppReceipts",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_AppReceipts_ReceiptNumber",
                table: "AppReceipts",
                column: "ReceiptNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppReceipts_Status",
                table: "AppReceipts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppRef_DuocQuocGia_MaThuoc",
                table: "AppRef_DuocQuocGia",
                column: "MaThuoc",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppRef_ICD_MaICD",
                table: "AppRef_ICD",
                column: "MaICD",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppRef_NgheNghiep_Name",
                table: "AppRef_NgheNghiep",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_AppRef_TinhThanh_CityCode",
                table: "AppRef_TinhThanh",
                column: "CityCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppRef_XaPhuong_CityId",
                table: "AppRef_XaPhuong",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_AppRef_XaPhuong_WardCode",
                table: "AppRef_XaPhuong",
                column: "WardCode");

            migrationBuilder.CreateIndex(
                name: "IX_AppRefundReceipts_ApprovedByAdminId",
                table: "AppRefundReceipts",
                column: "ApprovedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_AppRefundReceipts_ReceiptId",
                table: "AppRefundReceipts",
                column: "ReceiptId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppServiceGroups_GroupCode",
                table: "AppServiceGroups",
                column: "GroupCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppSpecialtyExamDetails_ClinicalExamId_SpecialtyType",
                table: "AppSpecialtyExamDetails",
                columns: new[] { "ClinicalExamId", "SpecialtyType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppSpecialtyExamDetails_DoctorStaffId",
                table: "AppSpecialtyExamDetails",
                column: "DoctorStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSpecialtyExamDetails_HealthClassificationRefId",
                table: "AppSpecialtyExamDetails",
                column: "HealthClassificationRefId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSpecimenTubes_BatchEmployeeMappingId",
                table: "AppSpecimenTubes",
                column: "BatchEmployeeMappingId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSpecimenTubes_EmployeeId",
                table: "AppSpecimenTubes",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSpecimenTubes_ManifestId",
                table: "AppSpecimenTubes",
                column: "ManifestId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSpecimenTubes_Status",
                table: "AppSpecimenTubes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppSpecimenTubes_TubeBarcode",
                table: "AppSpecimenTubes",
                column: "TubeBarcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppSpecimenTubes_TubeType",
                table: "AppSpecimenTubes",
                column: "TubeType");

            migrationBuilder.CreateIndex(
                name: "IX_AppStaffBranchAssignments_BranchId",
                table: "AppStaffBranchAssignments",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppStaffBranchAssignments_StaffId_BranchId",
                table: "AppStaffBranchAssignments",
                columns: new[] { "StaffId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppStaffs_StaffCode",
                table: "AppStaffs",
                column: "StaffCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppStockLotExpiries_BranchId",
                table: "AppStockLotExpiries",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppStockLotExpiries_DrugId",
                table: "AppStockLotExpiries",
                column: "DrugId");

            migrationBuilder.CreateIndex(
                name: "IX_AppStockLotExpiries_ExpiryDate",
                table: "AppStockLotExpiries",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_AppStockLotExpiries_LotNumber",
                table: "AppStockLotExpiries",
                column: "LotNumber");

            migrationBuilder.CreateIndex(
                name: "IX_AppStockLotExpiries_WarehouseId",
                table: "AppStockLotExpiries",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_AppStockTransactions_BranchId",
                table: "AppStockTransactions",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppStockTransactions_CreatedAt",
                table: "AppStockTransactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AppStockTransactions_DrugId",
                table: "AppStockTransactions",
                column: "DrugId");

            migrationBuilder.CreateIndex(
                name: "IX_AppStockTransactions_StockLotExpiryId",
                table: "AppStockTransactions",
                column: "StockLotExpiryId");

            migrationBuilder.CreateIndex(
                name: "IX_AppStockTransactions_TransactionType",
                table: "AppStockTransactions",
                column: "TransactionType");

            migrationBuilder.CreateIndex(
                name: "IX_AppStockTransactions_WarehouseId",
                table: "AppStockTransactions",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_AppVitalSigns_ClinicalExamId",
                table: "AppVitalSigns",
                column: "ClinicalExamId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppWarehouses_BranchId",
                table: "AppWarehouses",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppWarehouses_WarehouseCode",
                table: "AppWarehouses",
                column: "WarehouseCode");

            migrationBuilder.CreateIndex(
                name: "IX_CapturedMedias_DiagnosticImagingRecordId",
                table: "CapturedMedias",
                column: "DiagnosticImagingRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugMappings_InternalDrugId",
                table: "DrugMappings",
                column: "InternalDrugId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrugMappings_NationalDrugId",
                table: "DrugMappings",
                column: "NationalDrugId");

            migrationBuilder.CreateIndex(
                name: "IX_NationalGatewayConfigs_FacilityCode_GatewayType",
                table: "NationalGatewayConfigs",
                columns: new[] { "FacilityCode", "GatewayType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PractitionerLicenses_LicenseNumber",
                table: "PractitionerLicenses",
                column: "LicenseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PractitionerLicenses_StaffId",
                table: "PractitionerLicenses",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionTemplateItems_PrescriptionTemplateId",
                table: "PrescriptionTemplateItems",
                column: "PrescriptionTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceM3Mappings_M3JsonKey",
                table: "ServiceM3Mappings",
                column: "M3JsonKey");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceM3Mappings_ServiceId_LabParameterCode",
                table: "ServiceM3Mappings",
                columns: new[] { "ServiceId", "LabParameterCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppAuditLogPIIs");

            migrationBuilder.DropTable(
                name: "AppBranchServiceAvailabilities");

            migrationBuilder.DropTable(
                name: "AppClinicRoomLoads");

            migrationBuilder.DropTable(
                name: "AppClinicRoomQueues");

            migrationBuilder.DropTable(
                name: "AppClinicRooms");

            migrationBuilder.DropTable(
                name: "AppCompanySettlements");

            migrationBuilder.DropTable(
                name: "AppDental32TeethDetails");

            migrationBuilder.DropTable(
                name: "AppEncounters");

            migrationBuilder.DropTable(
                name: "AppGppRetailSaleItems");

            migrationBuilder.DropTable(
                name: "AppHealthcareFacilities");

            migrationBuilder.DropTable(
                name: "AppIcd10s");

            migrationBuilder.DropTable(
                name: "AppLabResultRecords");

            migrationBuilder.DropTable(
                name: "AppMedicalOrderItems");

            migrationBuilder.DropTable(
                name: "AppMedicalRecordSummaries");

            migrationBuilder.DropTable(
                name: "AppMedicalSupplies");

            migrationBuilder.DropTable(
                name: "AppMedicationDispenseItems");

            migrationBuilder.DropTable(
                name: "AppNationalSyncOutbox");

            migrationBuilder.DropTable(
                name: "AppPackageServices");

            migrationBuilder.DropTable(
                name: "AppPrescriptionItems");

            migrationBuilder.DropTable(
                name: "AppQueueAssignments");

            migrationBuilder.DropTable(
                name: "AppQueueSequences");

            migrationBuilder.DropTable(
                name: "AppRef_BenhGiaDinh");

            migrationBuilder.DropTable(
                name: "AppRef_ChiTraChiTiet");

            migrationBuilder.DropTable(
                name: "AppRef_DanhMucMeta");

            migrationBuilder.DropTable(
                name: "AppRef_DanToc");

            migrationBuilder.DropTable(
                name: "AppRef_DiaDiemKham");

            migrationBuilder.DropTable(
                name: "AppRef_GioiTinh");

            migrationBuilder.DropTable(
                name: "AppRef_ICD");

            migrationBuilder.DropTable(
                name: "AppRef_KetLuanDeNghi");

            migrationBuilder.DropTable(
                name: "AppRef_KetQuaMaTuy");

            migrationBuilder.DropTable(
                name: "AppRef_M3DoiTuongKham");

            migrationBuilder.DropTable(
                name: "AppRef_NgheNghiep");

            migrationBuilder.DropTable(
                name: "AppRef_NguonChiTra");

            migrationBuilder.DropTable(
                name: "AppRef_NhomMau");

            migrationBuilder.DropTable(
                name: "AppRef_NoiCongTac");

            migrationBuilder.DropTable(
                name: "AppRef_TrangThaiXetNghiem");

            migrationBuilder.DropTable(
                name: "AppRef_XaPhuong");

            migrationBuilder.DropTable(
                name: "AppRef_YesNo");

            migrationBuilder.DropTable(
                name: "AppRef_YeuToNhomMau");

            migrationBuilder.DropTable(
                name: "AppRefundReceipts");

            migrationBuilder.DropTable(
                name: "AppSpecimenTubes");

            migrationBuilder.DropTable(
                name: "AppStaffBranchAssignments");

            migrationBuilder.DropTable(
                name: "AppStockLotExpiries");

            migrationBuilder.DropTable(
                name: "AppStockTransactions");

            migrationBuilder.DropTable(
                name: "AppVitalSigns");

            migrationBuilder.DropTable(
                name: "CapturedMedias");

            migrationBuilder.DropTable(
                name: "DrugMappings");

            migrationBuilder.DropTable(
                name: "ImagingReportTemplates");

            migrationBuilder.DropTable(
                name: "NationalGatewayConfigs");

            migrationBuilder.DropTable(
                name: "PractitionerLicenses");

            migrationBuilder.DropTable(
                name: "PrescriptionTemplateItems");

            migrationBuilder.DropTable(
                name: "ServiceM3Mappings");

            migrationBuilder.DropTable(
                name: "AppDepartments");

            migrationBuilder.DropTable(
                name: "AppRef_TinhTrangRang");

            migrationBuilder.DropTable(
                name: "AppSpecialtyExamDetails");

            migrationBuilder.DropTable(
                name: "AppPatients");

            migrationBuilder.DropTable(
                name: "AppGppRetailSales");

            migrationBuilder.DropTable(
                name: "AppMedicalOrders");

            migrationBuilder.DropTable(
                name: "AppMedicationDispenses");

            migrationBuilder.DropTable(
                name: "AppQueueTickets");

            migrationBuilder.DropTable(
                name: "AppRef_TinhThanh");

            migrationBuilder.DropTable(
                name: "AppReceipts");

            migrationBuilder.DropTable(
                name: "AppBatchEmployeeMappings");

            migrationBuilder.DropTable(
                name: "AppColdChainManifests");

            migrationBuilder.DropTable(
                name: "AppBranches");

            migrationBuilder.DropTable(
                name: "AppDiagnosticImagingRecords");

            migrationBuilder.DropTable(
                name: "AppDrugs");

            migrationBuilder.DropTable(
                name: "AppRef_DuocQuocGia");

            migrationBuilder.DropTable(
                name: "PrescriptionTemplates");

            migrationBuilder.DropTable(
                name: "AppMedicalServices");

            migrationBuilder.DropTable(
                name: "AppClinicalExaminations");

            migrationBuilder.DropTable(
                name: "AppRef_PhanLoaiSucKhoe");

            migrationBuilder.DropTable(
                name: "AppStaffs");

            migrationBuilder.DropTable(
                name: "AppWarehouses");

            migrationBuilder.DropTable(
                name: "AppPrescriptions");

            migrationBuilder.DropTable(
                name: "AppCompanyEmployees");

            migrationBuilder.DropTable(
                name: "AppPhlebotomyBatches");

            migrationBuilder.DropTable(
                name: "AppActiveIngredients");

            migrationBuilder.DropTable(
                name: "AppServiceGroups");

            migrationBuilder.DropTable(
                name: "AppPackageTargetGroups");

            migrationBuilder.DropTable(
                name: "AppPackages");

            migrationBuilder.DropTable(
                name: "AppCompanyContracts");

            migrationBuilder.DropTable(
                name: "AppCompanies");
        }
    }
}
