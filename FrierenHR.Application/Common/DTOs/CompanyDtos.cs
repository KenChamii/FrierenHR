namespace FrierenHR.Application.Common.DTOs;

public record CompanyDto(Guid Id, string Name, string Code, bool IsActive, int EmployeeCount);
public record CreateCompanyDto(string Name, string Code);
public record DepartmentDto(Guid Id, Guid CompanyId, string Name, int EmployeeCount);
public record CreateDepartmentDto(string Name);