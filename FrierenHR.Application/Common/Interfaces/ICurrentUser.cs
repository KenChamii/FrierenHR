namespace FrierenHR.Application.Common.Interfaces;

/// <summary>
/// Who is making the current request. Declared here and implemented in the web layer, so the
/// audit interceptor in Infrastructure can record an actor without depending on HTTP.
///
/// Every property is nullable: a background job such as the nightly accrual run has no user.
/// </summary>
public interface ICurrentUser
{
    Guid? EmployeeId { get; }
    Guid? CompanyId { get; }
}
