var builder = DistributedApplication.CreateBuilder(args);

var appHostMode = builder.Configuration["AppHost:Mode"];
var runDashboardOnly = string.Equals(appHostMode, "dashboard", StringComparison.OrdinalIgnoreCase);

if (!runDashboardOnly)
{
    var api = builder.AddProject<Projects.WebApi>("webapi")
        .WithHttpHealthCheck("/health");

    var web = builder.AddViteApp("web", "../PriceChecker.Web", "start:aspire")
        .WithPnpm()
        .WithReference(api)
        .WithEnvironment("VITE_API_URL", api.GetEndpoint("http"))
        .WithExternalHttpEndpoints()
        .WaitFor(api);

    api.WithEnvironment("Cors__Origins__0", web.GetEndpoint("http"));
}

await builder.Build().RunAsync().ConfigureAwait(false);
