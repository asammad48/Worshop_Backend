namespace Shared.Errors;

public sealed class ForbiddenException : DomainException { public ForbiddenException(string m) : base(m, 403) {} }
