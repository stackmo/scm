using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class Add_Doctor_Rbac_Queue_Locking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_AppRef_ICD_MaICD\";");

            migrationBuilder.AddColumn<bool>(
                name: "CanOrderCls",
                table: "AppStaffs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanPrescribe",
                table: "AppStaffs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanViewFullClinicalHistory",
                table: "AppStaffs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ClinicalActorGroup",
                table: "AppStaffs",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AppRef_YeuToNhomMau",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AppRef_YesNo",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AppRef_TrangThaiXetNghiem",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AppRef_TinhTrangRang",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AppRef_PhanLoaiSucKhoe",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AppRef_NhomMau",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AppRef_KetQuaMaTuy",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AppRef_GioiTinh",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "SoDangKy",
                table: "AppRef_DuocQuocGia",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MaThuoc",
                table: "AppRef_DuocQuocGia",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "AppReceipts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CallAttemptCount",
                table: "AppQueueAssignments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "PriorityRecall",
                table: "AppQueueAssignments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReturnedFromClsAt",
                table: "AppQueueAssignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SentToClsAt",
                table: "AppQueueAssignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsExternalPurchase",
                table: "AppPrescriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "AppPrescriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "AppPrescriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReceiptId",
                table: "AppPrescriptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SpecialtyType",
                table: "AppPrescriptions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "AppPatients",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BhytAddress",
                table: "AppPatients",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BhytAreaCode",
                table: "AppPatients",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BhytBenefitRate",
                table: "AppPatients",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BhytInitialFacilityCode",
                table: "AppPatients",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BhytValidFrom",
                table: "AppPatients",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BhytValidTo",
                table: "AppPatients",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BloodTypeId",
                table: "AppPatients",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CityCode",
                table: "AppPatients",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "AppPatients",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DanTocId",
                table: "AppPatients",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FamilyHistoryDiseaseIds",
                table: "AppPatients",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FamilyHistoryIcdCodes",
                table: "AppPatients",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NationalityCode",
                table: "AppPatients",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NgheNghiepCode",
                table: "AppPatients",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NgheNghiepId",
                table: "AppPatients",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonalHistoryJson",
                table: "AppPatients",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RhFactorId",
                table: "AppPatients",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WardCode",
                table: "AppPatients",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WardId",
                table: "AppPatients",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkPlaceName",
                table: "AppPatients",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkPlaceWardCode",
                table: "AppPatients",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ContractId",
                table: "AppPackages",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "AppPackages",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "ApplicableGender",
                table: "AppPackages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AppPackages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxAge",
                table: "AppPackages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinAge",
                table: "AppPackages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "AppPackages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TargetPrice",
                table: "AppPackages",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidFrom",
                table: "AppPackages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidTo",
                table: "AppPackages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdditionalSummaryNotes",
                table: "AppMedicalRecordSummaries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyStamp",
                table: "AppMedicalRecordSummaries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsAdditionalOutOfPackageTab",
                table: "AppMedicalRecordSummaries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsHealthQualified",
                table: "AppMedicalRecordSummaries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SummaryStatus",
                table: "AppMedicalRecordSummaries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SpecialtyType",
                table: "AppMedicalOrders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSelectedForPayment",
                table: "AppMedicalOrderItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "AppMedicalOrderItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentStatus",
                table: "AppMedicalOrderItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ReceiptId",
                table: "AppMedicalOrderItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BhytReasonCode",
                table: "AppEncounters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BhytReferralCode",
                table: "AppEncounters",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "AppEncounters",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "M3LocationId",
                table: "AppEncounters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "M3PaymentDetailId",
                table: "AppEncounters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "M3PaymentSourceId",
                table: "AppEncounters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "M3SubjectTypeId",
                table: "AppEncounters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "QueueTicketId",
                table: "AppEncounters",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReceiptPaidStatus",
                table: "AppEncounters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VisitReason",
                table: "AppEncounters",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyStamp",
                table: "AppClinicalExaminations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsSummaryLocked",
                table: "AppClinicalExaminations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedAt",
                table: "AppClinicalExaminations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LockedByLeadDoctorId",
                table: "AppClinicalExaminations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppConclusionAdviceDictionaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemType = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ContentVi = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ContentEn = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DefaultGradeRef = table.Column<int>(type: "integer", nullable: true),
                    SpecialtyCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppConclusionAdviceDictionaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRolePermissionRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoleCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActorGroup = table.Column<int>(type: "integer", nullable: false),
                    ModuleKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    TabKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CanView = table.Column<bool>(type: "boolean", nullable: false),
                    CanCreate = table.Column<bool>(type: "boolean", nullable: false),
                    CanEdit = table.Column<bool>(type: "boolean", nullable: false),
                    CanDelete = table.Column<bool>(type: "boolean", nullable: false),
                    CanSign = table.Column<bool>(type: "boolean", nullable: false),
                    CanPrint = table.Column<bool>(type: "boolean", nullable: false),
                    CanFinalize = table.Column<bool>(type: "boolean", nullable: false),
                    PermissionVersion = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRolePermissionRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppUserPermissionOverrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    TabKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CanView = table.Column<bool>(type: "boolean", nullable: true),
                    CanCreate = table.Column<bool>(type: "boolean", nullable: true),
                    CanEdit = table.Column<bool>(type: "boolean", nullable: true),
                    CanDelete = table.Column<bool>(type: "boolean", nullable: true),
                    CanSign = table.Column<bool>(type: "boolean", nullable: true),
                    CanPrint = table.Column<bool>(type: "boolean", nullable: true),
                    CanFinalize = table.Column<bool>(type: "boolean", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUserPermissionOverrides", x => x.Id);
                });

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS ""AppUsers"" (
    ""Id"" uuid NOT NULL,
    ""BranchId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    ""BranchCode"" character varying(50) NOT NULL DEFAULT 'CNQ1',
    ""LegacyUserId"" integer NULL,
    ""UserName"" character varying(150) NOT NULL,
    ""FullName"" character varying(150) NOT NULL,
    ""StaffLegacyId"" integer NULL,
    ""StaffId"" uuid NULL,
    ""Role"" character varying(50) NOT NULL DEFAULT 'doctor',
    ""ActorGroup"" integer NOT NULL DEFAULT 2,
    ""PasswordHash"" character varying(256) NULL,
    ""IsActive"" boolean NOT NULL DEFAULT TRUE,
    ""LastLoginAt"" timestamp with time zone NULL,
    CONSTRAINT ""PK_AppUsers"" PRIMARY KEY (""Id"")
);
ALTER TABLE ""AppUsers"" ADD COLUMN IF NOT EXISTS ""BranchId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
ALTER TABLE ""AppUsers"" ADD COLUMN IF NOT EXISTS ""BranchCode"" character varying(50) NOT NULL DEFAULT 'CNQ1';
ALTER TABLE ""AppUsers"" ADD COLUMN IF NOT EXISTS ""ActorGroup"" integer NOT NULL DEFAULT 2;
ALTER TABLE ""AppUsers"" ADD COLUMN IF NOT EXISTS ""PasswordHash"" character varying(256) NULL;
ALTER TABLE ""AppUsers"" ADD COLUMN IF NOT EXISTS ""LastLoginAt"" timestamp with time zone NULL;
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_AppUsers_AppStaffs_StaffId') THEN
        ALTER TABLE ""AppUsers"" ADD CONSTRAINT ""FK_AppUsers_AppStaffs_StaffId""
            FOREIGN KEY (""StaffId"") REFERENCES ""AppStaffs"" (""Id"") ON DELETE SET NULL;
    END IF;
