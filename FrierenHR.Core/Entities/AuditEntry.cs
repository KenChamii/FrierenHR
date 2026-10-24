using FrierenHR.Core.Common;
using FrierenHR.Core.Enums;

namespace FrierenHR.Core.Entities;

/// <summary>One recorded change to one record.</summary>
public class AuditEntry : BaseEntity
{
    /// <summary>CLR type name of the record that changed, e.g. "CompanyRuleConfig".</summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>Primary key of the record that changed.</summary>
    public Guid EntityId { get; set; }

    public AuditAction Action { get; set; }

    /// <summary>Set when the changed record is company scoped.</summary>
    public Guid? CompanyId { get; set; }

    /// <summary>Null for a change made by a background job rather than a person.</summary>
    public Guid? ChangedByEmployeeId { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>The properties that changed, as JSON: { "Priority": { "from": 0, "to": 1 } }.</summary>
    public string Changes { get; set; } = "{}";
}
