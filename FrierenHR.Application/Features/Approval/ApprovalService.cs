using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Features.Approval;
using FrierenHR.Core.Entities;
using FrierenHR.Core.Enums;

public class ApprovalService : IApprovalService
{
    private readonly IApprovalRepository _approvalRepository;

    public ApprovalService(IApprovalRepository approvalRepository) => _approvalRepository = approvalRepository;

    public async Task<ApprovalChainDto> CreateChainAsync(CreateApprovalChainDto dto, CancellationToken ct = default)
    {
        var chain = new ApprovalChain
        {
            CompanyId = dto.CompanyId,
            Name = dto.Name,
            Steps = dto.Steps.Select(s => new ApprovalChainStep
            {
                StepOrder = s.StepOrder,
                ApproverRole = s.ApproverRole,
                EscalateAfterDays = s.EscalateAfterDays
            }).ToList()
        };
        await _approvalRepository.AddChainAsync(chain, ct);
        await _approvalRepository.SaveChangesAsync(ct);
        return ToChainDto(chain);
    }

    public async Task<ApprovalChainDto?> GetChainForCompanyAsync(Guid companyId, CancellationToken ct = default)
    {
        var chain = await _approvalRepository.GetChainForCompanyAsync(companyId, ct);
        return chain is null ? null : ToChainDto(chain);
    }

    public async Task<ApprovalInstanceDto> StartInstanceAsync(Guid leaveRequestId, Guid chainId, CancellationToken ct = default)
    {
        var chain = await _approvalRepository.GetChainByIdAsync(chainId, ct)
            ?? throw new InvalidOperationException($"Approval chain '{chainId}' not found.");

        var instance = new ApprovalInstance
        {
            LeaveRequestId = leaveRequestId,
            ApprovalChainId = chainId,
            CurrentStepOrder = chain.Steps.Min(s => s.StepOrder),
            Status = ApprovalStatus.Pending,
            StepStartedAt = DateTime.UtcNow
        };
        await _approvalRepository.AddInstanceAsync(instance, ct);
        await _approvalRepository.SaveChangesAsync(ct);
        return ToInstanceDto(instance, chain);
    }

    public async Task<ApprovalInstanceDto> AdvanceAsync(Guid instanceId, bool approve, CancellationToken ct = default)
    {
        var instance = await _approvalRepository.GetInstanceByIdAsync(instanceId, ct)
            ?? throw new InvalidOperationException($"Approval instance '{instanceId}' not found.");
        var chain = await _approvalRepository.GetChainByIdAsync(instance.ApprovalChainId, ct)
            ?? throw new InvalidOperationException($"Approval chain '{instance.ApprovalChainId}' not found.");

        if (!approve)
        {
            instance.Status = ApprovalStatus.Rejected;
        }
        else
        {
            var steps = chain.Steps.OrderBy(s => s.StepOrder).ToList();
            var nextStep = steps.FirstOrDefault(s => s.StepOrder > instance.CurrentStepOrder);
            if (nextStep is null)
            {
                instance.Status = ApprovalStatus.Approved; // last step approved -> whole chain approved
            }
            else
            {
                instance.CurrentStepOrder = nextStep.StepOrder;
                instance.StepStartedAt = DateTime.UtcNow;
                // Status stays Pending, now waiting on the next step's approver role.
            }
        }

        _approvalRepository.UpdateInstance(instance);
        await _approvalRepository.SaveChangesAsync(ct);
        return ToInstanceDto(instance, chain);
    }

    public async Task EscalateOverdueAsync(CancellationToken ct = default)
    {
        var pendingInstances = await _approvalRepository.GetAllPendingInstancesAsync(ct);
        foreach (var instance in pendingInstances)
        {
            var chain = await _approvalRepository.GetChainByIdAsync(instance.ApprovalChainId, ct);
            var currentStep = chain?.Steps.FirstOrDefault(s => s.StepOrder == instance.CurrentStepOrder);
            if (currentStep is null) continue;

            var daysWaiting = (DateTime.UtcNow - instance.StepStartedAt).TotalDays;
            if (daysWaiting >= currentStep.EscalateAfterDays)
            {
                instance.Status = ApprovalStatus.Escalated;
                _approvalRepository.UpdateInstance(instance);
            }
        }
        await _approvalRepository.SaveChangesAsync(ct);
    }

    private static ApprovalChainDto ToChainDto(ApprovalChain c) => new(c.Id, c.CompanyId, c.Name,
        c.Steps.OrderBy(s => s.StepOrder).Select(s => new ApprovalChainStepDto(s.StepOrder, s.ApproverRole, s.EscalateAfterDays)).ToList());

    private static ApprovalInstanceDto ToInstanceDto(ApprovalInstance i, ApprovalChain chain)
    {
        var role = chain.Steps.FirstOrDefault(s => s.StepOrder == i.CurrentStepOrder)?.ApproverRole ?? EmployeeRole.Manager;
        return new ApprovalInstanceDto(i.Id, i.LeaveRequestId, i.CurrentStepOrder, i.Status, role);
    }
}