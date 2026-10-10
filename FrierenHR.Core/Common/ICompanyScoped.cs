namespace FrierenHR.Core.Common;

/// <summary>
/// An entity that belongs to exactly one company.
///
/// Multi-tenancy is enforced by a WHERE clause on every query, so a forgotten one is a data
/// leak rather than a bug someone notices. Declaring the relationship on the type lets the
/// repository base class and the audit interceptor apply it generically instead of each
/// caller remembering.
/// </summary>
public interface ICompanyScoped
{
    Guid CompanyId { get; }
}
