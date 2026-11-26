using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddLisRisWorkstationAndGateway : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DicomSopInstanceUid",
                table: "CapturedMedias",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationSeconds",
                table: "CapturedMedias",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MediaType",
                table: "CapturedMedias",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrlOrBase64",
                table: "CapturedMedias",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "EmployeeId",
                table: "AppSpecimenTubes",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "BatchEmployeeMappingId",
                table: "AppSpecimenTubes",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "BranchCode",
                table: "AppSpecimenTubes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "AppSpecimenTubes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "EncounterCode",
                table: "AppSpecimenTubes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EncounterId",
                table: "AppSpecimenTubes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MedicalOrderId",
                table: "AppSpecimenTubes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MedicalOrderItemId",
                table: "AppSpecimenTubes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AddendumCount",
                table: "AppLabResultRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AddendumText",
                table: "AppLabResultRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BridgePayloadHash",
                table: "AppLabResultRecords",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyStamp",
                table: "AppLabResultRecords",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "DoctorApprovedById",
                table: "AppLabResultRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DoctorApprovedByName",
                table: "AppLabResultRecords",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DoctorLicenseNo",
                table: "AppLabResultRecords",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DoctorSignedAt",
                table: "AppLabResultRecords",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasAddendum",
                table: "AppLabResultRecords",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "InputSource",
                table: "AppLabResultRecords",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "AppLabResultRecords",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPanic",
                table: "AppLabResultRecords",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "MedicalOrderId",
                table: "AppLabResultRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MedicalOrderItemId",
                table: "AppLabResultRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PanicAckNote",
                table: "AppLabResultRecords",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PanicAcknowledgedAt",
                table: "AppLabResultRecords",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PanicAcknowledgedById",
                table: "AppLabResultRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PanicAcknowledgedByName",
                table: "AppLabResultRecords",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewStatus",
                table: "AppLabResultRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SignatureHash",
                table: "AppLabResultRecords",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TechVerifiedAt",
                table: "AppLabResultRecords",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TechVerifiedById",
                table: "AppLabResultRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechVerifiedByName",
                table: "AppLabResultRecords",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnlockReason",
                table: "AppLabResultRecords",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "AppLabResultRecords",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "Recommendation",
                table: "AppDiagnosticImagingRecords",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "AppDiagnosticImagingRecords",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AddendumCount",
                table: "AppDiagnosticImagingRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AddendumText",
                table: "AppDiagnosticImagingRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CapturedAt",
                table: "AppDiagnosticImagingRecords",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CloTestResult",
                table: "AppDiagnosticImagingRecords",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyStamp",
                table: "AppDiagnosticImagingRecords",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "DicomAccessionNumber",
                table: "AppDiagnosticImagingRecords",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EndoscopyGroup",
                table: "AppDiagnosticImagingRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FilmDeliveredAt",
                table: "AppDiagnosticImagingRecords",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FilmDeliveredById",
                table: "AppDiagnosticImagingRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FilmDeliveredByName",
                table: "AppDiagnosticImagingRecords",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAbnormal",
                table: "AppDiagnosticImagingRecords",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsFilmDelivered",
                table: "AppDiagnosticImagingRecords",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "AppDiagnosticImagingRecords",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LayoutMode",
                table: "AppDiagnosticImagingRecords",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<Guid>(
                name: "MedicalOrderId",
                table: "AppDiagnosticImagingRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MedicalOrderItemId",
                table: "AppDiagnosticImagingRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoomCode",
                table: "AppDiagnosticImagingRecords",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SignatureHash",
                table: "AppDiagnosticImagingRecords",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechNotes",
                table: "AppDiagnosticImagingRecords",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TechnicianId",
                table: "AppDiagnosticImagingRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicianName",
                table: "AppDiagnosticImagingRecords",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnlockReason",
                table: "AppDiagnosticImagingRecords",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "AppDiagnosticImagingRecords",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowStatus",
                table: "AppDiagnosticImagingRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AppDiagnosticAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncounterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ActorRole = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedByName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MaskedPiiSummary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    BeforeSnapshotJson = table.Column<string>(type: "text", nullable: true),
                    AfterSnapshotJson = table.Column<string>(type: "text", nullable: true),
                    TimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppDiagnosticAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppExternalServiceMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScmServiceCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PartnerOrderCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PartnerTestCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PartnerSampleType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StandardUnit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConversionFactor = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppExternalServiceMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppLabPanicThresholds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TestCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TestName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Gender = table.Column<int>(type: "integer", nullable: false),
                    PanicLow = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    PanicHigh = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppLabPanicThresholds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppLisIntegrationOutbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicalOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicalOrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<int>(type: "integer", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    NextRetryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppLisIntegrationOutbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppLisPartnerConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PartnerCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PartnerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProviderType = table.Column<int>(type: "integer", nullable: false),
                    ConnectionConfigEncrypted = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsDefaultForBranch = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppLisPartnerConfigs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppSpecimenTubes_BranchId",
                table: "AppSpecimenTubes",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSpecimenTubes_EncounterId",
                table: "AppSpecimenTubes",
                column: "EncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSpecimenTubes_MedicalOrderId",
                table: "AppSpecimenTubes",
                column: "MedicalOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLabResultRecords_BridgePayloadHash",
                table: "AppLabResultRecords",
                column: "BridgePayloadHash");

            migrationBuilder.CreateIndex(
                name: "IX_AppLabResultRecords_IsPanic",
                table: "AppLabResultRecords",
                column: "IsPanic");

            migrationBuilder.CreateIndex(
                name: "IX_AppLabResultRecords_MedicalOrderId",
                table: "AppLabResultRecords",
                column: "MedicalOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLabResultRecords_ReviewStatus",
                table: "AppLabResultRecords",
                column: "ReviewStatus");

            migrationBuilder.CreateIndex(
                name: "IX_AppDiagnosticImagingRecords_DicomAccessionNumber",
                table: "AppDiagnosticImagingRecords",
                column: "DicomAccessionNumber");

            migrationBuilder.CreateIndex(
                name: "IX_AppDiagnosticImagingRecords_MedicalOrderId",
                table: "AppDiagnosticImagingRecords",
                column: "MedicalOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_AppDiagnosticImagingRecords_RoomCode",
                table: "AppDiagnosticImagingRecords",
                column: "RoomCode");

            migrationBuilder.CreateIndex(
                name: "IX_AppDiagnosticImagingRecords_WorkflowStatus",
                table: "AppDiagnosticImagingRecords",
                column: "WorkflowStatus");

            migrationBuilder.CreateIndex(
                name: "IX_AppDiagnosticAuditLogs_Action",
                table: "AppDiagnosticAuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AppDiagnosticAuditLogs_BranchId",
                table: "AppDiagnosticAuditLogs",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppDiagnosticAuditLogs_EncounterId",
                table: "AppDiagnosticAuditLogs",
                column: "EncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_AppDiagnosticAuditLogs_EntityId",
                table: "AppDiagnosticAuditLogs",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_AppDiagnosticAuditLogs_TimestampUtc",
                table: "AppDiagnosticAuditLogs",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AppExternalServiceMappings_BranchId",
                table: "AppExternalServiceMappings",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppExternalServiceMappings_PartnerId_PartnerTestCode",
                table: "AppExternalServiceMappings",
                columns: new[] { "PartnerId", "PartnerTestCode" });

            migrationBuilder.CreateIndex(
                name: "IX_AppExternalServiceMappings_PartnerId_ScmServiceCode",
                table: "AppExternalServiceMappings",
                columns: new[] { "PartnerId", "ScmServiceCode" });

            migrationBuilder.CreateIndex(
                name: "IX_AppLabPanicThresholds_BranchId",
                table: "AppLabPanicThresholds",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLabPanicThresholds_BranchId_TestCode_Gender",
                table: "AppLabPanicThresholds",
                columns: new[] { "BranchId", "TestCode", "Gender" });

            migrationBuilder.CreateIndex(
                name: "IX_AppLisIntegrationOutbox_BranchId",
                table: "AppLisIntegrationOutbox",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLisIntegrationOutbox_IdempotencyKey",
                table: "AppLisIntegrationOutbox",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppLisIntegrationOutbox_MedicalOrderItemId",
                table: "AppLisIntegrationOutbox",
                column: "MedicalOrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLisIntegrationOutbox_Status_NextRetryAt",
                table: "AppLisIntegrationOutbox",
                columns: new[] { "Status", "NextRetryAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AppLisPartnerConfigs_BranchId",
                table: "AppLisPartnerConfigs",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLisPartnerConfigs_BranchId_PartnerCode",
                table: "AppLisPartnerConfigs",
                columns: new[] { "BranchId", "PartnerCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppDiagnosticAuditLogs");

            migrationBuilder.DropTable(
                name: "AppExternalServiceMappings");

            migrationBuilder.DropTable(
                name: "AppLabPanicThresholds");

            migrationBuilder.DropTable(
                name: "AppLisIntegrationOutbox");

            migrationBuilder.DropTable(
                name: "AppLisPartnerConfigs");

            migrationBuilder.DropIndex(
                name: "IX_AppSpecimenTubes_BranchId",
                table: "AppSpecimenTubes");

            migrationBuilder.DropIndex(
                name: "IX_AppSpecimenTubes_EncounterId",
                table: "AppSpecimenTubes");

            migrationBuilder.DropIndex(
                name: "IX_AppSpecimenTubes_MedicalOrderId",
                table: "AppSpecimenTubes");

            migrationBuilder.DropIndex(
                name: "IX_AppLabResultRecords_BridgePayloadHash",
                table: "AppLabResultRecords");

            migrationBuilder.DropIndex(
                name: "IX_AppLabResultRecords_IsPanic",
                table: "AppLabResultRecords");

            migrationBuilder.DropIndex(
                name: "IX_AppLabResultRecords_MedicalOrderId",
                table: "AppLabResultRecords");

            migrationBuilder.DropIndex(
                name: "IX_AppLabResultRecords_ReviewStatus",
                table: "AppLabResultRecords");

            migrationBuilder.DropIndex(
                name: "IX_AppDiagnosticImagingRecords_DicomAccessionNumber",
                table: "AppDiagnosticImagingRecords");

            migrationBuilder.DropIndex(
                name: "IX_AppDiagnosticImagingRecords_MedicalOrderId",
                table: "AppDiagnosticImagingRecords");

            migrationBuilder.DropIndex(
                name: "IX_AppDiagnosticImagingRecords_RoomCode",
                table: "AppDiagnosticImagingRecords");

            migrationBuilder.DropIndex(
                name: "IX_AppDiagnosticImagingRecords_WorkflowStatus",
                table: "AppDiagnosticImagingRecords");

            migrationBuilder.DropColumn(
                name: "DicomSopInstanceUid",
                table: "CapturedMedias");

            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                table: "CapturedMedias");

            migrationBuilder.DropColumn(
                name: "MediaType",
                table: "CapturedMedias");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrlOrBase64",
                table: "CapturedMedias");

            migrationBuilder.DropColumn(
                name: "BranchCode",
                table: "AppSpecimenTubes");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "AppSpecimenTubes");

            migrationBuilder.DropColumn(
                name: "EncounterCode",
                table: "AppSpecimenTubes");

            migrationBuilder.DropColumn(
                name: "EncounterId",
                table: "AppSpecimenTubes");

            migrationBuilder.DropColumn(
                name: "MedicalOrderId",
                table: "AppSpecimenTubes");

            migrationBuilder.DropColumn(
                name: "MedicalOrderItemId",
                table: "AppSpecimenTubes");

            migrationBuilder.DropColumn(
                name: "AddendumCount",
                table: "AppLabResultRecords");

            migrationBuilder.DropColumn(
                name: "AddendumText",
                table: "AppLabResultRecords");

            migrationBuilder.DropColumn(
                name: "BridgePayloadHash",
                table: "AppLabResultRecords");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "AppLabResultRecords");

            migrationBuilder.DropColumn(
                name: "DoctorApprovedById",
                table: "AppLabResultRecords");

            migrationBuilder.DropColumn(
                name: "DoctorApprovedByName",
                table: "AppLabResultRecords");

            migrationBuilder.DropColumn(
                name: "DoctorLicenseNo",
                table: "AppLabResultRecords");

            migrationBuilder.DropColumn(
                name: "DoctorSignedAt",
                table: "AppLabResultRecords");

            migrationBuilder.DropColumn(
                name: "HasAddendum",
                table: "AppLabResultRecords");

            migrationBuilder.DropColumn(
                name: "InputSource",
                table: "AppLabResultRecords");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "AppLabResultRecords");

            migrationBuilder.DropColumn(
                name: "IsPanic",
                table: "AppLabResultRecords");

            migrationBuilder.DropColumn(
                name: "MedicalOrderId",
                table: "AppLabResultRecords");

            migrationBuilder.DropColumn(
                name: "MedicalOrderItemId",
                table: "AppLabResultRecords");

            migrationBuilder.DropColumn(
                name: "PanicAckNote",
                table: "AppLabResultRecords");

            migrationBuilder.DropColumn(
                name: "PanicAcknowledgedAt",
                table: "AppLabResultRecords");

            migrationBuilder.DropColumn(
                name: "PanicAcknowledgedById",
                table: "AppLabResultRecords");

            migrationBuilder.DropColumn(
                name: "PanicAcknowledgedByName",
                table: "AppLabResultRecords");

            migrationBuilder.DropColumn(
                name: "ReviewStatus",
                table: "AppLabResultRecords");

            migrationBuilder.DropColumn(
                name: "SignatureHash",
                table: "AppLabResultRecords");

            migrationBuilder.DropColumn(
                name: "TechVerifiedAt",
                table: "AppLabResultRecords");

            migrationBuilder.DropColumn(
                name: "TechVerifiedById",
                table: "AppLabResultRecords");

            migrationBuilder.DropColumn(
                name: "TechVerifiedByName",
                table: "AppLabResultRecords");

            migrationBuilder.DropColumn(
                name: "UnlockReason",
                table: "AppLabResultRecords");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "AppLabResultRecords");

            migrationBuilder.DropColumn(
                name: "AddendumCount",
                table: "AppDiagnosticImagingRecords");

            migrationBuilder.DropColumn(
                name: "AddendumText",
                table: "AppDiagnosticImagingRecords");

            migrationBuilder.DropColumn(
                name: "CapturedAt",
                table: "AppDiagnosticImagingRecords");

            migrationBuilder.DropColumn(
                name: "CloTestResult",
                table: "AppDiagnosticImagingRecords");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "AppDiagnosticImagingRecords");

            migrationBuilder.DropColumn(
                name: "DicomAccessionNumber",
                table: "AppDiagnosticImagingRecords");

            migrationBuilder.DropColumn(
                name: "EndoscopyGroup",
                table: "AppDiagnosticImagingRecords");

            migrationBuilder.DropColumn(
                name: "FilmDeliveredAt",
                table: "AppDiagnosticImagingRecords");

            migrationBuilder.DropColumn(
                name: "FilmDeliveredById",
                table: "AppDiagnosticImagingRecords");

            migrationBuilder.DropColumn(
                name: "FilmDeliveredByName",
                table: "AppDiagnosticImagingRecords");

            migrationBuilder.DropColumn(
                name: "IsAbnormal",
                table: "AppDiagnosticImagingRecords");

            migrationBuilder.DropColumn(
                name: "IsFilmDelivered",
                table: "AppDiagnosticImagingRecords");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "AppDiagnosticImagingRecords");

            migrationBuilder.DropColumn(
                name: "LayoutMode",
                table: "AppDiagnosticImagingRecords");

            migrationBuilder.DropColumn(
                name: "MedicalOrderId",
                table: "AppDiagnosticImagingRecords");

            migrationBuilder.DropColumn(
                name: "MedicalOrderItemId",
                table: "AppDiagnosticImagingRecords");

            migrationBuilder.DropColumn(
                name: "RoomCode",
                table: "AppDiagnosticImagingRecords");

            migrationBuilder.DropColumn(
                name: "SignatureHash",
                table: "AppDiagnosticImagingRecords");

            migrationBuilder.DropColumn(
                name: "TechNotes",
                table: "AppDiagnosticImagingRecords");

            migrationBuilder.DropColumn(
                name: "TechnicianId",
                table: "AppDiagnosticImagingRecords");

            migrationBuilder.DropColumn(
                name: "TechnicianName",
                table: "AppDiagnosticImagingRecords");

            migrationBuilder.DropColumn(
                name: "UnlockReason",
                table: "AppDiagnosticImagingRecords");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "AppDiagnosticImagingRecords");

            migrationBuilder.DropColumn(
                name: "WorkflowStatus",
                table: "AppDiagnosticImagingRecords");

            migrationBuilder.AlterColumn<Guid>(
                name: "EmployeeId",
                table: "AppSpecimenTubes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "BatchEmployeeMappingId",
                table: "AppSpecimenTubes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Recommendation",
                table: "AppDiagnosticImagingRecords",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "AppDiagnosticImagingRecords",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true);
        }
    }
}
