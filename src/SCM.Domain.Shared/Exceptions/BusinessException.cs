namespace SCM.Domain.Shared.Exceptions;

public class BusinessException : Exception
{
    public string Code { get; }
    public string? Details { get; }

    public BusinessException(string code, string message, string? details = null)
        : base(message)
    {
        Code = code;
        Details = details;
    }
}
