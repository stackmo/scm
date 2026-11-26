using System;

namespace SCM.Domain.Billing.Exceptions;

/// <summary>
/// Ném ra khi phòng Cận lâm sàng (CĐHA/Xét nghiệm) cố gắng tiếp nhận hoặc thực hiện dịch vụ phát sinh ngoài gói
/// nhưng dịch vụ đó chưa được thanh toán (INV-GATE-01).
/// </summary>
public class UnpaidServiceExecutionBlockedException : InvalidOperationException
{
    public Guid ServiceId { get; }
    public string ServiceCode { get; }

    public UnpaidServiceExecutionBlockedException(Guid serviceId, string serviceCode, string message)
        : base(message)
    {
        ServiceId = serviceId;
        ServiceCode = serviceCode;
    }
}
