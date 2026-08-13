using Genius.Atom.Web.Telemetry;
using Genius.PriceChecker.WebApi.Hubs;
using Genius.PriceChecker.WebApi.JsonConverters;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddAtomWebTelemetry(options =>
{
    options.ApplicationName = builder.Environment.ApplicationName;
    options.ActivitySourceName = "Genius.PriceChecker.WebApi.Mvc";
});

builder.Environment.ContentRootPath = Path.Combine(AppContext.BaseDirectory);
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "Logs"));

Genius.Atom.Infrastructure.Module.Configure(builder.Services, builder.Configuration);
Genius.Atom.Data.Module.Configure(builder.Services);
Genius.Atom.Web.Module.Configure(builder,
    new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0),
    configureJsonOptions: jsonOptions => JsonSetup.SetupJsonOptions(jsonOptions));

// Bound here rather than inside Core, so that the domain library stays free of configuration concerns.
// Absent keys keep the defaults declared on ScanScheduleOptions; a malformed value fails fast.
var scanScheduleOptions = builder.Configuration
    .GetSection(Genius.PriceChecker.Core.Module.ScanScheduleSection)
    .Get<Genius.PriceChecker.Core.ScanScheduleOptions>() ?? new();

Genius.PriceChecker.Core.Module.Configure(builder.Services, scanScheduleOptions);
Genius.PriceChecker.Db.Module.Configure(builder.Services, builder.Configuration);
Genius.PriceChecker.WebApi.Module.Configure(builder.Services);

builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        Genius.Atom.Data.JsonConverters.JsonSetup.SetupJsonOptions(options.PayloadSerializerOptions);
        JsonSetup.SetupJsonOptions(options.PayloadSerializerOptions);
    });

var dataPath = Path.Combine(builder.Environment.ContentRootPath, "Data");
Directory.CreateDirectory(dataPath);
var dbPath = Path.Combine(dataPath, "PriceChecker.db");
builder.Services.AddDbContext<Genius.PriceChecker.Db.PriceCheckerDbContext>(options =>
{
    options.UseSqlite($"Data Source={dbPath};Foreign Keys=True");
});

builder.AddReactAppCors();

var app = builder.Build();

var legacyDataPath = builder.Configuration["Database:LegacyImportPath"] is { Length: > 0 } configuredPath
    ? Path.Combine(builder.Environment.ContentRootPath, configuredPath)
    : dataPath;

Genius.Atom.Infrastructure.Module.Initialize(app.Services);
await Genius.PriceChecker.Db.Module.InitializeAsync(app.Services, legacyDataPath).ConfigureAwait(false);
Genius.Atom.Web.Module.Initialize(app);

app.UseReactAppCors();
app.MapAtomWebTelemetryEndpoints();
app.MapControllers();
app.MapHub<ScanHub>("/hubs/scan");

await app.RunAsync().ConfigureAwait(false);
