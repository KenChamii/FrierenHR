using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Features.Onboarding;
using FrierenHR.WebAPI.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrierenHR.WebAPI.Controllers;

[ApiController]
[Route("api/setup")]
// HR only. Setup is their work, and an employee seeing a list of things their company has
// not configured would be noise at best.
[Authorize(Roles = "HRAdmin")]
public class SetupController : ControllerBase
{
    private readonly ISetupProgressService _setup;
    private readonly ISampleDataService _sampleData;
    private readonly bool _sampleDataEnabled;

    public SetupController(ISetupProgressService setup, ISampleDataService sampleData, IConfiguration configuration)
    {
        _setup = setup;
        _sampleData = sampleData;

        // Off unless a deployment opts in. Sample staff are fine in a portfolio demo and
        // clutter in a company somebody actually runs.
        _sampleDataEnabled = configuration.GetValue<bool>("Demo:AllowSampleData");
    }

    // GET /api/setup/progress
    [HttpGet("progress")]
    public async Task<ActionResult<SetupProgressDto>> Progress(CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Forbid();
        return Ok(await _setup.GetAsync(companyId.Value, ct));
    }

    // GET /api/setup/sample-data
    [HttpGet("sample-data")]
    public async Task<ActionResult<SampleDataStatusDto>> SampleDataStatus(CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Forbid();
        var available = _sampleDataEnabled && await _sampleData.IsAvailableAsync(companyId.Value, ct);
        return Ok(new SampleDataStatusDto(available));
    }

    // POST /api/setup/sample-data
    [HttpPost("sample-data")]
    public async Task<IActionResult> LoadSampleData(CancellationToken ct)
    {
        if (!_sampleDataEnabled) return NotFound();
        var companyId = User.GetCompanyId();
        if (companyId is null) return Forbid();

        try
        {
            await _sampleData.LoadAsync(companyId.Value, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
