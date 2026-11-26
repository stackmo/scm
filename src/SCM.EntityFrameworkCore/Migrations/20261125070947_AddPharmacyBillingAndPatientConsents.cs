using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddPharmacyBillingAndPatientConsents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ElectronicInvoiceId",
                table: "AppGppRetailSales",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ElectronicInvoiceNumber",
                table: "AppGppRetailSales",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReceiptId",
                table: "AppGppRetailSales",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppPatientConsentSignatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EncounterId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncounterCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: true),
                    PatientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FormType = table.Column<int>(type: "integer", nullable: false),
                    FormCode = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    FormTitle = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    ContentSnapshotJson = table.Column<string>(type: "text", nullable: false),
                    SignerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SignerRelation = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SignatureImageBase64 = table.Column<string>(type: "text", nullable: false),
                    SignatureHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CollectedByStaffId = table.Column<Guid>(type: "uuid", nullable: true),
                    CollectedByStaffName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppPatientConsentSignatures", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppGppRetailSales_ElectronicInvoiceId",
                table: "AppGppRetailSales",
                column: "ElectronicInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_AppGppRetailSales_ReceiptId",
                table: "AppGppRetailSales",
                column: "ReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPatientConsentSignatures_BranchId_EncounterId_FormType",
                table: "AppPatientConsentSignatures",
                columns: new[] { "BranchId", "EncounterId", "FormType" });

            migrationBuilder.CreateIndex(
                name: "IX_AppPatientConsentSignatures_BranchId_SignedAt",
                table: "AppPatientConsentSignatures",
                columns: new[] { "BranchId", "SignedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppPatientConsentSignatures");

            migrationBuilder.DropIndex(
                name: "IX_AppGppRetailSales_ElectronicInvoiceId",
                table: "AppGppRetailSales");

            migrationBuilder.DropIndex(
                name: "IX_AppGppRetailSales_ReceiptId",
                table: "AppGppRetailSales");

            migrationBuilder.DropColumn(
                name: "ElectronicInvoiceId",
                table: "AppGppRetailSales");

            migrationBuilder.DropColumn(
                name: "ElectronicInvoiceNumber",
                table: "AppGppRetailSales");

            migrationBuilder.DropColumn(
                name: "ReceiptId",
                table: "AppGppRetailSales");
        }
    }
}
