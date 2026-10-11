using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Core.Entities;

namespace FrierenHR.Application.Features.Scheduling;

public class HolidayService : IHolidayService
{
    /// <summary>Said on the review screen, so nobody saves the list believing it is official.</summary>
    private const string FeedCaveat =
        "Public holidays only, and every one is suggested as Regular. "
        + "Special Non-Working days are declared by proclamation and are not in this list, "
        + "so check the types and the dates against the official calendar before importing.";

    private const string FeedSource = "Nager.Date";

    private readonly IHolidayRepository _holidayRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IWorkingCalendarProvider _calendarProvider;
    private readonly IHolidayFeed _holidayFeed;

    public HolidayService(
        IHolidayRepository holidayRepository,
        IDepartmentRepository departmentRepository,
        IWorkingCalendarProvider calendarProvider,
        IHolidayFeed holidayFeed)
    {
        _holidayRepository = holidayRepository;
        _departmentRepository = departmentRepository;
        _calendarProvider = calendarProvider;
        _holidayFeed = holidayFeed;
    }

    public async Task<HolidaySuggestionsDto> GetSuggestionsAsync(
        Guid companyId, int year, string countryCode, CancellationToken ct = default)
    {
        if (year < 2000 || year > 2100)
            throw new InvalidOperationException("Pick a year between 2000 and 2100.");

        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Trim().Length != 2)
            throw new InvalidOperationException("A two-letter country code is required, such as PH.");

        var suggestions = await _holidayFeed.GetForYearAsync(year, countryCode.Trim(), ct);

        // Company-wide entries only. A department-specific holiday on the same date does not
        // make the company-wide one redundant, so it should not suppress the suggestion.
        var existingDates = (await _holidayRepository.GetByCompanyAsync(companyId, ct))
            .Where(h => h.DepartmentId is null && h.Date.Year == year)
            .Select(h => h.Date)
            .ToHashSet();

        var rows = suggestions
            .Select(s => new HolidaySuggestionDto(
                s.Date, s.Name, Core.Enums.HolidayType.Regular, existingDates.Contains(s.Date)))
            .ToList();

        return new HolidaySuggestionsDto(year, countryCode.Trim().ToUpperInvariant(), FeedSource, FeedCaveat, rows);
    }

    public async Task<List<HolidayDto>> GetForYearAsync(Guid companyId, int year, CancellationToken ct = default)
    {
        var from = new DateOnly(year, 1, 1);
        var to = new DateOnly(year, 12, 31);

        // GetByCompanyAsync rather than GetForRangeAsync, because the admin screen shows every
        // scope including department-specific entries, which the range query filters out.
        var all = (await _holidayRepository.GetByCompanyAsync(companyId, ct))
            .Where(h => h.Date >= from && h.Date <= to)
            .ToList();

        var departments = await _departmentRepository.GetByCompanyAsync(companyId, ct);
        var namesById = departments.ToDictionary(d => d.Id, d => d.Name);

        return all
            .OrderBy(h => h.Date)
            .Select(h => ToDto(h, h.DepartmentId is not null && namesById.TryGetValue(h.DepartmentId.Value, out var n) ? n : null))
            .ToList();
    }

    public async Task<HolidayDto> SaveAsync(SaveHolidayDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("A holiday needs a name.");

        // Checked here so the caller gets a sentence rather than a unique index violation.
        var clash = await _holidayRepository.GetByDateAsync(dto.CompanyId, dto.Date, dto.DepartmentId, ct);
        if (clash is not null && clash.Id != dto.Id)
            throw new InvalidOperationException($"{dto.Date:d MMM yyyy} is already recorded as '{clash.Name}'.");

        var entity = dto.Id is null
            ? new Holiday { CompanyId = dto.CompanyId }
            : await _holidayRepository.GetForCompanyAsync(dto.Id.Value, dto.CompanyId, ct)
              ?? throw new InvalidOperationException($"Holiday '{dto.Id}' not found for this company.");

        entity.Date = dto.Date;
        entity.Name = dto.Name.Trim();
        entity.Type = dto.Type;
        entity.DepartmentId = dto.DepartmentId;

        if (dto.Id is null) await _holidayRepository.AddAsync(entity, ct);
        else { entity.UpdatedAt = DateTime.UtcNow; _holidayRepository.Update(entity); }

        await _holidayRepository.SaveChangesAsync(ct);
        return ToDto(entity, null);
    }

    public async Task DeleteAsync(Guid id, Guid companyId, CancellationToken ct = default)
    {
        var holiday = await _holidayRepository.GetForCompanyAsync(id, companyId, ct)
            ?? throw new InvalidOperationException($"Holiday '{id}' not found for this company.");

        _holidayRepository.Remove(holiday);
        await _holidayRepository.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Skips rather than fails on a date already recorded, because a year's calendar is
    /// usually pasted in twice at some point and losing the other eleven months to one
    /// duplicate helps nobody.
    /// </summary>
    public async Task<HolidayImportResultDto> ImportAsync(
        Guid companyId, IEnumerable<SaveHolidayDto> holidays, CancellationToken ct = default)
    {
        var added = 0;
        var skipped = 0;
        var messages = new List<string>();
        var seen = new HashSet<(DateOnly, Guid?)>();

        foreach (var dto in holidays)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                skipped++;
                messages.Add($"{dto.Date:d MMM}: skipped, no name.");
                continue;
            }

            // Guards against duplicates inside the batch as well as against the database.
            if (!seen.Add((dto.Date, dto.DepartmentId))
                || await _holidayRepository.GetByDateAsync(companyId, dto.Date, dto.DepartmentId, ct) is not null)
            {
                skipped++;
                messages.Add($"{dto.Date:d MMM}: already recorded.");
                continue;
            }

            await _holidayRepository.AddAsync(new Holiday
            {
                CompanyId = companyId,
                Date = dto.Date,
                Name = dto.Name.Trim(),
                Type = dto.Type,
                DepartmentId = dto.DepartmentId
            }, ct);
            added++;
        }

        if (added > 0) await _holidayRepository.SaveChangesAsync(ct);
        return new HolidayImportResultDto(added, skipped, messages);
    }

    public async Task<WorkingDaysDto> CountWorkingDaysAsync(
        Guid employeeId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        if (to < from) throw new InvalidOperationException("The end date cannot be before the start date.");

        var calendar = await _calendarProvider.ForEmployeeAsync(employeeId, from, to, ct);

        var nonWorking = calendar.NonWorkingDays(from, to)
            .Select(d => new NonWorkingDayDto(
                d.Date,
                d.Holiday is not null ? d.Holiday.ToString()! : d.Date.DayOfWeek.ToString()))
            .ToList();

        var calendarDays = to.DayNumber - from.DayNumber + 1;
        return new WorkingDaysDto(from, to, calendarDays, calendar.CountWorkingDays(from, to), nonWorking);
    }

    private static HolidayDto ToDto(Holiday h, string? departmentName) =>
        new(h.Id, h.CompanyId, h.Date, h.Name, h.Type, h.DepartmentId, departmentName);
}
