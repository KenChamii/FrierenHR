using FrierenHR.Application.Common.DTOs;

namespace FrierenHR.Application.Features.Onboarding;

public interface IRegistrationService
{
    /// <summary>
    /// Creates a company, its first HRAdmin and a starting configuration, all in one
    /// transaction. Throws <see cref="InvalidOperationException"/> if the company code or the
    /// email is taken, or if the password fails the policy.
    /// </summary>
    Task<RegisteredCompanyDto> RegisterCompanyAsync(
        RegisterCompanyDto dto, CancellationToken ct = default);
}
