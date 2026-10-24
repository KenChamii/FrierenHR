using FrierenHR.Core.Enums;
using FrierenHR.Core.Time;

namespace FrierenHR.Tests.Time;

public class WorkingCalendarTests
{
    // Mon 5 Jan 2026 through Sun 11 Jan 2026, so weekday and weekend cases are explicit.
    private static readonly DateOnly Monday = new(2026, 1, 5);
    private static readonly DateOnly Wednesday = new(2026, 1, 7);
    private static readonly DateOnly Friday = new(2026, 1, 9);
    private static readonly DateOnly Saturday = new(2026, 1, 10);
    private static readonly DateOnly Sunday = new(2026, 1, 11);

    private static WorkingCalendar WithHolidays(params (DateOnly, HolidayType)[] holidays) =>
        new(holidays, WeekDays.Weekend);

    [Fact]
    public void A_full_week_with_a_weekend_rest_counts_five_working_days()
    {
        Assert.Equal(5, WorkingCalendar.RestDaysOnly(WeekDays.Weekend).CountWorkingDays(Monday, Sunday));
    }

    [Fact]
    public void A_holiday_inside_the_range_is_not_counted()
    {
        var calendar = WithHolidays((Wednesday, HolidayType.Regular));

        Assert.Equal(4, calendar.CountWorkingDays(Monday, Sunday));
    }

    [Fact]
    public void A_holiday_falling_on_a_rest_day_does_not_subtract_twice()
    {
        var calendar = WithHolidays((Saturday, HolidayType.Regular));

        Assert.Equal(5, calendar.CountWorkingDays(Monday, Sunday));
    }

    [Fact]
    public void A_single_working_day_counts_one()
    {
        Assert.Equal(1, WorkingCalendar.RestDaysOnly(WeekDays.Weekend).CountWorkingDays(Monday, Monday));
    }

    [Fact]
    public void A_single_rest_day_counts_zero()
    {
        Assert.Equal(0, WorkingCalendar.RestDaysOnly(WeekDays.Weekend).CountWorkingDays(Saturday, Saturday));
    }

    [Fact]
    public void An_inverted_range_counts_zero_rather_than_throwing()
    {
        Assert.Equal(0, WorkingCalendar.RestDaysOnly(WeekDays.Weekend).CountWorkingDays(Friday, Monday));
    }

    [Fact]
    public void AlwaysOpen_counts_every_calendar_day()
    {
        Assert.Equal(7, WorkingCalendar.AlwaysOpen.CountWorkingDays(Monday, Sunday));
    }

    [Fact]
    public void Rest_days_other_than_the_weekend_are_honoured()
    {
        // A Friday and Saturday rest week, as observed in parts of the world.
        var calendar = WorkingCalendar.RestDaysOnly(WeekDays.Friday | WeekDays.Saturday);

        Assert.Equal(5, calendar.CountWorkingDays(Monday, Sunday));
        Assert.False(calendar.IsWorkingDay(Friday));
        Assert.True(calendar.IsWorkingDay(Sunday));
    }

    [Fact]
    public void The_holiday_type_is_reported_for_a_date_that_has_one()
    {
        var calendar = WithHolidays((Monday, HolidayType.SpecialNonWorking));

        Assert.Equal(HolidayType.SpecialNonWorking, calendar.HolidayTypeOn(Monday));
        Assert.Null(calendar.HolidayTypeOn(Friday));
    }

    [Fact]
    public void A_duplicate_date_takes_the_last_entry_rather_than_throwing()
    {
        // Two rows for one date is a data-entry mistake, not a reason to fail every query that touches the month.
        var calendar = WithHolidays(
            (Monday, HolidayType.Regular),
            (Monday, HolidayType.CompanySpecific));

        Assert.Equal(HolidayType.CompanySpecific, calendar.HolidayTypeOn(Monday));
    }

    [Fact]
    public void Working_days_are_listed_in_order()
    {
        var days = WorkingCalendar.RestDaysOnly(WeekDays.Weekend).WorkingDays(Monday, Sunday).ToList();

        Assert.Equal(5, days.Count);
        Assert.Equal(Monday, days[0]);
        Assert.Equal(Friday, days[^1]);
        Assert.Equal(days, days.OrderBy(d => d));
    }

    [Fact]
    public void Non_working_days_report_the_holiday_that_caused_them()
    {
        var calendar = WithHolidays((Wednesday, HolidayType.Regular));

        var nonWorking = calendar.NonWorkingDays(Monday, Sunday).ToList();

        Assert.Equal(3, nonWorking.Count); // the holiday plus Saturday and Sunday
        Assert.Equal(HolidayType.Regular, nonWorking.Single(d => d.Holiday is not null).Holiday);
        Assert.Equal(2, nonWorking.Count(d => d.Holiday is null));
    }

    [Fact]
    public void A_range_spanning_a_year_boundary_is_counted_correctly()
    {
        // Thu 31 Dec 2026 to Mon 4 Jan 2027. Thursday, Friday and Monday are working days, and 1 January is a holiday.
        var calendar = WithHolidays((new DateOnly(2027, 1, 1), HolidayType.Regular));

        Assert.Equal(2, calendar.CountWorkingDays(new DateOnly(2026, 12, 31), new DateOnly(2027, 1, 4)));
    }

    [Theory]
    [InlineData(DayOfWeek.Monday, true)]
    [InlineData(DayOfWeek.Friday, true)]
    [InlineData(DayOfWeek.Saturday, false)]
    [InlineData(DayOfWeek.Sunday, false)]
    public void WeekDays_maps_calendar_days_onto_its_flags(DayOfWeek day, bool expectedWorking)
    {
        Assert.Equal(expectedWorking, !WeekDays.Weekend.Includes(day));
    }
}
