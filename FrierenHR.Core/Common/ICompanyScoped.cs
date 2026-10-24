namespace FrierenHR.Core.Common;

/// <summary>An entity that belongs to exactly one company.</summary>
public interface ICompanyScoped
{
    Guid CompanyId { get; }
}
