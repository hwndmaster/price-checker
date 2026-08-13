using System.Diagnostics.CodeAnalysis;
using Genius.Atom.Data.Ef;
using Genius.PriceChecker.Db.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Db;

[ExcludeFromCodeCoverage]
public static class Module
{
    public static void Configure(IServiceCollection services, IConfiguration configuration)
    {
        DatabaseContextRegistration.Register<PriceCheckerDbContext>(services)
            .WithBackup(configuration);

        services.AddTransient<IAgentsRepository, AgentsRepository>();
        services.AddTransient<IProductsRepository, ProductsRepository>();
    }

    public static async Task InitializeAsync(IServiceProvider serviceProvider, string legacyDataPath)
    {
        Guard.NotNull(serviceProvider);

        using var scope = serviceProvider.CreateScope();

        var migrator = scope.ServiceProvider.GetRequiredService<IDatabaseMigrator>();
        await migrator.MigrateWithBackupAsync().ConfigureAwait(false);

        var dbContext = scope.ServiceProvider.GetRequiredService<PriceCheckerDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(Module).FullName!);
        await LegacyJsonDataImporter.ImportAsync(dbContext, legacyDataPath, logger).ConfigureAwait(false);
    }
}
