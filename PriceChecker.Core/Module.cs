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
            services.AddSingleton<IAgentRepository, AgentRepository>();
            services.AddSingleton<IProductRepository, ProductRepository>();
            services.AddSingleton<ISettingsRepository, SettingsRepository>();

            // Query services
            services.AddSingleton<IAgentQueryService, AgentRepository>();
            services.AddSingleton<IProductQueryService, ProductRepository>();

            // Services
            services.AddTransient<IPriceSeeker, PriceSeeker>();
            services.AddTransient<IProductStatusProvider, ProductStatusProvider>();
            services.AddSingleton<IProductPriceManager, ProductPriceManager>();

            // Command Handlers
            services.AddScoped<ICommandHandler<AgentDeleteCommand>, AgentDeleteCommandHandler>();
            services.AddScoped<ICommandHandler<AgentsStoreWithOverwriteCommand>, AgentsStoreWithOverwriteCommandHandler>();
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