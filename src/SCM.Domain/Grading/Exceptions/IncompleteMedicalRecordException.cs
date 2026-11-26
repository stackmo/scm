using SCM.Domain.Shared;
using SCM.Domain.Shared.Exceptions;

namespace SCM.Domain.Grading.Exceptions;

public class IncompleteMedicalRecordException : BusinessException
{
    public List<string> PendingItems { get; }

    public IncompleteMedicalRecordException(List<string> pendingItems, string message)
        : base(SCMErrorCodes.IncompleteMedicalRecord, message)
    {
        PendingItems = pendingItems ?? new List<string>();
    }
}
