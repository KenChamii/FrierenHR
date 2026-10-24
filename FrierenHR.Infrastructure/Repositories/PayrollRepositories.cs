using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Core.Entities;
using FrierenHR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FrierenHR.Infrastructure.Repositories;

public class PayrollDeductionRepository : CompanyScopedRepository<PayrollDeduction>, IPayrollDeductionRepository
{
    public PayrollDeductionRepository(FrierenHRDbContext context) : base(context) { }

    protected override IOrderedQueryable<PayrollDeduction> ApplyDefaultOrder(IQueryable<PayrollDeduction> query) =>
        query.OrderBy(d => d.SortOrder).ThenBy(d => d.Name);

    public async Task<List<PayrollDeduction>> GetActiveAsync(Guid companyId, CancellationToken ct = default) =>
        await DbSet.AsNoTracking()
            .Where(d => d.CompanyId == companyId && d.IsActive)
            .OrderBy(d => d.SortOrder).ThenBy(d => d.Name)
            .ToListAsync(ct);
}

public class PayslipRepository : IPayslipRepository
{
    private readonly FrierenHRDbContext _context;

    public PayslipRepository(FrierenHRDbContext context) => _context = context;

    public async Task<List<Payslip>> GetByEmployeeAsync(Guid employeeId, CancellationToken ct = default) =>
        await _context.Payslips.AsNoTracking()
            .Where(p => p.EmployeeId == employeeId)
            .OrderByDescending(p => p.PeriodEnd)
            .ToListAsync(ct);

    public async Task<List<Payslip>> GetForPeriodAsync(
        Guid companyId, DateOnly from, DateOnly to, CancellationToken ct = default) =>
        await _context.Payslips.AsNoTracking()
            .Include(p => p.Employee)
            .Where(p => p.CompanyId == companyId && p.PeriodStart == from && p.PeriodEnd == to)
            .OrderBy(p => p.Employee!.LastName).ThenBy(p => p.Employee!.FirstName)
            .ToListAsync(ct);

    public async Task<Payslip?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Payslips.AsNoTracking()
            .Include(p => p.Employee)
            .Include(p => p.Lines.OrderBy(l => l.SortOrder))
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task ReplaceForPeriodAsync(
        Guid companyId, DateOnly from, DateOnly to, List<Payslip> payslips, CancellationToken ct = default)
    {
        // Removed rather than updated in place.
        var existing = await _context.Payslips
            .Where(p => p.CompanyId == companyId && p.PeriodStart == from && p.PeriodEnd == to)
            .ToListAsync(ct);

        if (existing.Count > 0) _context.Payslips.RemoveRange(existing);

        await _context.Payslips.AddRangeAsync(payslips, ct);
        await _context.SaveChangesAsync(ct);
    }
}
