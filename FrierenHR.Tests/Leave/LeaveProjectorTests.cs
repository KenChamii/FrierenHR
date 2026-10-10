using FrierenHR.Core.Leave;

namespace FrierenHR.Tests.Leave;

public class LeaveProjectorTests
{
    [Fact]
    public void Without_a_cap_the_balance_grows_by_the_accrual_every_month()
    {
        var projection = LeaveProjector.Project(startingBalance: 5m, monthlyAccrual: 1.25m, cap: null, months: 12);

        Assert.Equal(20m, projection.ProjectedBalance);
        Assert.Equal(15m, projection.Accrued);
        Assert.Equal(0m, projection.Forfeited);
        Assert.False(projection.WillForfeit);
    }

    [Fact]
    public void A_cap_stops_the_balance_and_records_what_was_lost()
    {
        // 10 + 12 months at 1.25 would reach 25, so 10 days cannot be credited.
        var projection = LeaveProjector.Project(startingBalance: 10m, monthlyAccrual: 1.25m, cap: 15m, months: 12);

        Assert.Equal(15m, projection.ProjectedBalance);
        Assert.Equal(5m, projection.Accrued);
        Assert.Equal(10m, projection.Forfeited);
        Assert.True(projection.WillForfeit);
    }

    [Fact]
    public void The_month_the_cap_first_bites_is_reported()
    {
        // 13.00 -> 14.25 in full, then only 0.75 of the second month's 1.25 fits under the cap.
        var projection = LeaveProjector.Project(startingBalance: 13m, monthlyAccrual: 1.25m, cap: 15m, months: 6);

        Assert.Equal(2, projection.FirstForfeitingMonth);
    }

    [Fact]
    public void A_balance_already_over_its_cap_accrues_nothing_rather_than_going_negative()
    {
        // Possible after a cap is lowered. The balance must stand still, not be clawed back.
        var projection = LeaveProjector.Project(startingBalance: 20m, monthlyAccrual: 1.25m, cap: 15m, months: 3);

        Assert.Equal(20m, projection.ProjectedBalance);
        Assert.Equal(0m, projection.Accrued);
        Assert.Equal(3.75m, projection.Forfeited);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_horizon_of_zero_or_less_projects_nothing(int months)
    {
        var projection = LeaveProjector.Project(10m, 1.25m, 15m, months);

        Assert.Equal(10m, projection.ProjectedBalance);
        Assert.Equal(0, projection.MonthsProjected);
    }

    [Fact]
    public void An_accrual_of_zero_leaves_the_balance_alone()
    {
        var projection = LeaveProjector.Project(10m, 0m, 15m, 12);

        Assert.Equal(10m, projection.ProjectedBalance);
        Assert.Equal(0m, projection.Accrued);
    }

    [Theory]
    [InlineData(2026, 1, 1, 2027, 1, 1, 12)]
    [InlineData(2026, 1, 31, 2026, 2, 1, 1)]
    [InlineData(2026, 6, 1, 2026, 6, 30, 0)]   // same month, so no accrual run falls between
    [InlineData(2026, 6, 1, 2026, 1, 1, 0)]    // inverted range
    public void Months_between_counts_accrual_runs_not_elapsed_days(
        int y1, int m1, int d1, int y2, int m2, int d2, int expected)
    {
        var months = LeaveProjector.MonthsBetween(new DateOnly(y1, m1, d1), new DateOnly(y2, m2, d2));

        Assert.Equal(expected, months);
    }
}
