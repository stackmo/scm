using SCM.Domain.Shared;
using SCM.Domain.Shared.Exceptions;

namespace SCM.Domain.Prescription.Exceptions;

public class UnmappedNationalDrugException : BusinessException
{
    public string DrugName { get; }

    public UnmappedNationalDrugException(string drugName, string message)
        : base(SCMErrorCodes.UnmappedNationalDrug, message)
    {
        DrugName = drugName;
    }
}
