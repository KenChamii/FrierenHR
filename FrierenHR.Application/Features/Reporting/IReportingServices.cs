using FrierenHR.Application.Common.DTOs;

namespace FrierenHR.Application.Features.Reporting;

public interface IAvailabilityService
{
    /// <summary>
    /// Who is in and who is out across a date range, with the days that are thinnest on
    /// cover called out. Capped at a few weeks by the caller, since the grid is read a week
    /// or two at a time.
    /// </summary>
    Task<AvailabilityDto> GetAsync(
        Guid companyId, DateOnly from, DateOnly to, Guid? departmentId = null, CancellationToken ct = default);
}

public interface ILeaveForecastService
{
    /// <summary>
    /// What each balance becomes by the horizon if accrual keeps running, and what the cap
    /// will refuse. Possible only because the accrual rate is configuration the engine can
    /// be asked about.
    /// </summary>
    Task<LeaveForecastDto> ForEmployeeAsync(
        Guid employeeId, DateOnly? horizon = null, CancellationToken ct = default);
}

public interface IPayrollExportService
{
    Task<PayrollPeriodDto> BuildAsync(
        Guid companyId, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>The same figures as a CSV, which is how they reach a payroll system.</summary>
    Task<byte[]> BuildCsvAsync(
        Guid companyId, DateOnly from, DateOnly to, CancellationToken ct = default);
}

public interface ILeaveLiabilityService
{
    /// <summary>
    /// Accrued unused leave valued at a daily rate. The rate is supplied by the caller
    /// because salaries are not modelled, which is stated in the report rather than hidden.
    /// </summary>
    Task<LiabilityDto> GetAsync(Guid companyId, decimal dailyRate, CancellationToken ct = default);

    Task<byte[]> GetCsvAsync(Guid companyId, decimal dailyRate, CancellationToken ct = default);
}
