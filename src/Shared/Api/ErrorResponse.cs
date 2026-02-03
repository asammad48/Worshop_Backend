namespace Shared.Api;

public sealed record ErrorResponse(bool Success, string Message, IReadOnlyList<string> Errors, string? TraceId = null);
