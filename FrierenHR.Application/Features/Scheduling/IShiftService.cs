using FrierenHR.Application.Common.DTOs;
using FrierenHR.Core.Entities;

namespace FrierenHR.Application.Features.Scheduling;

public interface IShiftService
{
    Task<List<ShiftDto>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default);
    Task<ShiftDto> SaveAsync(SaveShiftDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid companyId, CancellationToken ct = default);

    /// <summary>The shift that applies to an employee: their own, else their department's, else the company default.</summary>
    Task<Shift?> ResolveForEmployeeAsync(Guid employeeId, CancellationToken ct = default);

    /// <summary>The same resolution, with the level that supplied it, for display.</summary>
    Task<ResolvedShiftDto> DescribeForEmployeeAsync(Guid employeeId, CancellationToken ct = default);
}
