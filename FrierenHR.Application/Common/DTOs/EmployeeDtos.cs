using FrierenHR.Core.Enums;

namespace FrierenHR.Application.Common.DTOs;

public record EmployeeDto(Guid Id, Guid CompanyId, Guid? DepartmentId, string? DepartmentName,
    Guid? ManagerId, string? ManagerName, string FirstName, string LastName, string Email,
    DateTime HireDate, int TenureMonths, EmployeeRole Role, bool IsActive);
public record CreateEmployeeDto(Guid CompanyId, Guid? DepartmentId, Guid? ManagerId,
    string FirstName, string LastName, string Email, string Password, DateTime HireDate, EmployeeRole Role);
public record UpdateEmployeeDto(Guid? DepartmentId, Guid? ManagerId, string FirstName, string LastName, EmployeeRole Role, bool IsActive);