namespace FrierenHR.Application.Common.Interfaces;

/// <summary>
/// Runs several writes as one database transaction.
///
/// Shaped as a callback rather than Begin/Commit/Rollback so the Application layer never holds
/// a transaction object it has to remember to dispose, and so nothing here needs to reference
/// EF Core.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Commits only if <paramref name="work"/> returns. Any exception rolls the whole thing
    /// back and is rethrown unchanged, so callers keep handling their own failures.
    /// </summary>
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken ct = default);
}
