using SCM.Domain.Shared;
using SCM.Domain.Shared.Exceptions;

namespace SCM.Domain.Billing.Exceptions;

public class XmlBalanceMismatchException : BusinessException
{
    public decimal TongChi { get; }
    public decimal SumChiTiet { get; }

    public XmlBalanceMismatchException(decimal tongChi, decimal sumChiTiet, string message)
        : base(SCMErrorCodes.XmlBalanceMismatch, message)
    {
        TongChi = tongChi;
        SumChiTiet = sumChiTiet;
    }
}
