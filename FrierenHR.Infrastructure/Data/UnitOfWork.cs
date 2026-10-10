using FrierenHR.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FrierenHR.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly FrierenHRDbContext _context;

    public UnitOfWork(FrierenHRDbContext context) => _context = context;

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> work, CancellationToken ct = default)
    {
        // Run through the execution strategy. A retrying strategy refuses to work with a
        // transaction opened outside it, and retrying the whole block is the correct behaviour
        // anyway: a transient failure part way through should replay from the start.
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);

            var result = await work(ct);

            await transaction.CommitAsync(ct);
            return result;
        });
    }
}
