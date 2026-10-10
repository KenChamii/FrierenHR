using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Features.Scheduling;
using FrierenHR.WebAPI.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrierenHR.WebAPI.Controllers;

[ApiController]
[Route("api/shifts")]
[Authorize]
public class ShiftsController : ControllerBase
{
    private readonly IShiftService _shiftService;
    public ShiftsController(IShiftService shiftService) => _shiftService = shiftService;

    [HttpGet]
    public async Task<ActionResult<List<ShiftDto>>> GetMine(CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Forbid();
        return Ok(await _shiftService.GetByCompanyAsync(companyId.Value, ct));
    }

    // GET /api/shifts/for-employee/{id} — which shift applies and where it came from, so the
    // attendance screen can show "08:00, from your department" rather than an unexplained time.
    [HttpGet("for-employee/{employeeId:guid}")]
    public async Task<ActionResult<ResolvedShiftDto>> ForEmployee(Guid employeeId, CancellationToken ct)
    {
        if (!User.IsSelfOrRole(employeeId, "Manager", "HRAdmin")) return Forbid();
        return Ok(await _shiftService.DescribeForEmployeeAsync(employeeId, ct));
    }

    [HttpPost]
    [Authorize(Roles = "HRAdmin")]
    public async Task<ActionResult<ShiftDto>> Save(SaveShiftDto dto, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Forbid();
        if (dto.CompanyId != companyId) dto = dto with { CompanyId = companyId.Value };

        try { return Ok(await _shiftService.SaveAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "HRAdmin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Forbid();

        try { await _shiftService.DeleteAsync(id, companyId.Value, ct); return NoContent(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
