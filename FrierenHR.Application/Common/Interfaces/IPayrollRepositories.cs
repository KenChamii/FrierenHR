using FrierenHR.Core.Entities;

namespace FrierenHR.Application.Common.Interfaces;

public interface IPayrollDeductionRepository : ICompanyScopedRepository<PayrollDeduction>
{
    /// <summary>Active lines only, in the order they appear on a payslip.</summary>
    Task<List<PayrollDeduction>> GetActiveAsync(Guid companyId, CancellationToken ct = default);
}

public interface IPayslipRepository
{
    Task<List<Payslip>> GetByEmployeeAsync(Guid employeeId, CancellationToken ct = default);

    Task<List<Payslip>> GetForPeriodAsync(
        Guid companyId, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>With its lines, for the detail view.</summary>
    Task<Payslip?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Replaces any payslip already issued for the same employee and period, so regenerating
    /// after a corrected timesheet produces one payslip rather than two.
    /// </summary>
    Task ReplaceForPeriodAsync(
        Guid companyId, DateOnly from, DateOnly to, List<Payslip> payslips, CancellationToken ct = default);
}
