using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Features.Scheduling;
using FrierenHR.WebAPI.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrierenHR.WebAPI.Controllers;

[ApiController]
[Route("api/holidays")]
[Authorize]
public class HolidaysController : ControllerBase
{
    private readonly IHolidayService _holidayService;
    public HolidaysController(IHolidayService holidayService) => _holidayService = holidayService;

    // GET /api/holidays?year=2026 — the company's calendar for a year. Readable by everyone,
    // because an employee planning leave needs to know which days are closed.
    [HttpGet]
    public async Task<ActionResult<List<HolidayDto>>> GetForYear([FromQuery] int? year, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Forbid();
        return Ok(await _holidayService.GetForYearAsync(companyId.Value, year ?? DateTime.UtcNow.Year, ct));
    }

    // GET /api/holidays/working-days?from=&to= — how many days a leave request would actually
    // deduct, with the skipped days listed. Used by the request form before submitting.
    [HttpGet("working-days")]
    public async Task<ActionResult<WorkingDaysDto>> CountWorkingDays(
        [FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] Guid? employeeId, CancellationToken ct)
    {
        var caller = User.GetEmployeeId();
        if (caller is null) return Forbid();

        // Someone may only ask about their own calendar unless they manage people.
        var subject = employeeId ?? caller.Value;
        if (!User.IsSelfOrRole(subject, "Manager", "HRAdmin")) return Forbid();

        try { return Ok(await _holidayService.CountWorkingDaysAsync(subject, from, to, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost]
    [Authorize(Roles = "HRAdmin")]
    public async Task<ActionResult<HolidayDto>> Save(SaveHolidayDto dto, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Forbid();
        // The caller's own company wins over whatever the body says, as elsewhere.
        if (dto.CompanyId != companyId) dto = dto with { CompanyId = companyId.Value };

        try { return Ok(await _holidayService.SaveAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    // POST /api/holidays/import — a year's calendar in one request. Duplicates are skipped
    // rather than failing the batch, and the result says what happened to each.
    [HttpPost("import")]
    [Authorize(Roles = "HRAdmin")]
    public async Task<ActionResult<HolidayImportResultDto>> Import(List<SaveHolidayDto> holidays, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Forbid();

        try { return Ok(await _holidayService.ImportAsync(companyId.Value, holidays, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "HRAdmin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Forbid();

        try { await _holidayService.DeleteAsync(id, companyId.Value, ct); return NoContent(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
