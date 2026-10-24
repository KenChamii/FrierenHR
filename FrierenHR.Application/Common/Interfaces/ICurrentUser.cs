namespace FrierenHR.Application.Common.Interfaces;

/// <summary>Who is making the current request.</summary>
public interface ICurrentUser
{
    Guid? EmployeeId { get; }
    Guid? CompanyId { get; }
}
