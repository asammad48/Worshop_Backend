namespace Shared.Errors;

public class DomainException : Exception
{
    public int StatusCode { get; }
    public IReadOnlyList<string> Errors { get; }

    public DomainException(string message, int statusCode = 409, IReadOnlyList<string>? errors = null) : base(message)
    {
        StatusCode = statusCode;
        Errors = errors ?? new[] { message };
    }
}
