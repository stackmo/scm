using System;
using SCM.Domain.Billing;
using SCM.Domain.Billing.Exceptions;
using SCM.Domain.Clinical;
using SCM.Domain.Pharmacy.Exceptions;
using SCM.Domain.Shared.Enums;
using PrescriptionEntity = SCM.Domain.Prescription.Prescription;
using Xunit;

namespace SCM.Domain.Tests;

public class BillingGatewayTests
{
    [Fact]
    public void Receipt_Should_Add_ReceiptItems_Correctly()
    {
        var encounterId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var cashierId = Guid.NewGuid();
        var receipt = new Receipt(
            encounterId,
            "ENC-2026-001",
            patientId,
            "Nguyen Van An",
            "REC-001",
            500000m,
            PaymentMethod.VietQR,
            cashierId,
            "Cashier 1");

        var orderId = Guid.NewGuid();
        var orderItemId = Guid.NewGuid();

        receipt.AddItem(
            ReceiptItemType.ParaclinicalService,
            "US_ABDOMEN",
            "Sieu am o bung tong quat",
            250000m,
            1,
            orderId,
            orderItemId);

        receipt.AddItem(
            ReceiptItemType.ParaclinicalService,
            "US_THYROID",
            "Sieu am tuyen giap",
            250000m,
            1,
            orderId,
            Guid.NewGuid());

        Assert.Equal(2, receipt.Items.Count);
        Assert.Equal(250000m, receipt.Items[0].TotalPrice);
        Assert.Equal(orderItemId, receipt.Items[0].MedicalOrderItemId);
    }

    [Fact]
    public void MedicalOrder_ExecutionGate_Should_Block_Unpaid_OutOfPackage_Service()
    {
        var encounterId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();

        // 1. Chỉ định ngoài gói (IsInPackage = false)
        var order = new MedicalOrder(
            encounterId,
            "ENC-2026-001",
            patientId,
            "Tran Van B",
            doctorId,
            "BS. Tri",
            MedicalOrderType.DiagnosticImaging,
            isInPackage: false,
            specialtyType: ClinicalSpecialtyType.InternalMedicine);

        order.AddItem(Guid.NewGuid(), "US_CARDIAC", "Sieu am tim", 450000m, 1);

        Assert.Equal(OrderPaymentStatus.PendingPayment, order.PaymentStatus);
        Assert.False(order.IsReadyForExecution());

        // Cố bắt đầu thực hiện khi chưa thanh toán -> ném lỗi
        Assert.Throws<InvalidOperationException>(() => order.StartExecution());

        // 2. Sau khi thanh toán
        var receiptId = Guid.NewGuid();
        order.MarkPaid(receiptId);

        Assert.Equal(OrderPaymentStatus.Paid, order.PaymentStatus);
        Assert.True(order.IsReadyForExecution());
        Assert.Equal(OrderPaymentStatus.Paid, order.Items[0].PaymentStatus);
        Assert.Equal(receiptId, order.Items[0].ReceiptId);

        // Giờ bắt đầu thực hiện thành công
        order.StartExecution();
        Assert.Equal(MedicalOrderStatus.InProgress, order.Status);
    }

    [Fact]
    public void MedicalOrder_InPackage_Should_Be_Ready_Immediately()
    {
        var encounterId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();

        var order = new MedicalOrder(
            encounterId,
            "ENC-2026-001",
            patientId,
            "Le Thi C",
            doctorId,
            "BS. Tri",
            MedicalOrderType.Laboratory,
            isInPackage: true,
            specialtyType: ClinicalSpecialtyType.InternalMedicine);

        order.AddItem(Guid.NewGuid(), "CBC", "Tong phan tich mau", 110000m, 1);

        Assert.Equal(OrderPaymentStatus.PackageCovered, order.PaymentStatus);
        Assert.True(order.IsReadyForExecution());

        order.StartExecution();
        Assert.Equal(MedicalOrderStatus.InProgress, order.Status);
    }

    [Fact]
    public void Prescription_DispenseGate_Should_Enforce_IsPaid_And_IsSigned()
    {
        var encounterId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();

        var prescription = new PrescriptionEntity(
            encounterId,
            "ENC-2026-001",
            patientId,
            "Pham Van D",
            doctorId,
            "BS. Huong",
            "001234/HCM-CCHN",
            "K29.7",
            "Viem da day");

        prescription.SpecialtyType = ClinicalSpecialtyType.InternalMedicine;
        prescription.AddItem(Guid.NewGuid(), "Esomeprazole 40mg", "VN-18234-14", 28, "Vien", "Uong", 14);

        // 1. Chưa ký số -> CanDispense = false
        Assert.False(prescription.CanDispense());

        // 2. Ký số nhưng chưa thanh toán (IsPaid = false) -> CanDispense = false
        prescription.ApplySignature("SIG-HASH-123");
        Assert.True(prescription.IsSigned);
        Assert.False(prescription.IsPaid);
        Assert.False(prescription.CanDispense());

        // 3. Nếu bệnh nhân tự mua ngoài -> CanDispense = false (kho không xuất)
        prescription.MarkExternalPurchase();
        Assert.True(prescription.IsExternalPurchase);
        Assert.False(prescription.CanDispense());

        // 4. Nếu mua tại phòng khám và đã thu tiền -> CanDispense = true
        var clinicRx = new PrescriptionEntity(
            encounterId,
            "ENC-2026-001",
            patientId,
            "Pham Van D",
            doctorId,
            "BS. Huong",
            "001234/HCM-CCHN",
            "K29.7",
            "Viem da day");
        clinicRx.AddItem(Guid.NewGuid(), "Esomeprazole 40mg", "VN-18234-14", 28, "Vien", "Uong", 14);
        clinicRx.ApplySignature("SIG-HASH-456");

        var receiptId = Guid.NewGuid();
        clinicRx.MarkPaid(receiptId);

        Assert.True(clinicRx.IsPaid);
        Assert.Equal(receiptId, clinicRx.ReceiptId);
        Assert.True(clinicRx.CanDispense());
    }

    [Fact]
    public void DomainExceptions_Should_Instantiate_With_Correct_Metadata()
    {
        var serviceId = Guid.NewGuid();
        var exService = new UnpaidServiceExecutionBlockedException(
            serviceId,
            "XR_CHEST",
            "Dich vu X-quang chua thanh toan.");

        Assert.Equal(serviceId, exService.ServiceId);
        Assert.Equal("XR_CHEST", exService.ServiceCode);

        var rxId = Guid.NewGuid();
        var exRx = new UnpaidPrescriptionDispenseBlockedException(
            rxId,
            "Don thuoc chua thanh toan tien vien phi.");

        Assert.Equal(rxId, exRx.PrescriptionId);
    }
}
