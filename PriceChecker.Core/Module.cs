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

            // Services
            services.AddTransient<IPersister, Persister>();
            services.AddTransient<IPriceSeeker, PriceSeeker>();
            services.AddSingleton<IProductPriceManager, ProductPriceManager>();
        }
    }
}
