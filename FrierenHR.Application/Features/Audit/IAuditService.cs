using FrierenHR.Application.Common.DTOs;

namespace FrierenHR.Application.Features.Audit;

public interface IAuditService
{
    /// <summary>A page of the company's trail, most recent first.</summary>
    Task<AuditPageDto> GetPageAsync(
        Guid companyId, string? entityName = null, Guid? entityId = null,
        int skip = 0, int take = 50, CancellationToken ct = default);
}
