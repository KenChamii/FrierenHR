using System.Text.Json;
using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Core.Common;
using FrierenHR.Core.Entities;
using FrierenHR.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FrierenHR.Infrastructure.Data.Auditing;

/// <summary>
/// Writes the audit trail from the change tracker, so a new feature is audited without its
/// author remembering to do anything. Services stay free of audit code entirely.
///
/// Two deliberate limits. Properties listed in <see cref="IgnoredProperties"/> are skipped,
/// because recording that UpdatedAt changed on every row is noise that buries the real
/// edits. And a failure here is swallowed: losing an audit row is bad, but failing the
/// user's save because the audit write failed is worse.
/// </summary>
public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private static readonly HashSet<string> IgnoredProperties = new(StringComparer.Ordinal)
    {
        // Timestamps change on every row and would bury the real edits.
        nameof(BaseEntity.CreatedAt),
        nameof(BaseEntity.UpdatedAt),
        // Never copy a credential into a second table that is read by administrators and
        // deliberately never deleted. That a password changed is recorded; the hash is not.
        nameof(Employee.PasswordHash),
    };

    /// <summary>Types that are never audited, to avoid auditing the audit trail.</summary>
    private static readonly HashSet<Type> ExcludedTypes = [typeof(AuditEntry)];

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ICurrentUser _currentUser;

    public AuditSaveChangesInterceptor(ICurrentUser currentUser) => _currentUser = currentUser;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        if (eventData.Context is not null) Capture(eventData.Context);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null) Capture(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    private void Capture(DbContext context)
    {
        try
        {
            // Materialise first: adding audit rows below mutates the tracker, and enumerating
            // it while it changes throws.
            var tracked = context.ChangeTracker.Entries<BaseEntity>()
                .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .Where(e => !ExcludedTypes.Contains(e.Entity.GetType()))
                .ToList();

            var entries = new List<AuditEntry>();
            foreach (var tracker in tracked)
            {
                var changes = DescribeChanges(tracker);
                // A modification that touched nothing but the ignored properties is not a change.
                if (tracker.State == EntityState.Modified && changes.Count == 0) continue;

                entries.Add(new AuditEntry
                {
                    EntityName = tracker.Entity.GetType().Name,
                    EntityId = tracker.Entity.Id,
                    Action = tracker.State switch
                    {
                        EntityState.Added => AuditAction.Created,
                        EntityState.Deleted => AuditAction.Deleted,
                        _ => AuditAction.Updated
                    },
                    CompanyId = (tracker.Entity as ICompanyScoped)?.CompanyId ?? _currentUser.CompanyId,
                    ChangedByEmployeeId = _currentUser.EmployeeId,
                    ChangedAt = DateTime.UtcNow,
                    Changes = JsonSerializer.Serialize(changes, JsonOptions)
                });
            }

            if (entries.Count > 0) context.Set<AuditEntry>().AddRange(entries);
        }
        catch
        {
            // Never fail the caller's save because auditing failed.
        }
    }

    private static Dictionary<string, object?> DescribeChanges(EntityEntry<BaseEntity> tracker)
    {
        var changes = new Dictionary<string, object?>();

        foreach (var property in tracker.Properties)
        {
            var name = property.Metadata.Name;
            if (IgnoredProperties.Contains(name)) continue;

            switch (tracker.State)
            {
                case EntityState.Added:
                    if (property.CurrentValue is not null) changes[name] = property.CurrentValue;
                    break;

                case EntityState.Deleted:
                    if (property.OriginalValue is not null) changes[name] = property.OriginalValue;
                    break;

                case EntityState.Modified when property.IsModified
                                               && !Equals(property.OriginalValue, property.CurrentValue):
                    changes[name] = new { from = property.OriginalValue, to = property.CurrentValue };
                    break;
            }
        }

        return changes;
    }
}
