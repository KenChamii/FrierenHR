namespace FrierenHR.Application.Common.Interfaces;

/// <summary>Runs several writes as one database transaction.</summary>
public interface IUnitOfWork
{
    /// <summary>Commits only if <paramref name="work"/> returns.</summary>
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken ct = default);
}
