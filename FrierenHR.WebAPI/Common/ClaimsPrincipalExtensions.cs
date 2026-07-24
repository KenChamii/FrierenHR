using System.Security.Claims;

namespace FrierenHR.WebAPI.Common;

/// <summary>
/// Reads the authenticated caller's identity straight off their JWT claims
/// (see TokenService.GenerateToken) instead of trusting whatever employeeId/
/// companyId the client puts in the request body or query string.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static Guid? GetEmployeeId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(value, out var id) ? id : null;
    }

    public static Guid? GetCompanyId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("companyId");
        return Guid.TryParse(value, out var id) ? id : null;
    }

    public static string? GetRole(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.Role);

    /// <summary>True if the caller IS the given employee, or holds one of the allowed roles (e.g. Manager/HRAdmin viewing someone else's record).</summary>
    public static bool IsSelfOrRole(this ClaimsPrincipal user, Guid employeeId, params string[] allowedRoles)
    {
        if (user.GetEmployeeId() == employeeId) return true;
        var role = user.GetRole();
        return role is not null && allowedRoles.Contains(role);
    }
}
