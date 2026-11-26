using SCM.Domain.Shared;
using SCM.Domain.Shared.Exceptions;

namespace SCM.Domain.Prescription.Exceptions;

public class DoctorLicenseInvalidException : BusinessException
{
    public Guid DoctorId { get; }

    public DoctorLicenseInvalidException(Guid doctorId, string message)
        : base(SCMErrorCodes.DoctorLicenseInvalid, message)
    {
        DoctorId = doctorId;
    }
}
