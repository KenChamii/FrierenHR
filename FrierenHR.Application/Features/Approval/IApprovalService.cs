using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Core.Entities;
using FrierenHR.Core.Enums;

namespace FrierenHR.Application.Features.Approval;

public interface IApprovalService
{
    Task<ApprovalChainDto> CreateChainAsync(CreateApprovalChainDto dto, CancellationToken ct = default);
    Task<ApprovalChainDto?> GetChainForCompanyAsync(Guid companyId, CancellationToken ct = default);
    Task<ApprovalInstanceDto> StartInstanceAsync(Guid leaveRequestId, Guid chainId, CancellationToken ct = default);
    Task<ApprovalInstanceDto> AdvanceAsync(Guid instanceId, bool approve, CancellationToken ct = default);
    Task EscalateOverdueAsync(CancellationToken ct = default);
}