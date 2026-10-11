namespace FrierenHR.WebAPI.Common;

/// <summary>
/// Confines an account that is still on a password somebody else chose to one action:
/// changing it.
///
/// Enforced here rather than only in the client, because a client-side redirect is a
/// suggestion. The window in which an administrator knows a working password should be as
/// short as the person's first sign-in.
/// </summary>
public class PasswordChangeRequiredMiddleware
{
    /// <summary>
    /// What such an account may still reach: signing in, signing up, reading its own record
    /// so the app can render, and the change-password endpoint itself.
    /// </summary>
    private static readonly string[] AlwaysAllowed =
    [
        "/api/auth",
        "/health",
        "/swagger",
    ];

    private const string ChangePasswordSegment = "/change-password";

    private readonly RequestDelegate _next;

    public PasswordChangeRequiredMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!MustChangePassword(context))
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;

        if (IsAllowed(path))
        {
            await _next(context);
            return;
        }

        // A distinct code rather than a bare 403, so the client can route to the change
        // screen instead of showing a generic "not allowed".
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new
        {
            code = "password_change_required",
            message = "Set your own password before using the rest of the application.",
        });
    }

    private static bool MustChangePassword(HttpContext context) =>
        context.User.Identity?.IsAuthenticated == true
        && string.Equals(context.User.FindFirst("mustChangePassword")?.Value, "true",
            StringComparison.OrdinalIgnoreCase);

    private static bool IsAllowed(string path)
    {
        if (AlwaysAllowed.Any(allowed => path.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)))
            return true;

        // The change-password endpoint lives under the employee route, so it is matched by
        // its last segment rather than by a prefix.
        return path.EndsWith(ChangePasswordSegment, StringComparison.OrdinalIgnoreCase);
    }
}
