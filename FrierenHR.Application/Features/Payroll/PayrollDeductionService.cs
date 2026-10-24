using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Core.Entities;
using FrierenHR.Core.Enums;

namespace FrierenHR.Application.Features.Payroll;

/// <summary>The deduction lines a company applies to every payslip.</summary>
public class PayrollDeductionService : IPayrollDeductionService
{
    /// <summary>A percentage deduction above this is almost certainly a typo.</summary>
    private const decimal MaxPercent = 100m;

    private readonly IPayrollDeductionRepository _repository;

    public PayrollDeductionService(IPayrollDeductionRepository repository) => _repository = repository;

    public async Task<List<PayrollDeductionDto>> GetByCompanyAsync(
        Guid companyId, CancellationToken ct = default)
    {
        var deductions = await _repository.GetByCompanyAsync(companyId, ct);
        return deductions.Select(ToDto).ToList();
    }

    public async Task<PayrollDeductionDto> SaveAsync(
        SavePayrollDeductionDto dto, CancellationToken ct = default)
    {
        var name = (dto.Name ?? string.Empty).Trim();
        if (name.Length == 0) throw new InvalidOperationException("A deduction needs a name.");
        if (dto.Value < 0) throw new InvalidOperationException("A deduction cannot be negative.");

        if (dto.Kind == DeductionKind.PercentOfGross && dto.Value > MaxPercent)
            throw new InvalidOperationException($"A percentage deduction cannot be above {MaxPercent}%.");

        var entity = dto.Id is { } id
            ? await _repository.GetForCompanyAsync(id, dto.CompanyId, ct)
                ?? throw new InvalidOperationException("That deduction was not found.")
            : new PayrollDeduction { CompanyId = dto.CompanyId };

        entity.Name = name;
        entity.Kind = dto.Kind;
        entity.Value = dto.Value;
        entity.SortOrder = dto.SortOrder;
        entity.IsActive = dto.IsActive;

        if (dto.Id is null) await _repository.AddAsync(entity, ct);
        else
        {
            entity.UpdatedAt = DateTime.UtcNow;
            _repository.Update(entity);
        }

        await _repository.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    public async Task DeleteAsync(Guid id, Guid companyId, CancellationToken ct = default)
    {
        var entity = await _repository.GetForCompanyAsync(id, companyId, ct)
            ?? throw new InvalidOperationException("That deduction was not found.");

        // Deactivated rather than removed.
        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        _repository.Update(entity);
        await _repository.SaveChangesAsync(ct);
    }

    private static PayrollDeductionDto ToDto(PayrollDeduction d) =>
        new(d.Id, d.CompanyId, d.Name, d.Kind, d.Value, d.SortOrder, d.IsActive);
}
