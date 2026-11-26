using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddReceptionBillingEInvoiceEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BranchCode",
                table: "NationalGatewayConfigs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "NationalGatewayConfigs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultSeriesCode",
                table: "NationalGatewayConfigs",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultTemplateCode",
                table: "NationalGatewayConfigs",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderCode",
                table: "NationalGatewayConfigs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TaxCode",
                table: "NationalGatewayConfigs",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CashierShiftId",
                table: "AppReceipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyStamp",
                table: "AppReceipts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "DepositAppliedAmount",
                table: "AppReceipts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "ElectronicInvoiceId",
                table: "AppReceipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ElectronicInvoiceNumber",
                table: "AppReceipts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountName",
                table: "AppBranches",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BankAccountNo",
                table: "AppBranches",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BankBin",
                table: "AppBranches",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "EnableDepositManagement",
                table: "AppBranches",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxCode",
                table: "AppBranches",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AppBillingSequences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Prefix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DatePeriod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CurrentNumber = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppBillingSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppCashierShifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CashierStaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    CashierName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OpeningCash = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CashCollected = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CashRefunded = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ExpectedCash = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CountedCash = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CashVariance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ExpectedTransfer = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ExpectedPos = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CloseNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppCashierShifts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppElectronicInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InvoiceSeriesId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierTaxCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TemplateCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SeriesCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProviderCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProviderTransactionId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ReservationCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LookupCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BuyerType = table.Column<int>(type: "integer", nullable: false),
                    BuyerName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    BuyerTaxCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    BuyerAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BuyerEmail = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    PaymentMethodName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CompanySettlementId = table.Column<Guid>(type: "uuid", nullable: true),
                    TotalAmountBeforeVat = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    VatRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    VatAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OriginalInvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CancelOrAdjustReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PdfUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    XmlPayload = table.Column<string>(type: "text", nullable: true),
                    IssuedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedByName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppElectronicInvoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppInvoiceSeries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TaxCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TemplateCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SeriesCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FromNumber = table.Column<int>(type: "integer", nullable: false),
                    ToNumber = table.Column<int>(type: "integer", nullable: false),
                    CurrentNumber = table.Column<int>(type: "integer", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppInvoiceSeries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppElectronicInvoiceItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ElectronicInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    ItemCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ItemName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    VatRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReceiptItemId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppElectronicInvoiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppElectronicInvoiceItems_AppElectronicInvoices_ElectronicI~",
                        column: x => x.ElectronicInvoiceId,
                        principalTable: "AppElectronicInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppElectronicInvoiceReceiptLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ElectronicInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppElectronicInvoiceReceiptLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppElectronicInvoiceReceiptLinks_AppElectronicInvoices_Elec~",
                        column: x => x.ElectronicInvoiceId,
                        principalTable: "AppElectronicInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppBillingSequences_BranchId_Prefix_DatePeriod",
                table: "AppBillingSequences",
                columns: new[] { "BranchId", "Prefix", "DatePeriod" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppCashierShifts_BranchId_CashierStaffId_Status",
                table: "AppCashierShifts",
                columns: new[] { "BranchId", "CashierStaffId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AppElectronicInvoiceItems_ElectronicInvoiceId",
                table: "AppElectronicInvoiceItems",
                column: "ElectronicInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_AppElectronicInvoiceReceiptLinks_ElectronicInvoiceId",
                table: "AppElectronicInvoiceReceiptLinks",
                column: "ElectronicInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_AppElectronicInvoiceReceiptLinks_ReceiptId",
                table: "AppElectronicInvoiceReceiptLinks",
                column: "ReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_AppElectronicInvoices_BranchId_TemplateCode_SeriesCode_Invo~",
                table: "AppElectronicInvoices",
                columns: new[] { "BranchId", "TemplateCode", "SeriesCode", "InvoiceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppElectronicInvoices_CompanySettlementId",
                table: "AppElectronicInvoices",
                column: "CompanySettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_AppElectronicInvoices_Status",
                table: "AppElectronicInvoices",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppInvoiceSeries_BranchId_TemplateCode_SeriesCode",
                table: "AppInvoiceSeries",
                columns: new[] { "BranchId", "TemplateCode", "SeriesCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppBillingSequences");

            migrationBuilder.DropTable(
                name: "AppCashierShifts");

            migrationBuilder.DropTable(
                name: "AppElectronicInvoiceItems");

            migrationBuilder.DropTable(
                name: "AppElectronicInvoiceReceiptLinks");

            migrationBuilder.DropTable(
                name: "AppInvoiceSeries");

            migrationBuilder.DropTable(
                name: "AppElectronicInvoices");

            migrationBuilder.DropColumn(
                name: "BranchCode",
                table: "NationalGatewayConfigs");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "NationalGatewayConfigs");

            migrationBuilder.DropColumn(
                name: "DefaultSeriesCode",
                table: "NationalGatewayConfigs");

            migrationBuilder.DropColumn(
                name: "DefaultTemplateCode",
                table: "NationalGatewayConfigs");

            migrationBuilder.DropColumn(
                name: "ProviderCode",
                table: "NationalGatewayConfigs");

            migrationBuilder.DropColumn(
                name: "TaxCode",
                table: "NationalGatewayConfigs");

            migrationBuilder.DropColumn(
                name: "CashierShiftId",
                table: "AppReceipts");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "AppReceipts");

            migrationBuilder.DropColumn(
                name: "DepositAppliedAmount",
                table: "AppReceipts");

            migrationBuilder.DropColumn(
                name: "ElectronicInvoiceId",
                table: "AppReceipts");

            migrationBuilder.DropColumn(
                name: "ElectronicInvoiceNumber",
                table: "AppReceipts");

            migrationBuilder.DropColumn(
                name: "BankAccountName",
                table: "AppBranches");

            migrationBuilder.DropColumn(
                name: "BankAccountNo",
                table: "AppBranches");

            migrationBuilder.DropColumn(
                name: "BankBin",
                table: "AppBranches");

            migrationBuilder.DropColumn(
                name: "EnableDepositManagement",
                table: "AppBranches");

            migrationBuilder.DropColumn(
                name: "TaxCode",
                table: "AppBranches");
        }
    }
}
