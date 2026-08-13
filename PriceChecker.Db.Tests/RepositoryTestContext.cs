using Genius.Atom.Data.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Genius.PriceChecker.Db.Tests;

/// <summary>
///   A hand-rolled <see cref="IDatabaseContext"/> over an isolated in-memory database,
///   so that repository tests bypass the dependency injection.
/// </summary>
internal sealed class RepositoryTestContext : IDatabaseContext, IAsyncDisposable
{
    public RepositoryTestContext()
    {
        var options = new DbContextOptionsBuilder<PriceCheckerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        DbContext = new PriceCheckerDbContext(options);
    }

    public PriceCheckerDbContext DbContext { get; }

    public EntityEntry<TEntity> Entry<TEntity>(TEntity entity)
        where TEntity : class
        => DbContext.Entry(entity);

    public DbSet<TEntity> Set<TEntity>()
        where TEntity : class
        => DbContext.Set<TEntity>();

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => DbContext.SaveChangesAsync(cancellationToken);

    public ValueTask DisposeAsync()
        => DbContext.DisposeAsync();
}
