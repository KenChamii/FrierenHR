using FrierenHR.Application.Common.DTOs;

namespace FrierenHR.Application.Features.Company;

public interface ICompanyService
{
    Task<List<CompanyDto>> GetAllAsync(CancellationToken ct = default);
    Task<CompanyDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CompanyDto> CreateAsync(CreateCompanyDto dto, CancellationToken ct = default);
    Task<List<DepartmentDto>> GetDepartmentsAsync(Guid companyId, CancellationToken ct = default);
    Task<DepartmentDto> CreateDepartmentAsync(Guid companyId, CreateDepartmentDto dto, CancellationToken ct = default);
}