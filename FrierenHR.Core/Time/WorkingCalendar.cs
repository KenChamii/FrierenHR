using FrierenHR.Core.Enums;

namespace FrierenHR.Core.Time;

/// <summary>Decides which days count as worked, given a company's holidays and a shift's rest days.</summary>
public sealed class WorkingCalendar
{
    private readonly IReadOnlyDictionary<DateOnly, HolidayType> _holidays;

    public WorkingCalendar(IEnumerable<(DateOnly Date, HolidayType Type)> holidays, WeekDays restDays)
    {
        // Last one wins if a company records the same date twice.
        var map = new Dictionary<DateOnly, HolidayType>();
        foreach (var (date, type) in holidays) map[date] = type;

        _holidays = map;
        RestDays = restDays;
    }

    /// <summary>A calendar with no holidays and the given rest days.</summary>
    public static WorkingCalendar RestDaysOnly(WeekDays restDays) => new([], restDays);

    /// <summary>Every day is a working day. The behaviour before holidays existed.</summary>
    public static WorkingCalendar AlwaysOpen { get; } = new([], WeekDays.None);

    public WeekDays RestDays { get; }

    public bool IsRestDay(DateOnly date) => RestDays.Includes(date);

    public bool IsHoliday(DateOnly date) => _holidays.ContainsKey(date);

    public HolidayType? HolidayTypeOn(DateOnly date) =>
        _holidays.TryGetValue(date, out var type) ? type : null;

    public bool IsWorkingDay(DateOnly date) => !IsRestDay(date) && !IsHoliday(date);

    /// <summary>Working days from start to end, both inclusive.</summary>
    public int CountWorkingDays(DateOnly start, DateOnly endInclusive)
    {
        var count = 0;
        foreach (var _ in WorkingDays(start, endInclusive)) count++;
        return count;
    }

    /// <summary>The working days in the range, in order. Empty when the range is inverted.</summary>
    public IEnumerable<DateOnly> WorkingDays(DateOnly start, DateOnly endInclusive)
    {
        for (var date = start; date <= endInclusive; date = date.AddDays(1))
            if (IsWorkingDay(date)) yield return date;
    }

    /// <summary>Non-working days in the range, with the reason, for explaining a day count.</summary>
    public IEnumerable<(DateOnly Date, HolidayType? Holiday)> NonWorkingDays(DateOnly start, DateOnly endInclusive)
    {
        for (var date = start; date <= endInclusive; date = date.AddDays(1))
        {
            if (IsWorkingDay(date)) continue;
            yield return (date, HolidayTypeOn(date));
        }
    }
}
