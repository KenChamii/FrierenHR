namespace FrierenHR.Application.Common.Security;

/// <summary>
/// Minimum password requirements, enforced server-side so this can't be bypassed by hitting
/// the API directly (the frontend mirrors this for immediate feedback, but that's UX only —
/// this is the actual enforcement point). Deliberately not going overboard with special-char/
/// rotation rules; current NIST guidance favors length + a mix of character types over complex
/// composition rules that just push people toward "Password1!" and rotation calendars.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;

    public static void Validate(string? password)
    {
        var errors = GetErrors(password);
        if (errors.Count > 0) throw new InvalidOperationException(string.Join(" ", errors));
    }

    public static List<string> GetErrors(string? password)
    {
        var errors = new List<string>();
        if (string.IsNullOrEmpty(password) || password.Length < MinLength)
            errors.Add($"Password must be at least {MinLength} characters long.");
        if (string.IsNullOrEmpty(password) || !password.Any(char.IsLetter))
            errors.Add("Password must contain at least one letter.");
        if (string.IsNullOrEmpty(password) || !password.Any(char.IsDigit))
            errors.Add("Password must contain at least one number.");
        return errors;
    }
}
