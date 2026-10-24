using FrierenHR.Application.Common.DTOs;

namespace FrierenHR.Application.Features.Payroll;

public interface IPayrollDeductionService
{
    Task<List<PayrollDeductionDto>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default);
    Task<PayrollDeductionDto> SaveAsync(SavePayrollDeductionDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid companyId, CancellationToken ct = default);
}

public interface IPayslipService
{
    /// <summary>Issues payslips for every employee with hours in the period and a recorded rate.</summary>
    Task<PayslipRunDto> GenerateAsync(
        Guid companyId, DateOnly from, DateOnly to, CancellationToken ct = default);

    Task<List<PayslipSummaryDto>> GetForEmployeeAsync(Guid employeeId, CancellationToken ct = default);

    Task<List<PayslipSummaryDto>> GetForPeriodAsync(
        Guid companyId, DateOnly from, DateOnly to, CancellationToken ct = default);

    Task<PayslipDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
