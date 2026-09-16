using Genius.Atom.Infrastructure.Logging;
using Genius.Atom.Web.Telemetry;
using Genius.PriceChecker.WebApi.Hubs;
using Genius.PriceChecker.WebApi.JsonConverters;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddAtomWebTelemetry(options =>
{
    options.ApplicationName = builder.Environment.ApplicationName;
    options.ActivitySourceName = "Genius.PriceChecker.WebApi.Mvc";

    // The deployed app host supervises this service over /health, so the endpoints have to exist in
    // Production too. Atom's default maps them in Development only, which left them returning 404 on
    // the server and made health supervision impossible.
    options.MapHealthEndpointsInDevelopmentOnly = false;

    // SignalR's own long-lived connections would otherwise produce a request-summary line each.
    options.RequestLogIgnoredPathPrefixes.Add("/hubs");
});

builder.Environment.ContentRootPath = Path.Combine(AppContext.BaseDirectory);
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "Logs"));

// ReplaceHostDefaults: CreateBuilder registers the Console, Debug and EventSource providers and Atom
// adds Serilog, so without this every line reaches the console twice. Atom removes just those four by
// type, leaving the OpenTelemetry provider AddAtomWebTelemetry registered above untouched.
Genius.Atom.Infrastructure.Module.Configure(builder.Services, builder.Configuration,
    options => options.LoggingMode = AtomLoggingMode.ReplaceHostDefaults);
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

app.LogAtomStartupSummary(summary => summary
    .AddFile("Database", dbPath)
    .Add("Legacy import path", legacyDataPath));

await app.RunAsync().ConfigureAwait(false);
