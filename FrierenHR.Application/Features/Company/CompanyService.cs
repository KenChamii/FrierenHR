using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Core.Entities;

namespace FrierenHR.Application.Features.Company;

public class CompanyService : ICompanyService
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IDepartmentRepository _departmentRepository;
    public CompanyService(ICompanyRepository companyRepository, IDepartmentRepository departmentRepository)
    { _companyRepository = companyRepository; _departmentRepository = departmentRepository; }

    public async Task<CompanyDto> CreateAsync(CreateCompanyDto dto, CancellationToken ct = default)
    {
        var existing = await _companyRepository.GetByCodeAsync(dto.Code, ct);
        if (existing is not null) throw new InvalidOperationException($"Company code '{dto.Code}' is already in use.");

        var entity = new FrierenHR.Core.Entities.Company { Name = dto.Name, Code = dto.Code };
        await _companyRepository.AddAsync(entity, ct);
        await _companyRepository.SaveChangesAsync(ct);
        return new CompanyDto(entity.Id, entity.Name, entity.Code, entity.IsActive, 0);
    }

    public async Task<List<CompanyDto>> GetAllAsync(CancellationToken ct = default)
    {
        var companies = await _companyRepository.GetAllAsync(ct);
        return companies.Select(c => new CompanyDto(c.Id, c.Name, c.Code, c.IsActive, c.Employees.Count)).ToList();
    }

    public async Task<CompanyDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var company = await _companyRepository.GetByIdAsync(id, ct);
        return company is null ? null : new CompanyDto(company.Id, company.Name, company.Code, company.IsActive, company.Employees.Count);
    }

    public async Task<List<DepartmentDto>> GetDepartmentsAsync(Guid companyId, CancellationToken ct = default)
    {
        var departments = await _departmentRepository.GetByCompanyAsync(companyId, ct);
        return departments.Select(d => new DepartmentDto(d.Id, d.CompanyId, d.Name, d.Employees.Count)).ToList();
    }

    public async Task<DepartmentDto> CreateDepartmentAsync(Guid companyId, CreateDepartmentDto dto, CancellationToken ct = default)
    {
        var entity = new FrierenHR.Core.Entities.Department { CompanyId = companyId, Name = dto.Name };
        await _departmentRepository.AddAsync(entity, ct);
        await _departmentRepository.SaveChangesAsync(ct);
        return new DepartmentDto(entity.Id, entity.CompanyId, entity.Name, 0);
    }
}