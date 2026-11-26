using Microsoft.EntityFrameworkCore;
using SCM.Application.Billing;
using SCM.Application.Contracts.Billing.Dtos;
using SCM.Domain.Billing;
using SCM.Domain.Billing.Exceptions;
using SCM.Domain.Branches;
using SCM.Domain.Shared.Enums;
using SCM.EntityFrameworkCore;
using Xunit;

namespace SCM.Application.Tests;

public class Wave8_Cashier_BhytXml_Settlement_Unit_Tests
{
    private SCMDbContext CreateTestContext()
    {
        var options = new DbContextOptionsBuilder<SCMDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new SCMDbContext(options);
    }

    // =========================================================================
    // AC-8.1: VietQR động phân định tài khoản ngân hàng thụ hưởng theo Chi nhánh
    // =========================================================================
    [Fact]
    public async Task GenerateVietQr_Uses_Branch_Specific_Account()
    {
        var context = CreateTestContext();
        var branch1 = new CurrentBranch(Guid.NewGuid(), "BR01", isAllBranches: false, role: "cashier");
        var branch2 = new CurrentBranch(Guid.NewGuid(), "BR02", isAllBranches: false, role: "cashier");

        var service1 = new BillingAppService(context, branch1);
        var service2 = new BillingAppService(context, branch2);

        var dto1 = new CreateReceiptDto
        {
            EncounterId = Guid.NewGuid(),
            EncounterCode = "ENC-BR01-001",
            PatientId = Guid.NewGuid(),
            PatientName = "Bệnh nhân HCM",
            TotalAmount = 350000,
            PaymentMethod = (int)PaymentMethod.VietQR,
            CashierId = Guid.NewGuid(),
            CashierName = "Thu Ngân 1"
        };

        var dto2 = new CreateReceiptDto
        {
            EncounterId = Guid.NewGuid(),
            EncounterCode = "ENC-BR02-002",
            PatientId = Guid.NewGuid(),
            PatientName = "Bệnh nhân HN",
            TotalAmount = 450000,
            PaymentMethod = (int)PaymentMethod.VietQR,
            CashierId = Guid.NewGuid(),
            CashierName = "Thu Ngân 2"
        };

        // When
        var rec1 = await service1.CreateReceiptAsync(dto1);
        var rec2 = await service2.CreateReceiptAsync(dto2);

        // Then: Chi nhánh BR01 mang STK Vietcombank (0071001234567), BR02 mang STK VietinBank (1903009876543)
        Assert.NotNull(rec1.VietQrPayload);
        Assert.NotNull(rec2.VietQrPayload);
        Assert.Contains("0071001234567", rec1.VietQrPayload);
        Assert.Contains("1903009876543", rec2.VietQrPayload);
        Assert.NotEqual(rec1.VietQrPayload, rec2.VietQrPayload);
    }

    // =========================================================================
    // AC-8.2: Kiểm chuẩn phương trình cân bằng chi phí BHYT QĐ 130
    // =========================================================================
    [Fact]
    public async Task Export_15_Xml_Passes_Balance_And_Schema()
    {
        var context = CreateTestContext();
        var service = new BillingAppService(context);

        var thuoc = new List<BhytXmlItemInput>
        {
            new() { MaDichVu = "TH01", TenDichVu = "Thuốc A", NhomChiPhi = 4, DonGia = 40000, SoLuong = 2 }, // 80.000
            new() { MaDichVu = "TH02", TenDichVu = "Thuốc B", NhomChiPhi = 4, DonGia = 40000, SoLuong = 1 }  // 40.000 => 120.000
        };

        var vtyt = new List<BhytXmlItemInput>
        {
            new() { MaDichVu = "VT01", TenDichVu = "Bơm tiêm", NhomChiPhi = 10, DonGia = 30000, SoLuong = 1 } // 30.000
        };

        var dvkt = new List<BhytXmlItemInput>
        {
            new() { MaDichVu = "DV01", TenDichVu = "Khám lâm sàng", NhomChiPhi = 1, DonGia = 40000, SoLuong = 1 } // 40.000
        };

        // When
        var result = await service.ExportBhytXml130Async("79001", "GD4790123456789", "Nguyễn Văn BHYT", thuoc, vtyt, dvkt);

        // Then
        Assert.True(result.IsBalanced);
        Assert.Equal(190000, result.TotalCost); // 120k + 30k + 40k = 190k
        Assert.Contains("<T_TONGCHI>190000</T_TONGCHI>", result.Xml1TongHop);
        Assert.Contains("<T_THUOC>120000</T_THUOC>", result.Xml1TongHop);
        Assert.Contains("<T_VTYT>30000</T_VTYT>", result.Xml1TongHop);
        Assert.Contains("<T_DVKT>40000</T_DVKT>", result.Xml1TongHop);
    }

