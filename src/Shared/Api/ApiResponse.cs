namespace Shared.Api;

public sealed record ApiResponse<T>(bool Success, string Message, T? Data = default, IReadOnlyList<string>? Errors = null)
{
    public static ApiResponse<T> Ok(T data, string message = "OK") => new(true, message, data, null);
    public static ApiResponse<T> Ok(string message = "OK") => new(true, message, default, null);
    public static ApiResponse<T> Fail(string message, params string[] errors) => new(false, message, default, errors);
}
