using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Features.Approval;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrierenHR.WebAPI.Controllers;

[ApiController]
[Route("api/approval-chains")]
[Authorize]
public class ApprovalChainsController : ControllerBase
{
    private readonly IApprovalService _approvalService;
    public ApprovalChainsController(IApprovalService approvalService) => _approvalService = approvalService;

    // Defining a company's approval chain (who approves what, in what order) is org configuration.
    [HttpPost, Authorize(Roles = "HRAdmin")]
    public async Task<ActionResult<ApprovalChainDto>> Create(CreateApprovalChainDto dto, CancellationToken ct) =>
        Ok(await _approvalService.CreateChainAsync(dto, ct));

    [HttpGet("by-company/{companyId:guid}")]
    public async Task<ActionResult<ApprovalChainDto?>> GetForCompany(Guid companyId, CancellationToken ct) =>
        Ok(await _approvalService.GetChainForCompanyAsync(companyId, ct));

    // NOTE: this doesn't yet verify the caller is actually the approver at the instance's current step (that requires loading the chain + step + requester role, which the Application layer doesn't expose yet).
    [HttpPost("instances/{instanceId:guid}/advance"), Authorize(Roles = "Manager,HRAdmin")]
    public async Task<ActionResult<ApprovalInstanceDto>> Advance(Guid instanceId, [FromQuery] bool approve, CancellationToken ct) =>
        Ok(await _approvalService.AdvanceAsync(instanceId, approve, ct));
}
