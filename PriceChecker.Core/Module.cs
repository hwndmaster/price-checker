using System.Diagnostics.CodeAnalysis;
using Genius.Atom.Data.JsonPersistence;
using Genius.Atom.Infrastructure.Commands;
using Genius.PriceChecker.Core.AgentHandlers;
using Genius.PriceChecker.Core.CommandHandlers;
using Genius.PriceChecker.Core.Commands;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Persistence;
using Genius.PriceChecker.Core.Repositories;
using Genius.PriceChecker.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Genius.PriceChecker.Core;

[ExcludeFromCodeCoverage]
public static class Module
{
    public static void Configure(IServiceCollection services)
    {
        // Json persistence
        services.AddSingleton<IJsonConverter, GuidReferenceJsonConverter<AgentRef>>();
        services.AddSingleton<IJsonConverter, GuidReferenceJsonConverter<ProductRef>>();

        // Repositories
        services.RegisterJsonRepository<Guid, AgentRef, Agent, AgentRepository, IAgentQueryService, IAgentRepository>();
        services.RegisterJsonRepository<Guid, ProductRef, Product, ProductRepository, IProductQueryService, IProductRepository>();
        services.AddSingleton<ISettingsRepository, SettingsRepository>();

        // Services
        services.AddTransient<IPriceSeeker, PriceSeeker>();
        services.AddTransient<IProductStatusProvider, ProductStatusProvider>();
        services.AddSingleton<IProductPriceManager, ProductPriceManager>();

        // Agent Handlers
        services.AddSingleton<IAgentHandlersProvider, AgentHandlersProvider>();
        services.AddTransient<SimpleRegex, SimpleRegex>();
        services.AddTransient<IAgentHandler, SimpleRegex>();
        services.AddTransient<IAgentHandler, SimpleRegexDivideBy100>();

        // Command Handlers
        services.AddScoped<ICommandHandler<AgentDeleteCommand>, AgentDeleteCommandHandler>();
        services.AddScoped<ICommandHandler<AgentsStoreWithOverwriteCommand>, AgentsStoreWithOverwriteCommandHandler>();
        services.AddScoped<ICommandHandler<ProductCreateCommand, Guid>, ProductCreateOrUpdateCommandHandler>();
        services.AddScoped<ICommandHandler<ProductUpdateCommand>, ProductCreateOrUpdateCommandHandler>();
        services.AddScoped<ICommandHandler<ProductDeleteCommand>, ProductDeleteCommandHandler>();
        services.AddScoped<ICommandHandler<ProductDropPricesCommand>, ProductDropPricesCommandHandler>();
        services.AddScoped<ICommandHandler<ProductEnqueueScanCommand>, ProductEnqueueScanCommandHandler>();
    }

    public static void Initialize(IServiceProvider serviceProvider)
    {
        serviceProvider.GetService<IProductPriceManager>()!.AutoRefreshInitialize();
    }
}