END $$;
CREATE INDEX IF NOT EXISTS ""IX_AppUsers_StaffId"" ON ""AppUsers"" (""StaffId"");
CREATE INDEX IF NOT EXISTS ""IX_AppUsers_UserName"" ON ""AppUsers"" (""UserName"");
");

            migrationBuilder.CreateTable(
                name: "ReceiptItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "text", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemType = table.Column<int>(type: "integer", nullable: false),
                    MedicalOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    MedicalOrderItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    PrescriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    PrescriptionItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemCode = table.Column<string>(type: "text", nullable: false),
                    ItemName = table.Column<string>(type: "text", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceiptItems_AppReceipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalTable: "AppReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppRef_ICD_MaICD",
                table: "AppRef_ICD",
                column: "MaICD");

            migrationBuilder.CreateIndex(
                name: "IX_AppPatients_InsuranceCardNumber",
                table: "AppPatients",
                column: "InsuranceCardNumber");

            migrationBuilder.CreateIndex(
                name: "IX_AppPackages_Scope_IsActive",
                table: "AppPackages",
                columns: new[] { "Scope", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AppEncounters_QueueTicketId",
                table: "AppEncounters",
                column: "QueueTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_AppConclusionAdviceDictionaries_ItemType_Code",
                table: "AppConclusionAdviceDictionaries",
                columns: new[] { "ItemType", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppRolePermissionRules_BranchId_RoleCode_ActorGroup_TabKey",
                table: "AppRolePermissionRules",
                columns: new[] { "BranchId", "RoleCode", "ActorGroup", "TabKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUserPermissionOverrides_UserId_BranchId_TabKey",
                table: "AppUserPermissionOverrides",
                columns: new[] { "UserId", "BranchId", "TabKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptItems_ReceiptId",
                table: "ReceiptItems",
                column: "ReceiptId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppConclusionAdviceDictionaries");

            migrationBuilder.DropTable(
                name: "AppRolePermissionRules");

            migrationBuilder.DropTable(
                name: "AppUserPermissionOverrides");

            migrationBuilder.DropTable(
                name: "AppUsers");

            migrationBuilder.DropTable(
                name: "ReceiptItems");

            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_AppRef_ICD_MaICD\";");

            migrationBuilder.DropIndex(
                name: "IX_AppPatients_InsuranceCardNumber",
                table: "AppPatients");

            migrationBuilder.DropIndex(
                name: "IX_AppPackages_Scope_IsActive",
                table: "AppPackages");

            migrationBuilder.DropIndex(
                name: "IX_AppEncounters_QueueTicketId",
                table: "AppEncounters");

            migrationBuilder.DropColumn(
                name: "CanOrderCls",
                table: "AppStaffs");

            migrationBuilder.DropColumn(
                name: "CanPrescribe",
                table: "AppStaffs");

            migrationBuilder.DropColumn(
                name: "CanViewFullClinicalHistory",
                table: "AppStaffs");

            migrationBuilder.DropColumn(
                name: "ClinicalActorGroup",
                table: "AppStaffs");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "AppReceipts");

            migrationBuilder.DropColumn(
                name: "CallAttemptCount",
                table: "AppQueueAssignments");

            migrationBuilder.DropColumn(
                name: "PriorityRecall",
                table: "AppQueueAssignments");

            migrationBuilder.DropColumn(
                name: "ReturnedFromClsAt",
                table: "AppQueueAssignments");

            migrationBuilder.DropColumn(
                name: "SentToClsAt",
                table: "AppQueueAssignments");

            migrationBuilder.DropColumn(
                name: "IsExternalPurchase",
                table: "AppPrescriptions");

            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "AppPrescriptions");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "AppPrescriptions");

            migrationBuilder.DropColumn(
                name: "ReceiptId",
                table: "AppPrescriptions");

            migrationBuilder.DropColumn(
                name: "SpecialtyType",
                table: "AppPrescriptions");

            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "AppPatients");

            migrationBuilder.DropColumn(
                name: "BhytAddress",
                table: "AppPatients");

            migrationBuilder.DropColumn(
                name: "BhytAreaCode",
                table: "AppPatients");

            migrationBuilder.DropColumn(
                name: "BhytBenefitRate",
                table: "AppPatients");

            migrationBuilder.DropColumn(
                name: "BhytInitialFacilityCode",
                table: "AppPatients");

            migrationBuilder.DropColumn(
                name: "BhytValidFrom",
                table: "AppPatients");

            migrationBuilder.DropColumn(
                name: "BhytValidTo",
                table: "AppPatients");

            migrationBuilder.DropColumn(
                name: "BloodTypeId",
                table: "AppPatients");

            migrationBuilder.DropColumn(
                name: "CityCode",
                table: "AppPatients");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "AppPatients");

            migrationBuilder.DropColumn(
                name: "DanTocId",
                table: "AppPatients");

            migrationBuilder.DropColumn(
                name: "FamilyHistoryDiseaseIds",
                table: "AppPatients");

            migrationBuilder.DropColumn(
                name: "FamilyHistoryIcdCodes",
                table: "AppPatients");

            migrationBuilder.DropColumn(
                name: "NationalityCode",
                table: "AppPatients");

            migrationBuilder.DropColumn(
                name: "NgheNghiepCode",
                table: "AppPatients");

            migrationBuilder.DropColumn(
                name: "NgheNghiepId",
                table: "AppPatients");

            migrationBuilder.DropColumn(
                name: "PersonalHistoryJson",
                table: "AppPatients");

            migrationBuilder.DropColumn(
                name: "RhFactorId",
                table: "AppPatients");

            migrationBuilder.DropColumn(
                name: "WardCode",
                table: "AppPatients");

            migrationBuilder.DropColumn(
                name: "WardId",
                table: "AppPatients");

            migrationBuilder.DropColumn(
                name: "WorkPlaceName",
                table: "AppPatients");

            migrationBuilder.DropColumn(
                name: "WorkPlaceWardCode",
                table: "AppPatients");

            migrationBuilder.DropColumn(
                name: "ApplicableGender",
                table: "AppPackages");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AppPackages");

            migrationBuilder.DropColumn(
                name: "MaxAge",
                table: "AppPackages");

            migrationBuilder.DropColumn(
                name: "MinAge",
                table: "AppPackages");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "AppPackages");

            migrationBuilder.DropColumn(
                name: "TargetPrice",
                table: "AppPackages");

            migrationBuilder.DropColumn(
                name: "ValidFrom",
                table: "AppPackages");

            migrationBuilder.DropColumn(
                name: "ValidTo",
                table: "AppPackages");

            migrationBuilder.DropColumn(
                name: "AdditionalSummaryNotes",
                table: "AppMedicalRecordSummaries");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "AppMedicalRecordSummaries");

            migrationBuilder.DropColumn(
                name: "IsAdditionalOutOfPackageTab",
                table: "AppMedicalRecordSummaries");

            migrationBuilder.DropColumn(
                name: "IsHealthQualified",
                table: "AppMedicalRecordSummaries");

            migrationBuilder.DropColumn(
                name: "SummaryStatus",
                table: "AppMedicalRecordSummaries");

            migrationBuilder.DropColumn(
                name: "SpecialtyType",
                table: "AppMedicalOrders");

            migrationBuilder.DropColumn(
                name: "IsSelectedForPayment",
                table: "AppMedicalOrderItems");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "AppMedicalOrderItems");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "AppMedicalOrderItems");

            migrationBuilder.DropColumn(
                name: "ReceiptId",
                table: "AppMedicalOrderItems");

            migrationBuilder.DropColumn(
                name: "BhytReasonCode",
                table: "AppEncounters");

            migrationBuilder.DropColumn(
                name: "BhytReferralCode",
                table: "AppEncounters");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "AppEncounters");

            migrationBuilder.DropColumn(
                name: "M3LocationId",
                table: "AppEncounters");

            migrationBuilder.DropColumn(
                name: "M3PaymentDetailId",
                table: "AppEncounters");

            migrationBuilder.DropColumn(
                name: "M3PaymentSourceId",
                table: "AppEncounters");

            migrationBuilder.DropColumn(
                name: "M3SubjectTypeId",
                table: "AppEncounters");

            migrationBuilder.DropColumn(
                name: "QueueTicketId",
                table: "AppEncounters");

            migrationBuilder.DropColumn(
                name: "ReceiptPaidStatus",
                table: "AppEncounters");

            migrationBuilder.DropColumn(
                name: "VisitReason",
                table: "AppEncounters");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "AppClinicalExaminations");

            migrationBuilder.DropColumn(
                name: "IsSummaryLocked",
                table: "AppClinicalExaminations");

            migrationBuilder.DropColumn(
                name: "LockedAt",
                table: "AppClinicalExaminations");

            migrationBuilder.DropColumn(
                name: "LockedByLeadDoctorId",
                table: "AppClinicalExaminations");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AppRef_YeuToNhomMau",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AppRef_YesNo",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AppRef_TrangThaiXetNghiem",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AppRef_TinhTrangRang",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AppRef_PhanLoaiSucKhoe",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AppRef_NhomMau",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AppRef_KetQuaMaTuy",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AppRef_GioiTinh",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "SoDangKy",
                table: "AppRef_DuocQuocGia",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MaThuoc",
                table: "AppRef_DuocQuocGia",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<Guid>(
                name: "ContractId",
                table: "AppPackages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "AppPackages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppRef_ICD_MaICD",
                table: "AppRef_ICD",
                column: "MaICD",
                unique: true);
        }
    }
}
