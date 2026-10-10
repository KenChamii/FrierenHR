using FrierenHR.Core.Common;
using FrierenHR.Core.Entities;

namespace FrierenHR.Application.Common.Interfaces;

/// <summary>
/// Reads and writes for a company-scoped entity. The company filter lives here rather than at
/// each call site, because in a multi-tenant system a forgotten one leaks another company's
/// rows instead of merely returning the wrong answer.
/// </summary>
public interface ICompanyScopedRepository<T> : IRepository<T> where T : BaseEntity, ICompanyScoped
{
    Task<List<T>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default);

    /// <summary>Loads one row only if it belongs to the given company, otherwise null.</summary>
    Task<T?> GetForCompanyAsync(Guid id, Guid companyId, CancellationToken ct = default);
}

public interface IHolidayRepository : ICompanyScopedRepository<Holiday>
{
    /// <summary>
    /// Holidays that apply to the employee's department, which means company-wide holidays
    /// plus any recorded for that department. Pass null to get company-wide only.
    /// </summary>
    Task<List<Holiday>> GetForRangeAsync(
        Guid companyId, DateOnly from, DateOnly to, Guid? departmentId = null, CancellationToken ct = default);

    /// <summary>Used to reject a duplicate before it reaches the unique index.</summary>
    Task<Holiday?> GetByDateAsync(
        Guid companyId, DateOnly date, Guid? departmentId, CancellationToken ct = default);
}

public interface IShiftRepository : ICompanyScopedRepository<Shift>
{
    Task<Shift?> GetDefaultAsync(Guid companyId, CancellationToken ct = default);

    /// <summary>
    /// The employee with their shift and their department's shift loaded, so the resolution
    /// chain can run without three round trips.
    /// </summary>
    Task<Employee?> GetEmployeeWithShiftsAsync(Guid employeeId, CancellationToken ct = default);
}

public interface IAttendanceCorrectionRepository : IRepository<AttendanceCorrection>
{
    Task<List<AttendanceCorrection>> GetByEmployeeAsync(Guid employeeId, CancellationToken ct = default);

    /// <summary>Pending requests a manager may decide: their reports, or company-wide for HR.</summary>
    Task<List<AttendanceCorrection>> GetPendingForApproverAsync(
        Guid approverEmployeeId, bool companyWide, CancellationToken ct = default);

    /// <summary>Stops a second request for a day that already has one awaiting a decision.</summary>
    Task<AttendanceCorrection?> GetPendingForDateAsync(
        Guid employeeId, DateOnly date, CancellationToken ct = default);
}

public interface IAuditRepository
{
    /// <summary>
    /// Most recent first. Optional filters narrow to one record's history or one kind of
    /// record, which is how the trail is actually read.
    /// </summary>
    Task<List<AuditEntry>> GetForCompanyAsync(
        Guid companyId, string? entityName = null, Guid? entityId = null,
        int skip = 0, int take = 50, CancellationToken ct = default);

    Task<int> CountForCompanyAsync(
        Guid companyId, string? entityName = null, Guid? entityId = null, CancellationToken ct = default);
}
