using FrierenHR.Core.Enums;
using FrierenHR.Core.Payroll;

namespace FrierenHR.Tests.Payroll;

public class PayslipCalculatorTests
{
    private static DeductionRule Fixed(string name, decimal amount, int order = 0) =>
        new(name, DeductionKind.FixedAmount, amount, order);

    private static DeductionRule Percent(string name, decimal percent, int order = 0) =>
        new(name, DeductionKind.PercentOfGross, percent, order);

    [Fact]
    public void Gross_is_regular_hours_plus_overtime_at_the_same_rate()
    {
        // Overtime arrives already multiplied, so the rate applied here is the plain one.
        var result = PayslipCalculator.Calculate(250m, regularHours: 80m, payableOvertimeHours: 10m, []);

        Assert.Equal(20_000m, result.RegularPay);
        Assert.Equal(2_500m, result.OvertimePay);
        Assert.Equal(22_500m, result.GrossPay);
        Assert.Equal(22_500m, result.NetPay);
    }

    [Fact]
    public void An_overtime_line_appears_only_when_there_is_overtime()
    {
        var without = PayslipCalculator.Calculate(250m, 80m, 0m, []);
        var with = PayslipCalculator.Calculate(250m, 80m, 4m, []);

        Assert.Single(without.Lines);
        Assert.Equal(2, with.Lines.Count);
        Assert.Contains(with.Lines, l => l.Label == "Overtime");
    }

    [Fact]
    public void A_fixed_deduction_subtracts_its_amount()
    {
        var result = PayslipCalculator.Calculate(250m, 80m, 0m, [Fixed("Staff loan", 1_500m)]);

        Assert.Equal(1_500m, result.TotalDeductions);
        Assert.Equal(18_500m, result.NetPay);
    }

    [Fact]
    public void A_percentage_deduction_is_taken_on_gross()
    {
        var result = PayslipCalculator.Calculate(250m, 80m, 0m, [Percent("Provident fund", 5m)]);

        Assert.Equal(1_000m, result.TotalDeductions);
        Assert.Equal(19_000m, result.NetPay);
    }

    [Fact]
    public void Percentages_are_all_taken_on_gross_so_their_order_cannot_change_net_pay()
    {
        // Were each percentage taken on the running balance.
        var oneWay = PayslipCalculator.Calculate(
            250m, 80m, 0m, [Percent("A", 5m, 0), Percent("B", 10m, 1)]);

        var otherWay = PayslipCalculator.Calculate(
            250m, 80m, 0m, [Percent("B", 10m, 0), Percent("A", 5m, 1)]);

        Assert.Equal(3_000m, oneWay.TotalDeductions);
        Assert.Equal(oneWay.NetPay, otherWay.NetPay);
    }

    [Fact]
    public void Deductions_larger_than_gross_leave_nothing_to_pay_rather_than_a_negative_slip()
    {
        var result = PayslipCalculator.Calculate(250m, 8m, 0m, [Fixed("Advance", 5_000m)]);

        Assert.Equal(2_000m, result.GrossPay);
        Assert.Equal(5_000m, result.TotalDeductions); // shown in full, not trimmed to fit
        Assert.Equal(0m, result.NetPay);
    }

    [Fact]
    public void A_deduction_worth_nothing_is_left_off_the_payslip()
    {
        var result = PayslipCalculator.Calculate(250m, 80m, 0m, [Fixed("Unused", 0m)]);

        Assert.Single(result.Lines);
        Assert.Equal(0m, result.TotalDeductions);
    }

    [Fact]
    public void Hours_are_never_negative_even_if_the_caller_passes_them_that_way()
    {
        var result = PayslipCalculator.Calculate(250m, -10m, -5m, []);

        Assert.Equal(0m, result.GrossPay);
    }

    [Fact]
    public void A_negative_rate_is_rejected_rather_than_producing_a_negative_payslip()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PayslipCalculator.Calculate(-1m, 80m, 0m, []));
    }

    // Written as double literals on purpose. An int literal in InlineData cannot be converted to a double?
    [Theory]
    [InlineData(34_800d, 174d, 200d)]
    [InlineData(30_000d, 174d, 172.41d)]
    [InlineData(null, 174d, 0d)]     // no rate recorded
    [InlineData(30_000d, 0d, 0d)]    // no divisor, so no derivable rate
    public void The_hourly_rate_comes_from_the_monthly_rate_and_the_standard_hours(
        double? monthly, double hours, double expected)
    {
        var rate = PayslipCalculator.HourlyRateFrom(
            monthly is null ? null : (decimal)monthly.Value, (decimal)hours);

        Assert.Equal((decimal)expected, rate);
    }
}
