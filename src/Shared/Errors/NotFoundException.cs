namespace Shared.Errors;

public sealed class NotFoundException : DomainException { public NotFoundException(string m) : base(m, 404) {} }
