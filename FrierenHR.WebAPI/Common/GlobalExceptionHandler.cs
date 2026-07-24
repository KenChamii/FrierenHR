using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FrierenHR.WebAPI.Common;

/// <summary>
/// Catches anything that escapes a controller action unhandled (DB connection errors, null
/// refs, etc.) and turns it into a consistent ProblemDetails JSON response instead of the
/// framework's raw developer exception page / stack trace leaking to the client.
///
/// Expected business-rule errors (InvalidOperationException from the Application layer) are
/// still caught locally in each controller's try/catch and turned into 400s with a friendly
/// message — this handler is the safety net for everything that ISN'T expected.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        _logger.LogError(exception, "Unhandled exception on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            // Full exception details only in Development — never leak stack traces / connection
            // strings / query text to a client in Production.
            Detail = _env.IsDevelopment() ? exception.ToString() : "Please try again, or contact support if the problem persists.",
            Instance = httpContext.Request.Path,
        };

        httpContext.Response.StatusCode = problem.Status.Value;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}
