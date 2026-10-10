using FrierenHR.Core.Common;
using FrierenHR.Core.Enums;

namespace FrierenHR.Core.Entities;

/// <summary>
/// One recorded change to one record. Written by the save interceptor rather than by each
/// service, so a new feature is audited without its author remembering to do anything.
///
/// Append-only by intent: nothing in the application updates or deletes these rows.
/// </summary>
public class AuditEntry : BaseEntity
{
    /// <summary>CLR type name of the record that changed, e.g. "CompanyRuleConfig".</summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>Primary key of the record that changed.</summary>
    public Guid EntityId { get; set; }

    public AuditAction Action { get; set; }

    /// <summary>
    /// Set when the changed record is company scoped, so an administrator can read their own
    /// company's trail without seeing anyone else's.
    /// </summary>
    public Guid? CompanyId { get; set; }

    /// <summary>Null for a change made by a background job rather than a person.</summary>
    public Guid? ChangedByEmployeeId { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The properties that changed, as JSON: { "Priority": { "from": 0, "to": 1 } }. Stored as
    /// a document because the shape differs per entity and nothing queries inside it.
    /// </summary>
    public string Changes { get; set; } = "{}";
}
