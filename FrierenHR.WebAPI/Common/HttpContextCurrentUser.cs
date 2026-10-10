using FrierenHR.Application.Common.Interfaces;

namespace FrierenHR.WebAPI.Common;

/// <summary>
/// Supplies the acting user to layers that must not know about HTTP, in particular the audit
/// interceptor in Infrastructure.
///
/// Both properties are null outside a request, which is how a background job such as the
/// nightly accrual run is recorded as "System" rather than attributed to whoever happened to
/// start the process.
/// </summary>
public sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;
    public HttpContextCurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    public Guid? EmployeeId => _accessor.HttpContext?.User.GetEmployeeId();
    public Guid? CompanyId => _accessor.HttpContext?.User.GetCompanyId();
}
