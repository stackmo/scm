using SCM.Domain.Shared;
using SCM.Domain.Shared.Exceptions;

namespace SCM.Domain.Grading.Exceptions;

public class HealthGradingViolationException : BusinessException
{
    public string WorstSpecialty { get; }
    public int WorstGrade { get; }
    public int AttemptedGrade { get; }

    public HealthGradingViolationException(string worstSpecialty, int worstGrade, int attemptedGrade, string message)
        : base(SCMErrorCodes.HealthGradingViolation, message)
    {
        WorstSpecialty = worstSpecialty;
        WorstGrade = worstGrade;
        AttemptedGrade = attemptedGrade;
    }
}
