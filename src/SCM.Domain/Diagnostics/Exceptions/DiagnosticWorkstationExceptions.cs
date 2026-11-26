using SCM.Domain.Shared;
using SCM.Domain.Shared.Exceptions;

namespace SCM.Domain.Diagnostics.Exceptions;

public class LabReviewSequenceViolationException : BusinessException
{
    public LabReviewSequenceViolationException(string message, string? details = null)
        : base(SCMErrorCodes.LabReviewSequenceViolation, message, details)
    {
    }
}

public class PanicValueUnacknowledgedException : BusinessException
{
    public PanicValueUnacknowledgedException(string message, string? details = null)
        : base(SCMErrorCodes.PanicValueUnacknowledged, message, details)
    {
    }
}

public class LabResultRecordImmutableException : BusinessException
{
    public LabResultRecordImmutableException(string message, string? details = null)
        : base(SCMErrorCodes.LabResultRecordImmutable, message, details)
    {
    }
}

public class ImagingStudyNotCapturedException : BusinessException
{
    public ImagingStudyNotCapturedException(string message, string? details = null)
        : base(SCMErrorCodes.ImagingStudyNotCaptured, message, details)
    {
    }
}

public class UnauthorizedModalitySignException : BusinessException
{
    public UnauthorizedModalitySignException(string message, string? details = null)
        : base(SCMErrorCodes.UnauthorizedModalitySign, message, details)
    {
    }
}

public class ImagingMediaRequiredException : BusinessException
{
    public ImagingMediaRequiredException(string message, string? details = null)
        : base(SCMErrorCodes.ImagingMediaRequired, message, details)
    {
    }
}

public class ImagingRecordImmutableException : BusinessException
{
    public ImagingRecordImmutableException(string message, string? details = null)
        : base(SCMErrorCodes.ImagingRecordImmutable, message, details)
    {
    }
}

public class WorkstationConcurrencyConflictException : BusinessException
{
    public WorkstationConcurrencyConflictException(string message, string? details = null)
        : base(SCMErrorCodes.ConcurrencyConflict, message, details)
    {
    }
}
