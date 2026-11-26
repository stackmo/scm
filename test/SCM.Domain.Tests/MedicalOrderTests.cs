using SCM.Domain.Clinical;
using SCM.Domain.Shared.Enums;
using Xunit;

namespace SCM.Domain.Tests;

public class MedicalOrderTests
{
    [Fact]
    public void CreateMedicalOrder_InPackage_DefaultsToPackageCoveredAndAllowsExecution()
    {
        // Given: Chỉ định dịch vụ trong gói hợp đồng B2B
        var order = new MedicalOrder(
            encounterId: Guid.NewGuid(),
            encounterCode: "ENC-B2B-01",
            patientId: Guid.NewGuid(),
            patientName: "Trần Thị Lan",
            doctorId: Guid.NewGuid(),
            doctorName: "BS. Lê Hoàng",
            orderType: MedicalOrderType.DiagnosticImaging,
            isInPackage: true,
            packageId: Guid.NewGuid(),
            packageName: "Gói Khám Tiêu Chuẩn Doanh Nghiệp");

        order.AddItem(Guid.NewGuid(), "US_ABDOMEN", "Siêu âm ổ bụng tổng quát", 250000m);

        // Then
        Assert.True(order.IsInPackage);
        Assert.Equal(OrderPaymentStatus.PackageCovered, order.PaymentStatus);
        Assert.Equal(250000m, order.TotalAmount);
        Assert.Equal(MedicalOrderStatus.Ordered, order.Status);

        // When: Phòng CLS tiếp nhận thực hiện
        order.StartExecution();

        // Then
        Assert.Equal(MedicalOrderStatus.InProgress, order.Status);

        // When: Hoàn tất
        order.CompleteOrder();
        Assert.Equal(MedicalOrderStatus.Completed, order.Status);
        Assert.NotNull(order.CompletedAt);
    }

    [Fact]
    public void CreateMedicalOrder_OutOfPackage_PendingPayment_BlocksExecutionUntilPaid()
    {
        // Given: Chỉ định phát sinh ngoài gói khám
        var order = new MedicalOrder(
            encounterId: Guid.NewGuid(),
            encounterCode: "ENC-B2B-02",
            patientId: Guid.NewGuid(),
            patientName: "Phạm Văn Minh",
            doctorId: Guid.NewGuid(),
            doctorName: "BS. Lê Hoàng",
            orderType: MedicalOrderType.DiagnosticImaging,
            isInPackage: false,
            diagnosisIcd10: "K29.7",
            clinicalNotes: "Chỉ định phát sinh theo yêu cầu bác sĩ");

        order.AddItem(Guid.NewGuid(), "ENDO_GASTRO", "Nội soi dạ dày không gây mê", 800000m);

        // Then
        Assert.False(order.IsInPackage);
        Assert.Equal(OrderPaymentStatus.PendingPayment, order.PaymentStatus);
        Assert.Equal(800000m, order.TotalAmount);

        // When: Phòng nội soi cố tình tiếp nhận khi chưa thu tiền
        // Then: Bị chặn bởi Invariant INV-DESK-02
        var ex = Assert.Throws<InvalidOperationException>(() => order.StartExecution());
        Assert.Contains("chưa thanh toán (PendingPayment)", ex.Message);

        // When: Thu ngân thu tiền thành công
        order.MarkPaid();
        Assert.Equal(OrderPaymentStatus.Paid, order.PaymentStatus);

        // When: Phòng nội soi tiếp nhận sau khi thu tiền
        order.StartExecution();
        Assert.Equal(MedicalOrderStatus.InProgress, order.Status);
    }

    [Fact]
    public void MedicalOrder_CannotCancelWhenAlreadyCompleted()
    {
        // Given: Đơn chỉ định đã hoàn tất
        var order = new MedicalOrder(
            encounterId: Guid.NewGuid(),
            encounterCode: "ENC-03",
            patientId: Guid.NewGuid(),
            patientName: "Lê Văn C",
            doctorId: Guid.NewGuid(),
            doctorName: "BS. Hoàng",
            orderType: MedicalOrderType.Laboratory,
            isInPackage: true);

        order.AddItem(Guid.NewGuid(), "CBC", "Tổng phân tích tế bào máu", 100000m);
        order.StartExecution();
        order.CompleteOrder();

        // When & Then: Cố gắng hủy
        Assert.Throws<InvalidOperationException>(() => order.CancelOrder("Bệnh nhân đổi ý"));
    }
}
