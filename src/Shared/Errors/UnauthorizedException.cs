namespace Shared.Errors;

public sealed class UnauthorizedException : DomainException { public UnauthorizedException(string m) : base(m, 401) {} }
