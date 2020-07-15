using Microsoft.Extensions.DependencyInjection;

namespace RedLock.Redis
{
    internal static class RedlockRedisServiceCollectionExtensions
    {
        public static IServiceCollection AddRedisRedlock(this IServiceCollection services)
        {
            services.AddOptions();
            return services;
        }
    }
}