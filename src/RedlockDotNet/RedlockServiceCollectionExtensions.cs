using System.Diagnostics.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace RedlockDotNet
{
    /// <summary>Extensions on <see cref="IServiceCollection"/></summary>
    public static class RedlockServiceCollectionExtensions
    {
        /// <summary>Creates di builder</summary>
        [Pure]
        public static IRedlockBuilder AddRedlock(this IServiceCollection services)
        {
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