namespace FrierenHR.Core.RulesEngine;

public class RuleContext
{
    private readonly Dictionary<string, object?> _facts = new(StringComparer.OrdinalIgnoreCase);
    public RuleContext() { }
    public RuleContext(IDictionary<string, object?> facts) { foreach (var kv in facts) _facts[kv.Key] = kv.Value; }
    public RuleContext Set(string field, object? value) { _facts[field] = value; return this; }
    public bool TryGet(string field, out object? value) => _facts.TryGetValue(field, out value);
    public IReadOnlyDictionary<string, object?> Facts => _facts;
}