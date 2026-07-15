using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Features.Approval;
using Microsoft.AspNetCore.Mvc;

namespace FrierenHR.WebAPI.Controllers;

[ApiController]
[Route("api/approval-chains")]
public class ApprovalChainsController : ControllerBase
{
    private readonly IApprovalService _approvalService;
    public ApprovalChainsController(IApprovalService approvalService) => _approvalService = approvalService;

    [HttpPost]
    public async Task<ActionResult<ApprovalChainDto>> Create(CreateApprovalChainDto dto, CancellationToken ct) =>
        Ok(await _approvalService.CreateChainAsync(dto, ct));

    [HttpGet("by-company/{companyId:guid}")]
    public async Task<ActionResult<ApprovalChainDto?>> GetForCompany(Guid companyId, CancellationToken ct) =>
        Ok(await _approvalService.GetChainForCompanyAsync(companyId, ct));

    [HttpPost("instances/{instanceId:guid}/advance")]
    public async Task<ActionResult<ApprovalInstanceDto>> Advance(Guid instanceId, [FromQuery] bool approve, CancellationToken ct) =>
        Ok(await _approvalService.AdvanceAsync(instanceId, approve, ct));
}