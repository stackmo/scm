using System;

namespace SCM.Domain.Pharmacy.Exceptions;

/// <summary>
/// Ném ra khi Dược sĩ tại quầy thuốc cố gắng xuất kho cấp phát thuốc cho đơn thuốc chưa thanh toán viện phí (INV-GATE-02).
/// </summary>
public class UnpaidPrescriptionDispenseBlockedException : InvalidOperationException
{
    public Guid PrescriptionId { get; }

    public UnpaidPrescriptionDispenseBlockedException(Guid prescriptionId, string message)
        : base(message)
    {
        PrescriptionId = prescriptionId;
    }
}
