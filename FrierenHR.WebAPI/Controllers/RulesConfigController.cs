using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Features.RulesConfig;
using FrierenHR.WebAPI.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrierenHR.WebAPI.Controllers;

[ApiController]
[Route("api/rules-config")]
[Authorize(Roles = "HRAdmin")]
public class RulesConfigController : ControllerBase
{
    private readonly IRuleConfigService _ruleConfigService;
    public RulesConfigController(IRuleConfigService ruleConfigService) => _ruleConfigService = ruleConfigService;

    // GET /api/rules-config — list every rule configured for the caller's own company (any RuleType, active or not).
    [HttpGet]
    public async Task<ActionResult<List<RuleConfigDto>>> GetMine(CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Forbid();
        return Ok(await _ruleConfigService.GetByCompanyAsync(companyId.Value, ct));
    }

    // POST /api/rules-config — save/update a rule set for a company (the exit-criteria endpoint)
    [HttpPost]
    public async Task<ActionResult<RuleConfigDto>> Save(SaveRuleConfigDto dto, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Forbid();
        // Force the caller's own company regardless of what the request body says — same
        // pattern as Employees/Departments, so nobody can write rules into another company.
        if (dto.CompanyId != companyId) dto = dto with { CompanyId = companyId.Value };

        try { return Ok(await _ruleConfigService.SaveAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    // POST /api/rules-config/seed-defaults — fills in a starter rule for any RuleType this
    // company hasn't configured yet, so there's always something real to test against and
    // tweak instead of starting from a blank JSON box. Safe to call more than once.
    [HttpPost("seed-defaults")]
    public async Task<ActionResult<List<RuleConfigDto>>> SeedDefaults(CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Forbid();
        return Ok(await _ruleConfigService.SeedDefaultsAsync(companyId.Value, ct));
    }

    // POST /api/rules-config/test-evaluate — standalone eval: takes { companyId, ruleType, facts },
    // loads active rules, builds a RuleContext from facts, runs EvaluateFirstMatch, returns the result.
    [HttpPost("test-evaluate")]
    public async Task<ActionResult<RuleEvaluationResultDto>> TestEvaluate(TestEvaluateRequestDto dto, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Forbid();
        if (dto.CompanyId != companyId) dto = dto with { CompanyId = companyId.Value };
        return Ok(await _ruleConfigService.TestEvaluateAsync(dto, ct));
    }
}
