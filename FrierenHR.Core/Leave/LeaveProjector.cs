namespace FrierenHR.Core.Leave;

/// <summary>
/// What a balance becomes if accrual keeps running, and how much of it is lost at the cap.
/// </summary>
/// <param name="StartingBalance">The balance the projection began from.</param>
/// <param name="MonthsProjected">Accrual runs projected, which may be zero.</param>
/// <param name="Accrued">Days actually added, after the cap took its share.</param>
/// <param name="Forfeited">Days the cap refused. This is the number worth acting on.</param>
/// <param name="ProjectedBalance">Starting balance plus what was accrued.</param>
public sealed record LeaveProjection(
    decimal StartingBalance,
    int MonthsProjected,
    decimal Accrued,
    decimal Forfeited,
    decimal ProjectedBalance,
    decimal? Cap)
{
    /// <summary>True when the cap costs the employee something over the projected period.</summary>
    public bool WillForfeit => Forfeited > 0;

    /// <summary>
    /// The first month in which the cap starts refusing days, counted from 1, or null when it
    /// never does. Lets a reminder say when to take leave rather than only that some is lost.
    /// </summary>
    public int? FirstForfeitingMonth { get; init; }
}

/// <summary>
/// Runs accrual forward month by month. Pure and dependency-free, so the arithmetic behind
/// "you will lose 2.5 days by December" is testable on its own.
///
/// Month by month rather than one multiplication, because the cap applies at every accrual
/// run. An employee sitting at 14 of a 15 day cap and earning 1.25 a month gains 1 day in
/// total, not 1.25 times the number of months.
/// </summary>
public static class LeaveProjector
{
    public static LeaveProjection Project(
        decimal startingBalance, decimal monthlyAccrual, decimal? cap, int months)
    {
        // Nothing to project. Reported as the balance unchanged rather than thrown, because a
        // horizon in the past is a caller's validation problem and zero is the honest answer.
        if (months <= 0 || monthlyAccrual <= 0)
            return new LeaveProjection(startingBalance, 0, 0, 0, startingBalance, cap);

        var balance = startingBalance;
        var accrued = 0m;
        var forfeited = 0m;
        int? firstForfeitingMonth = null;

        for (var month = 1; month <= months; month++)
        {
            var granted = monthlyAccrual;

            if (cap is not null)
            {
                var room = Math.Max(0, cap.Value - balance);
                granted = Math.Min(monthlyAccrual, room);

                var lost = monthlyAccrual - granted;
                if (lost > 0)
                {
                    forfeited += lost;
                    firstForfeitingMonth ??= month;
                }
            }

            balance += granted;
            accrued += granted;
        }

        return new LeaveProjection(startingBalance, months, accrued, forfeited, balance, cap)
        {
            FirstForfeitingMonth = firstForfeitingMonth
        };
    }

    /// <summary>
    /// Whole accrual runs between two dates. Used to project to year end without every caller
    /// doing calendar arithmetic of its own.
    /// </summary>
    public static int MonthsBetween(DateOnly from, DateOnly to)
    {
        if (to <= from) return 0;
        return Math.Max(0, (to.Year - from.Year) * 12 + (to.Month - from.Month));
    }
}
