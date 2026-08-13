using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Genius.PriceChecker.Db;

/// <summary>
///   Used by the EF Core design-time tools (dotnet ef migrations ...).
/// </summary>
internal sealed class PriceCheckerDbContextFactory : IDesignTimeDbContextFactory<PriceCheckerDbContext>
{
    public PriceCheckerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PriceCheckerDbContext>();
        optionsBuilder.UseSqlite("Data Source=../Data/PriceChecker.db;Foreign Keys=True");

        return new PriceCheckerDbContext(optionsBuilder.Options);
    }
}
