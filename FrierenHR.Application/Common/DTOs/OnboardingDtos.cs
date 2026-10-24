using FrierenHR.Core.Enums;

namespace FrierenHR.Application.Common.DTOs;

/// <summary>A company and its first administrator, submitted together.</summary>
public record RegisterCompanyDto(
    string CompanyName,
    string CompanyCode,
    string FirstName,
    string LastName,
    string Email,
    string Password);

/// <summary>What registration produced.</summary>
public record RegisteredCompanyDto(
    Guid CompanyId,
    string CompanyName,
    string CompanyCode,
    Guid EmployeeId,
    string FullName,
    string Email,
    EmployeeRole Role);
