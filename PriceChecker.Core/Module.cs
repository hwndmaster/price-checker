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

    public static void Configure(IServiceCollection services, ScanScheduleOptions scanScheduleOptions)
    {
        // Scan schedule
        services.AddSingleton<IScanSchedule>(new ScanSchedule(scanScheduleOptions));

        // Services
        services.AddTransient<IPriceSeeker, PriceSeeker>();
        services.AddTransient<IProductStatusProvider, ProductStatusProvider>();

        // Agent Handlers
        services.AddSingleton<IAgentHandlersProvider, AgentHandlersProvider>();
        services.AddTransient<SimpleRegex, SimpleRegex>();
        services.AddTransient<IAgentHandler, SimpleRegex>();
        services.AddTransient<IAgentHandler, SimpleRegexDivideBy100>();
    }
}
