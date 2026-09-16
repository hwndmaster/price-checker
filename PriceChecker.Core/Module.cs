using System.Diagnostics.CodeAnalysis;
using Genius.PriceChecker.Core.AgentHandlers;
using Genius.PriceChecker.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Genius.PriceChecker.Core;

[ExcludeFromCodeCoverage]
public static class Module
{
    /// <summary>
    ///   The configuration section the <see cref="ScanScheduleOptions"/> are bound from by the host.
    /// </summary>
    public const string ScanScheduleSection = "Scanning:Schedule";

    /// <summary>
    ///   The configuration section the <see cref="ScanPacingOptions"/> are bound from by the host.
    /// </summary>
    public const string ScanPacingSection = "Scanning:Pacing";

    public static void Configure(IServiceCollection services, ScanScheduleOptions scanScheduleOptions,
        ScanPacingOptions scanPacingOptions)
    {
        Guard.NotNull(scanPacingOptions);
        scanPacingOptions.Validate();

        // Scan schedule and pacing
        services.AddSingleton<IScanSchedule>(new ScanSchedule(scanScheduleOptions));
        services.AddSingleton(scanPacingOptions);

        // Services
        services.AddTransient<IPriceSeeker, PriceSeeker>();
        services.AddSingleton<IDelayService, DelayService>();
        services.AddSingleton<IScanSessionRunner, ScanSessionRunner>();
        services.AddTransient<IProductStatusProvider, ProductStatusProvider>();

        // Agent Handlers
        services.AddSingleton<IAgentHandlersProvider, AgentHandlersProvider>();
        services.AddTransient<SimpleRegex, SimpleRegex>();
        services.AddTransient<IAgentHandler, SimpleRegex>();
        services.AddTransient<IAgentHandler, SimpleRegexDivideBy100>();
    }
}
