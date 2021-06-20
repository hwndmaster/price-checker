using System;
using System.Diagnostics.CodeAnalysis;
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

            // Services
            services.AddSingleton<IIoService, IoService>();
            services.AddTransient<IPersister, Persister>();
            services.AddTransient<IPriceSeeker, PriceSeeker>();
            services.AddTransient<IProductStatusProvider, ProductStatusProvider>();
            services.AddSingleton<IProductPriceManager, ProductPriceManager>();
            services.AddSingleton<ITrickyHttpClient, TrickyHttpClient>();
        }

        public static void Initialize(IServiceProvider serviceProvider)
        {
            serviceProvider.GetService<IProductPriceManager>().AutoRefreshInitialize();
        }
    }
}