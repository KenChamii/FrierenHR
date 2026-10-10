using FrierenHR.Core.Enums;

namespace FrierenHR.Core.Time;

public static class WeekDaysExtensions
{
    /// <summary>Maps a calendar day onto its WeekDays flag.</summary>
    public static WeekDays ToWeekDays(this DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => WeekDays.Monday,
        DayOfWeek.Tuesday => WeekDays.Tuesday,
        DayOfWeek.Wednesday => WeekDays.Wednesday,
        DayOfWeek.Thursday => WeekDays.Thursday,
        DayOfWeek.Friday => WeekDays.Friday,
        DayOfWeek.Saturday => WeekDays.Saturday,
        DayOfWeek.Sunday => WeekDays.Sunday,
        _ => WeekDays.None
    };

    public static bool Includes(this WeekDays set, DayOfWeek day) => set.HasFlag(day.ToWeekDays());

    public static bool Includes(this WeekDays set, DateOnly date) => set.Includes(date.DayOfWeek);
}
