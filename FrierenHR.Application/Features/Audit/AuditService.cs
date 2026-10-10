using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Common.Interfaces;

namespace FrierenHR.Application.Features.Audit;

public class AuditService : IAuditService
{
    private readonly IAuditRepository _audit;
    private readonly IEmployeeRepository _employees;

    public AuditService(IAuditRepository audit, IEmployeeRepository employees)
    {
        _audit = audit;
        _employees = employees;
    }

    public async Task<AuditPageDto> GetPageAsync(
        Guid companyId, string? entityName = null, Guid? entityId = null,
        int skip = 0, int take = 50, CancellationToken ct = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 200);

        var entries = await _audit.GetForCompanyAsync(companyId, entityName, entityId, skip, take, ct);
        var total = await _audit.CountForCompanyAsync(companyId, entityName, entityId, ct);

        // One lookup for the page's actors rather than one per row. The trail deliberately
        // holds no foreign key to Employee, so the name is resolved here and left blank for
        // anyone since removed.
        var employees = await _employees.GetByCompanyAsync(companyId, ct);
        var namesById = employees.ToDictionary(e => e.Id, e => $"{e.FirstName} {e.LastName}");

        var dtos = entries
            .Select(e => new AuditEntryDto(
                e.Id, e.EntityName, e.EntityId, e.Action, e.ChangedByEmployeeId,
                e.ChangedByEmployeeId is not null && namesById.TryGetValue(e.ChangedByEmployeeId.Value, out var name)
                    ? name
                    : e.ChangedByEmployeeId is null ? "System" : null,
                e.ChangedAt, e.Changes))
            .ToList();

        return new AuditPageDto(dtos, total, skip, take);
    }
}