    // =========================================================================
    // AC-8.3: Chống gian lận thu ngân: Cấm sửa phiếu PAID, chỉ hoàn tiền khi Admin duyệt
    // =========================================================================
    [Fact]
    public async Task Receipt_Cannot_Be_Modified_Without_Admin_Refund()
    {
        var context = CreateTestContext();
        var service = new BillingAppService(context);

        var rec = await service.CreateReceiptAsync(new CreateReceiptDto
        {
            EncounterId = Guid.NewGuid(),
            EncounterCode = "ENC-PAID",
            PatientId = Guid.NewGuid(),
            PatientName = "Lê Thị Tiền Mặt",
            TotalAmount = 200000,
            PaymentMethod = (int)PaymentMethod.Cash,
            CashierId = Guid.NewGuid(),
            CashierName = "Thu Ngân 1"
        });

        // Xác nhận thanh toán PAID
        await service.ConfirmPaymentAsync(rec.Id, "TX-CASH-001");

        var paidReceipt = await context.Receipts.FirstAsync(r => r.Id == rec.Id);
        Assert.Equal(ReceiptStatus.Paid, paidReceipt.Status);

        // 1. Cấm sửa trực tiếp phiếu đã thanh toán
        Assert.Throws<NotSupportedException>(() => paidReceipt.AssertCanModify());
        context.Entry(paidReceipt).State = EntityState.Detached;

        // 2. Thu ngân không có quyền Admin cố hoàn tiền -> Throws RefundUnauthorizedException
        var nonAdminId = Guid.NewGuid();
        var ex = await Assert.ThrowsAsync<RefundUnauthorizedException>(() =>
            service.ProcessRefundAsync(new RefundRequestDto
            {
                ReceiptId = rec.Id,
                AdminId = nonAdminId,
                AdminName = "Nhân viên thu ngân",
                IsAdminRole = false,
                RefundAmount = 200000,
                RefundReason = "Bệnh nhân hủy dịch vụ"
            }));

        Assert.Equal("SCM:BIL:009", ex.Code);
        Assert.Contains("Chỉ Quản trị viên chi nhánh", ex.Message);

        // 3. Admin chi nhánh phê duyệt hoàn tiền -> Thành công
        var adminId = Guid.NewGuid();
        var refunded = await service.ProcessRefundAsync(new RefundRequestDto
        {
            ReceiptId = rec.Id,
            AdminId = adminId,
            AdminName = "Quản Trị Viên Trưởng",
            IsAdminRole = true,
            RefundAmount = 200000,
            RefundReason = "Bác sĩ chỉ định hủy dịch vụ cận lâm sàng"
        });

        Assert.Equal((int)ReceiptStatus.Refunded, refunded.Status);
    }

    // =========================================================================
    // AC-8.4: Quyết toán đoàn B2B tính chính xác theo số nhân sự thực tế đã khám
    // =========================================================================
    [Fact]
    public async Task B2b_Settlement_Calculates_Exact_Actual_Exams()
    {
        var context = CreateTestContext();
        var service = new SettlementAppService(context);

        // Given: Hợp đồng 1.000 người, đơn giá 500k, chiết khấu 10%, thực tế khám 920 người, vắng 80 người
        var input = new CalculateB2bSettlementDto
        {
            CompanyContractId = Guid.NewGuid(),
            ContractNumber = "HD-B2B-2026-001",
            CompanyName = "Tập Đoàn Đồ Chơi Trẻ Em Toàn Quốc",
            TotalContractEmployees = 1000,
            ActualCompletedEmployees = 920,
            UnitPricePerEmployee = 500000,
            DiscountPercentage = 10
        };

        // When
        var settlement = await service.CalculateB2bSettlementAsync(input);

        // Then
        // Gross = 920 * 500.000 = 460.000.000 VND
        // Discount = 460.000.000 * 10% = 46.000.000 VND
        // Net = 414.000.000 VND
        Assert.Equal(80, settlement.AbsentEmployees);
        Assert.Equal(460000000m, settlement.GrossAmount);
        Assert.Equal(46000000m, settlement.DiscountAmount);
        Assert.Equal(414000000m, settlement.NetAmount);
    }

    // =========================================================================
    // AC-8.5: Phát hành Hóa đơn điện tử FAST ERP với số hóa đơn và mã tra cứu
    // =========================================================================
    [Fact]
    public async Task Issue_Fast_EInvoice_Generates_LookupCode()
    {
        var context = CreateTestContext();
        var service = new SettlementAppService(context);

        var settlement = await service.CalculateB2bSettlementAsync(new CalculateB2bSettlementDto
        {
            CompanyContractId = Guid.NewGuid(),
            ContractNumber = "HD-002",
            CompanyName = "Công ty TNHH SmartClinic",
            TotalContractEmployees = 100,
            ActualCompletedEmployees = 100,
            UnitPricePerEmployee = 600000,
            DiscountPercentage = 5
        });

        // When
        var invoiced = await service.IssueFastEInvoiceAsync(settlement.Id, "1C26T");

        // Then
        Assert.NotNull(invoiced.InvoiceNumber);
        Assert.StartsWith("INV", invoiced.InvoiceNumber);
        Assert.NotNull(invoiced.InvoiceLookupCode);
        Assert.StartsWith("FAST-", invoiced.InvoiceLookupCode);
        Assert.Equal((int)SettlementStatus.Invoiced, invoiced.Status);
    }
}
