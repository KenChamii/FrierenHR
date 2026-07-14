using FrierenHR.Application.Common.DTOs;

namespace FrierenHR.Application.Features.Employee;

public interface IEmployeeService
{
    Task<List<EmployeeDto>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default);
    Task<EmployeeDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<EmployeeDto>> GetDirectReportsAsync(Guid managerId, CancellationToken ct = default);
    Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto, CancellationToken ct = default);
    Task<EmployeeDto> UpdateAsync(Guid id, UpdateEmployeeDto dto, CancellationToken ct = default);
}