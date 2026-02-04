using Shared.Errors;

namespace Application.Security;

public static class PasswordValidator
{
    public static void Validate(string password)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            errors.Add("Password must be at least 8 characters long.");

        if (!password.Any(char.IsUpper))
            errors.Add("Password must contain at least one uppercase letter.");

        if (!password.Any(char.IsLower))
            errors.Add("Password must contain at least one lowercase letter.");

        if (!password.Any(char.IsDigit))
            errors.Add("Password must contain at least one digit.");

        if (errors.Any())
            throw new ValidationException("Password policy violation", errors);
    }
}
