using System.Diagnostics.CodeAnalysis;
using Genius.PriceChecker.Infrastructure.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Genius.PriceChecker.Infrastructure
{
    [ExcludeFromCodeCoverage]
    public static class Module
    {
        public static void Configure(IServiceCollection services)
        {
            services.AddSingleton<IEventBus, EventBus>();
        }
    }
}
