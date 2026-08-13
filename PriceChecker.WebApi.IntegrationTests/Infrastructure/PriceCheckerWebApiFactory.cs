using Genius.PriceChecker.Db;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Genius.PriceChecker.WebApi.IntegrationTests.Infrastructure;

internal sealed class PriceCheckerWebApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _databaseConnection;
    private readonly string _emptyLegacyDataPath;

    public PriceCheckerWebApiFactory()
    {
        _databaseConnection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        _databaseConnection.Open();          // keep-alive: closing it drops the in-memory DB

        _emptyLegacyDataPath = Path.Combine(Path.GetTempPath(), $"pricechecker-it-{Guid.NewGuid()}");
        Directory.CreateDirectory(_emptyLegacyDataPath);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTests");
        builder.ConfigureAppConfiguration((_, configurationBuilder) => configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            // No legacy JSON files there, so the one-time import is skipped.
            ["Database:LegacyImportPath"] = _emptyLegacyDataPath,
        }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<PriceCheckerDbContext>>();
            services.RemoveAll<PriceCheckerDbContext>();
            services.AddDbContext<PriceCheckerDbContext>(options => options.UseSqlite(_databaseConnection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _databaseConnection.Dispose();

            // Dispose may be invoked more than once (sync + async disposal paths).
            if (Directory.Exists(_emptyLegacyDataPath))
            {
                Directory.Delete(_emptyLegacyDataPath, recursive: true);
            }
        }
    }
}
