using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Features.Reporting;
using FrierenHR.WebAPI.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrierenHR.WebAPI.Controllers;

/// <summary>
/// Read-only reporting. Every endpoint takes its company from the caller's token rather than
/// from a query parameter, so one tenant cannot ask for another's figures.
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    /// <summary>
    /// Widest window the availability grid will return. A year of employee-days is a slow
    /// query and an unreadable screen.
    /// </summary>
    private const int MaxAvailabilityDays = 92;

    private readonly IAvailabilityService _availability;
    private readonly ILeaveForecastService _forecast;
    private readonly IPayrollExportService _payroll;
    private readonly ILeaveLiabilityService _liability;

    public ReportsController(
        IAvailabilityService availability,
        ILeaveForecastService forecast,
        IPayrollExportService payroll,
        ILeaveLiabilityService liability)
    {
        _availability = availability;
        _forecast = forecast;
        _payroll = payroll;
        _liability = liability;
    }

    // GET /api/reports/availability?from=2026-10-01&to=2026-10-14&departmentId=...
    [HttpGet("availability")]
    [Authorize(Roles = "Manager,HRAdmin")]
    public async Task<ActionResult<AvailabilityDto>> Availability(
        [FromQuery] DateOnly from, [FromQuery] DateOnly to,
        [FromQuery] Guid? departmentId, CancellationToken ct = default)
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Forbid();
        if (to < from) return BadRequest("The end date cannot be before the start date.");
        if (from.AddDays(MaxAvailabilityDays) < to)
            return BadRequest($"Please request at most {MaxAvailabilityDays} days at a time.");

        return Ok(await _availability.GetAsync(companyId.Value, from, to, departmentId, ct));
    }

    // GET /api/reports/leave-forecast?employeeId=...&horizon=2027-12-31
    // An employee may forecast their own balance; only HR may forecast somebody else's.
    [HttpGet("leave-forecast")]
    public async Task<ActionResult<LeaveForecastDto>> LeaveForecast(
        [FromQuery] Guid? employeeId, [FromQuery] DateOnly? horizon, CancellationToken ct = default)
    {
        var callerId = User.GetEmployeeId();
        if (callerId is null) return Forbid();

        var subjectId = employeeId ?? callerId.Value;
        if (!User.IsSelfOrRole(subjectId, "Manager", "HRAdmin")) return Forbid();

        return Ok(await _forecast.ForEmployeeAsync(subjectId, horizon, ct));
    }

    // GET /api/reports/payroll-period?from=2026-10-01&to=2026-10-15
    [HttpGet("payroll-period")]
    [Authorize(Roles = "HRAdmin")]
    public async Task<ActionResult<PayrollPeriodDto>> PayrollPeriod(
        [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct = default)
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Forbid();
        if (to < from) return BadRequest("The end date cannot be before the start date.");

        return Ok(await _payroll.BuildAsync(companyId.Value, from, to, ct));
    }

    // GET /api/reports/payroll-period.csv?from=2026-10-01&to=2026-10-15
    [HttpGet("payroll-period.csv")]
    [Authorize(Roles = "HRAdmin")]
    public async Task<IActionResult> PayrollPeriodCsv(
        [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct = default)
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Forbid();
        if (to < from) return BadRequest("The end date cannot be before the start date.");

        var bytes = await _payroll.BuildCsvAsync(companyId.Value, from, to, ct);
        return File(bytes, "text/csv", $"payroll-{from:yyyy-MM-dd}-to-{to:yyyy-MM-dd}.csv");
    }

    // GET /api/reports/leave-liability?dailyRate=1500
    [HttpGet("leave-liability")]
    [Authorize(Roles = "HRAdmin")]
    public async Task<ActionResult<LiabilityDto>> LeaveLiability(
        [FromQuery] decimal dailyRate, CancellationToken ct = default)
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Forbid();
        if (dailyRate < 0) return BadRequest("The daily rate cannot be negative.");

        return Ok(await _liability.GetAsync(companyId.Value, dailyRate, ct));
    }

    // GET /api/reports/leave-liability.csv?dailyRate=1500
    [HttpGet("leave-liability.csv")]
    [Authorize(Roles = "HRAdmin")]
    public async Task<IActionResult> LeaveLiabilityCsv(
        [FromQuery] decimal dailyRate, CancellationToken ct = default)
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Forbid();
        if (dailyRate < 0) return BadRequest("The daily rate cannot be negative.");

        var bytes = await _liability.GetCsvAsync(companyId.Value, dailyRate, ct);
        return File(bytes, "text/csv", $"leave-liability-{DateTime.UtcNow:yyyy-MM-dd}.csv");
    }
}
