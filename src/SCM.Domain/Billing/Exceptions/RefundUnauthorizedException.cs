using SCM.Domain.Shared;
using SCM.Domain.Shared.Exceptions;

namespace SCM.Domain.Billing.Exceptions;

public class RefundUnauthorizedException : BusinessException
{
    public Guid UserId { get; }

    public RefundUnauthorizedException(Guid userId, string message)
        : base(SCMErrorCodes.RefundUnauthorized, message)
    {
        UserId = userId;
    }
}
