using FrierenHR.Core.Enums;

namespace FrierenHR.Application.Common.DTOs;

/// <summary>
/// A company and its first administrator, submitted together. There is no separate step for
/// the administrator: a company created without one can never be logged into.
/// </summary>
public record RegisterCompanyDto(
    string CompanyName,
    string CompanyCode,
    string FirstName,
    string LastName,
    string Email,
    string Password);

/// <summary>
/// What registration produced. The token is minted by the API from this, so the person lands
/// in the application rather than at a login form they have just set a password for.
/// </summary>
public record RegisteredCompanyDto(
    Guid CompanyId,
    string CompanyName,
    string CompanyCode,
    Guid EmployeeId,
    string FullName,
    string Email,
    EmployeeRole Role);
