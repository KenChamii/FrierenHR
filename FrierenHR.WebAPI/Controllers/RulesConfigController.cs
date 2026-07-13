using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Features.RulesConfig;
using Microsoft.AspNetCore.Mvc;

namespace FrierenHR.WebAPI.Controllers;

[ApiController]
[Route("api/rules-config")]
public class RulesConfigController : ControllerBase
{
    private readonly IRuleConfigService _ruleConfigService;
    public RulesConfigController(IRuleConfigService ruleConfigService) => _ruleConfigService = ruleConfigService;

    // POST /api/rules-config — save/update a rule set for a company (the exit-criteria endpoint)
    [HttpPost]
    public async Task<ActionResult<RuleConfigDto>> Save(SaveRuleConfigDto dto, CancellationToken ct)
    {
        try { return Ok(await _ruleConfigService.SaveAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    // POST /api/rules-config/test-evaluate — standalone eval: takes { companyId, ruleType, facts },
    // loads active rules, builds a RuleContext from facts, runs EvaluateFirstMatch, returns the result.
    [HttpPost("test-evaluate")]
    public async Task<ActionResult<RuleEvaluationResultDto>> TestEvaluate(TestEvaluateRequestDto dto, CancellationToken ct) =>
        Ok(await _ruleConfigService.TestEvaluateAsync(dto, ct));
}