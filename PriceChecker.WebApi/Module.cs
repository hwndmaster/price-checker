using System.Diagnostics.CodeAnalysis;
using Genius.Atom.Data.Validation;
using Genius.PriceChecker.WebApi.Services;
using Genius.PriceChecker.WebApi.Validators;

namespace Genius.PriceChecker.WebApi;

[ExcludeFromCodeCoverage]
public static class Module
{
    public static void Configure(IServiceCollection services)
    {
        // Request validators
        services
            .AddTransient<IRequestValidator, CreateAgentRequestValidator>()
            .AddTransient<IRequestValidator, UpdateAgentRequestValidator>()
            .AddTransient<IRequestValidator, CreateProductRequestValidator>()
            .AddTransient<IRequestValidator, UpdateProductRequestValidator>();

        // Price scanning
        services.AddSingleton<IScanContext, ScanContext>();
        services.AddSingleton<IScanNotifier, ScanHubNotifier>();
        services.AddSingleton<IScanOrchestrator, ScanOrchestrator>();
        services.AddHostedService<ScheduledScanHostedService>();
    }
}
