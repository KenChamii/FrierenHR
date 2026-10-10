using FrierenHR.Core.RulesEngine.Models;

namespace FrierenHR.Core.RulesEngine.Schema;

/// <summary>How a value is entered and compared. Drives the input control and the operator list.</summary>
public enum RuleValueKind
{
    Integer,
    Decimal,
    Text,
    Enum
}

/// <summary>A fact the evaluator can read for a rule type.</summary>
/// <param name="Name">Key as it appears in the rule JSON and in <see cref="RuleContext"/>.</param>
/// <param name="Label">What an administrator sees instead of the key.</param>
/// <param name="Unit">Appended after the input, e.g. "months". Null when the value needs no unit.</param>
/// <param name="Operators">Only the operators that make sense for this value kind.</param>
/// <param name="Options">Allowed values when <paramref name="Kind"/> is <see cref="RuleValueKind.Enum"/>.</param>
public record RuleFieldSchema(
    string Name,
    string Label,
    RuleValueKind Kind,
    string[] Operators,
    string? Unit = null,
    string[]? Options = null);

/// <summary>One parameter of an action, e.g. the amount on AccrueDays.</summary>
public record RuleActionParamSchema(
    string Name,
    string Label,
    RuleValueKind Kind,
    bool Required,
    string? Unit = null);

/// <summary>An action the executor can carry out for a rule type.</summary>
/// <param name="Summary">Sentence fragment used to build the plain-English preview.</param>
public record RuleActionSchema(
    string Type,
    string Label,
    string Summary,
    RuleActionParamSchema[] Parameters);

public record RuleTypeSchema(
    string RuleType,
    string Label,
    string Description,
    RuleFieldSchema[] Fields,
    RuleActionSchema[] Actions)
{
    /// <summary>False when the engine supports this rule type but nothing calls it yet.</summary>
    public bool Wired { get; init; } = true;

    /// <summary>A working rule to start from, for seeding and for the "new rule" form.</summary>
    public required RuleDefinition Starter { get; init; }
}
