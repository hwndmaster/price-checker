using System.Diagnostics.CodeAnalysis;
using Genius.Atom.Data.Validation;
using Genius.PriceChecker.Core.Configuration;
using Genius.PriceChecker.WebApi.Services;
using Genius.PriceChecker.WebApi.Validators;

namespace Genius.PriceChecker.WebApi;

[ExcludeFromCodeCoverage]
public static class Module
{
    public static void Configure(IServiceCollection services, IConfiguration configuration)
    {
        Guard.NotNull(configuration);

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

        // Outbound notifications. Bound here rather than inside Core, which stays free of
        // configuration concerns; absent settings simply leave the integration off.
        services.Configure<TelegramSettings>(configuration.GetSection(TelegramSettings.SectionName));
        services.AddTransient<IPriceAlertNotifier, TelegramPriceAlertNotifier>();
    }
}
