using FrierenHR.Application.Common.DTOs;

namespace FrierenHR.Application.Features.Scheduling;

public interface IHolidayService
{
    Task<List<HolidayDto>> GetForYearAsync(Guid companyId, int year, CancellationToken ct = default);
    Task<HolidayDto> SaveAsync(SaveHolidayDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid companyId, CancellationToken ct = default);

    /// <summary>
    /// A year's public holidays from an outside source, for review. Saves nothing: the
    /// source lags proclamations and cannot tell a Regular holiday from a Special
    /// Non-Working one, so an administrator corrects the list before importing it.
    /// </summary>
    Task<HolidaySuggestionsDto> GetSuggestionsAsync(
        Guid companyId, int year, string countryCode, CancellationToken ct = default);

    /// <summary>Adds many at once, skipping dates already recorded rather than failing the batch.</summary>
    Task<HolidayImportResultDto> ImportAsync(
        Guid companyId, IEnumerable<SaveHolidayDto> holidays, CancellationToken ct = default);

    /// <summary>
    /// Working days in a range for one employee, with the non-working days listed. Used by
    /// the leave request form so someone sees the deduction before they submit.
    /// </summary>
    Task<WorkingDaysDto> CountWorkingDaysAsync(
        Guid employeeId, DateOnly from, DateOnly to, CancellationToken ct = default);
}
