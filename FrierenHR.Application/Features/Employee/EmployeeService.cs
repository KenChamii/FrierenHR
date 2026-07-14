using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Application.Common.Security;


namespace FrierenHR.Application.Features.Employee;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    public EmployeeService(IEmployeeRepository employeeRepository) => _employeeRepository = employeeRepository;

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto, CancellationToken ct = default)
    {
        var existing = await _employeeRepository.GetByEmailAsync(dto.Email, ct);
        if (existing is not null) throw new InvalidOperationException($"Email '{dto.Email}' is already registered.");

        var entity = new FrierenHR.Core.Entities.Employee
        {
            CompanyId = dto.CompanyId,
            DepartmentId = dto.DepartmentId,
            ManagerId = dto.ManagerId,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PasswordHash = PasswordHasher.Hash(dto.Password),
            HireDate = dto.HireDate,
            Role = dto.Role
        };
        await _employeeRepository.AddAsync(entity, ct);
        await _employeeRepository.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    public async Task<List<EmployeeDto>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default)
    {
        var employees = await _employeeRepository.GetByCompanyAsync(companyId, ct);
        return employees.Select(ToDto).ToList();
    }

    public async Task<EmployeeDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(id, ct);
        return employee is null ? null : ToDto(employee);
    }

    public async Task<List<EmployeeDto>> GetDirectReportsAsync(Guid managerId, CancellationToken ct = default)
    {
        var reports = await _employeeRepository.GetDirectReportsAsync(managerId, ct);
        return reports.Select(ToDto).ToList();
    }

    public async Task<EmployeeDto> UpdateAsync(Guid id, UpdateEmployeeDto dto, CancellationToken ct = default)
    {
        var entity = await _employeeRepository.GetByIdAsync(id, ct)
            ?? throw new InvalidOperationException($"Employee '{id}' not found.");

        entity.DepartmentId = dto.DepartmentId;
        entity.ManagerId = dto.ManagerId;
        entity.FirstName = dto.FirstName;
        entity.LastName = dto.LastName;
        entity.Role = dto.Role;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        _employeeRepository.Update(entity);
        await _employeeRepository.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    private static EmployeeDto ToDto(FrierenHR.Core.Entities.Employee e) => new(e.Id, e.CompanyId, e.DepartmentId, e.Department?.Name,
        e.ManagerId, e.Manager is null ? null : $"{e.Manager.FirstName} {e.Manager.LastName}",
        e.FirstName, e.LastName, e.Email, e.HireDate, e.TenureMonths(), e.Role, e.IsActive);
}