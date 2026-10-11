using FrierenHR.Application.Common.DTOs;

namespace FrierenHR.Application.Features.Employee;

public interface IEmployeeService
{
    Task<List<EmployeeDto>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default);
    Task<EmployeeDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<EmployeeDto>> GetDirectReportsAsync(Guid managerId, CancellationToken ct = default);
    Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto, CancellationToken ct = default);
    Task<EmployeeDto> UpdateAsync(Guid id, UpdateEmployeeDto dto, CancellationToken ct = default);
    Task ChangePasswordAsync(Guid id, ChangePasswordDto dto, CancellationToken ct = default);

    /// <summary>
    /// Issues a new temporary password for somebody who cannot sign in, and returns it once
    /// so HR can pass it on. The account must change it at the next sign-in.
    ///
    /// This is the only route back into a locked-out account: there is no email, so nothing
    /// can be sent to the person directly.
    /// </summary>
    Task<ResetPasswordResultDto> ResetPasswordAsync(
        Guid employeeId, Guid companyId, CancellationToken ct = default);
}