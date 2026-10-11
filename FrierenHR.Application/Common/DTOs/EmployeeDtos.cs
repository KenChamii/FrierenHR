using FrierenHR.Core.Enums;

namespace FrierenHR.Application.Common.DTOs;

/// <param name="MonthlyRate">
/// Null when no pay has been recorded. Payslip generation skips such an employee by name
/// rather than pricing them at zero.
/// </param>
public record EmployeeDto(Guid Id, Guid CompanyId, Guid? DepartmentId, string? DepartmentName,
    Guid? ManagerId, string? ManagerName, string FirstName, string LastName, string Email,
    DateTime HireDate, int TenureMonths, EmployeeRole Role, bool IsActive,
    decimal? MonthlyRate = null, decimal StandardMonthlyHours = 174m);

public record CreateEmployeeDto(Guid CompanyId, Guid? DepartmentId, Guid? ManagerId,
    string FirstName, string LastName, string Email, string Password, DateTime HireDate, EmployeeRole Role,
    decimal? MonthlyRate = null, decimal StandardMonthlyHours = 174m);

public record UpdateEmployeeDto(Guid? DepartmentId, Guid? ManagerId, string FirstName, string LastName,
    EmployeeRole Role, bool IsActive,
    decimal? MonthlyRate = null, decimal StandardMonthlyHours = 174m);

public record ChangePasswordDto(string CurrentPassword, string NewPassword);
