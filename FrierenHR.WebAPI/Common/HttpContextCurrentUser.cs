using FrierenHR.Application.Common.Interfaces;

namespace FrierenHR.WebAPI.Common;

/// <summary>Supplies the acting user to layers that must not know about HTTP, in particular the audit interceptor in Infrastructure.</summary>
public sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;
    public HttpContextCurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    public Guid? EmployeeId => _accessor.HttpContext?.User.GetEmployeeId();
    public Guid? CompanyId => _accessor.HttpContext?.User.GetCompanyId();
}
