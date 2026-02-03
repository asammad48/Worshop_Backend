namespace Shared.Errors;

public sealed class ValidationException : DomainException { public ValidationException(string m, IReadOnlyList<string> e) : base(m, 400, e) {} }
