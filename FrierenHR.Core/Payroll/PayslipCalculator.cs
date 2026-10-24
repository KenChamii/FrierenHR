using FrierenHR.Core.Enums;

namespace FrierenHR.Core.Payroll;

/// <summary>A deduction to apply, flattened out of the stored definition.</summary>
public sealed record DeductionRule(string Name, DeductionKind Kind, decimal Value, int SortOrder);

/// <summary>One priced line, ready to become a PayslipLine.</summary>
public sealed record CalculatedLine(
    string Label, PayslipLineKind Kind, decimal Amount, string? Basis, int SortOrder);

/// <summary>What an employee is paid for a period, and how it was arrived at.</summary>
public sealed record PayslipCalculation(
    decimal HourlyRate,
    decimal RegularPay,
    decimal OvertimePay,
    decimal GrossPay,
    decimal TotalDeductions,
    decimal NetPay,
    IReadOnlyList<CalculatedLine> Lines);

/// <summary>Turns hours into money.</summary>
public static class PayslipCalculator
{
    /// <summary>Money is rounded to two places once, at the point it becomes a line.</summary>
    private const int MoneyPlaces = 2;

    /// <summary>Prices a period.</summary>
    /// <param name="hourlyRate">Derived from monthly pay by the caller, so the divisor lives in one place.</param>
    /// <param name="regularHours">Hours up to the standard day, as the payroll export reports them.</param>
    /// <param name="payableOvertimeHours">Overtime hours with the company's multiplier already applied.</param>
    public static PayslipCalculation Calculate(
        decimal hourlyRate,
        decimal regularHours,
        decimal payableOvertimeHours,
        IEnumerable<DeductionRule> deductions)
    {
        if (hourlyRate < 0) throw new ArgumentOutOfRangeException(nameof(hourlyRate));

        var regular = Math.Max(0, regularHours);
        var overtime = Math.Max(0, payableOvertimeHours);

        var regularPay = Round(regular * hourlyRate);
        var overtimePay = Round(overtime * hourlyRate);
        var gross = regularPay + overtimePay;

        var lines = new List<CalculatedLine>
        {
            new("Regular hours", PayslipLineKind.Earning, regularPay,
                $"{Trim(regular)} h x {Money(hourlyRate)}", 0),
        };

        // Shown only when there is overtime. A zero line on every payslip is noise.
        if (overtimePay > 0)
        {
            lines.Add(new CalculatedLine("Overtime", PayslipLineKind.Earning, overtimePay,
                $"{Trim(overtime)} h x {Money(hourlyRate)}", 1));
        }

        var totalDeductions = 0m;
        var sortOrder = 2;

        // Every percentage is taken on gross, never on the running balance.
        foreach (var deduction in deductions.OrderBy(d => d.SortOrder).ThenBy(d => d.Name))
        {
            var amount = deduction.Kind switch
            {
                DeductionKind.FixedAmount => Round(deduction.Value),
                DeductionKind.PercentOfGross => Round(gross * deduction.Value / 100m),
                _ => 0m,
            };

            if (amount <= 0) continue;

            var basis = deduction.Kind == DeductionKind.PercentOfGross
                ? $"{Trim(deduction.Value)}% of {Money(gross)}"
                : null;

            lines.Add(new CalculatedLine(
                deduction.Name, PayslipLineKind.Deduction, amount, basis, sortOrder++));

            totalDeductions += amount;
        }

        // Deductions larger than gross leave nothing to pay rather than a negative payslip.
        var net = Math.Max(0, gross - totalDeductions);

        return new PayslipCalculation(
            hourlyRate, regularPay, overtimePay, gross, totalDeductions, net, lines);
    }

    /// <summary>The hourly rate implied by a monthly rate, or zero when either is missing.</summary>
    public static decimal HourlyRateFrom(decimal? monthlyRate, decimal standardMonthlyHours) =>
        monthlyRate is { } monthly && standardMonthlyHours > 0
            ? Round(monthly / standardMonthlyHours)
            : 0m;

    private static decimal Round(decimal value) =>
        Math.Round(value, MoneyPlaces, MidpointRounding.AwayFromZero);

    private static string Money(decimal value) => value.ToString("N2");

    private static string Trim(decimal value) => value.ToString("0.##");
}
