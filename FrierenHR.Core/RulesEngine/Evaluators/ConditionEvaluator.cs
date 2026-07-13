using System.Globalization;
using FrierenHR.Core.RulesEngine.Models;

namespace FrierenHR.Core.RulesEngine.Evaluators;

public static class ConditionEvaluator
{
    private static readonly string[] SupportedOps = { ">=", "<=", "==", "!=", "in" };

    public static bool AllMatch(IEnumerable<RuleCondition> conditions, RuleContext context, out string? error)
    {
        error = null;
        foreach (var condition in conditions)
        {
            var result = Matches(condition, context, out var conditionError);
            if (conditionError is not null) { error = conditionError; return false; }
            if (!result) return false;
        }
        return true;
    }

    public static bool Matches(RuleCondition condition, RuleContext context, out string? error)
    {
        error = null;
        if (!SupportedOps.Contains(condition.Op))
        {
            error = $"Unsupported operator '{condition.Op}'. Supported: {string.Join(", ", SupportedOps)}";
            return false;
        }
        if (!context.TryGet(condition.Field, out var actualRaw) || actualRaw is null)
        {
            error = $"Field '{condition.Field}' missing from rule context.";
            return false;
        }
        if (condition.Op == "in")
        {
            var candidates = ToStringList(condition.Value);
            return candidates.Any(c => string.Equals(c, actualRaw.ToString(), StringComparison.OrdinalIgnoreCase));
        }
        if (TryToDecimal(actualRaw, out var actualNum) && TryToDecimal(condition.Value, out var expectedNum))
        {
            return condition.Op switch
            {
                ">=" => actualNum >= expectedNum,
                "<=" => actualNum <= expectedNum,
                "==" => actualNum == expectedNum,
                "!=" => actualNum != expectedNum,
                _ => false
            };
        }
        var actualStr = actualRaw.ToString() ?? string.Empty;
        var expectedStr = condition.Value?.ToString() ?? string.Empty;
        return condition.Op switch
        {
            "==" => string.Equals(actualStr, expectedStr, StringComparison.OrdinalIgnoreCase),
            "!=" => !string.Equals(actualStr, expectedStr, StringComparison.OrdinalIgnoreCase),
            _ => throw new InvalidOperationException($"Operator '{condition.Op}' requires numeric operands but field '{condition.Field}' is not numeric.")
        };
    }

    private static bool TryToDecimal(object? value, out decimal result)
    {
        result = 0;
        if (value is null) return false;
        if (value is decimal d) { result = d; return true; }
        if (value is int i) { result = i; return true; }
        if (value is double db) { result = (decimal)db; return true; }
        if (value is long l) { result = l; return true; }
        return decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }

    private static List<string> ToStringList(object? value)
    {
        if (value is null) return new();
        if (value is IEnumerable<object> list) return list.Select(v => v.ToString() ?? "").ToList();
        return value.ToString()!.Split(',').Select(s => s.Trim()).ToList();
    }
}