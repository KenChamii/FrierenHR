using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Features.Audit;
using FrierenHR.WebAPI.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrierenHR.WebAPI.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize(Roles = "HRAdmin")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _audit;
    public AuditController(IAuditService audit) => _audit = audit;

    // GET /api/audit?entityName=CompanyRuleConfig&entityId=...&skip=0&take=50 Scoped to the caller's company by the service.
    [HttpGet]
    public async Task<ActionResult<AuditPageDto>> Get(
        [FromQuery] string? entityName, [FromQuery] Guid? entityId,
        [FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Forbid();
        return Ok(await _audit.GetPageAsync(companyId.Value, entityName, entityId, skip, take, ct));
    }
}
