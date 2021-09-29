using System;
using System.Diagnostics.CodeAnalysis;
using Genius.Atom.Infrastructure.Commands;
using Genius.PriceChecker.Core.CommandHandlers;
using Genius.PriceChecker.Core.Commands;
using Genius.PriceChecker.Core.Repositories;
using Genius.PriceChecker.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Genius.PriceChecker.Core
{
    [ExcludeFromCodeCoverage]
    public static class Module
    {
        public static void Configure(IServiceCollection services)
        {
            // Repositories
            services.AddSingleton<AgentRepository>();
            services.AddSingleton<ProductRepository>();
            services.AddSingleton<IAgentRepository>(sp => sp.GetService<AgentRepository>());
            services.AddSingleton<IProductRepository>(sp => sp.GetService<ProductRepository>());
            services.AddSingleton<ISettingsRepository, SettingsRepository>();

            // Query services
            services.AddSingleton<IAgentQueryService>(sp => sp.GetService<AgentRepository>());
            services.AddSingleton<IProductQueryService>(sp => sp.GetService<ProductRepository>());

            // Services
            services.AddTransient<IPriceSeeker, PriceSeeker>();
            services.AddTransient<IProductStatusProvider, ProductStatusProvider>();
            services.AddSingleton<IProductPriceManager, ProductPriceManager>();

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
            serviceProvider.GetService<IProductPriceManager>().AutoRefreshInitialize();
        }
    }
}