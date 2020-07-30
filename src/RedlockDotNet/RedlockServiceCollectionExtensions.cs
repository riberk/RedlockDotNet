using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RedlockDotNet
{
    /// <summary>Extensions on <see cref="IServiceCollection"/></summary>
    public static class RedlockServiceCollectionExtensions
    {
        /// <summary>Creates di builder</summary>
        public static IRedlockBuilder AddRedlock(this IServiceCollection services, Action<RedlockOptions>? configure = null)
        {
            services.AddLogging();
            services.AddOptions();
            services.TryAddSingleton<IRedlockFactory, RedlockFactory>();
            if (configure != null)
            {
                services.Configure(configure);
            }
            return new Builder(services);
        }
        
        private class Builder : IRedlockBuilder
        {
            public Builder(IServiceCollection services)
            {
                Services = services;
            }

            public IServiceCollection Services { get; }
        }
    }
}