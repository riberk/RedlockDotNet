using Microsoft.Extensions.DependencyInjection;

namespace RedlockDotNet.Redis
{
    /// <summary>Di builder for redlock redis implementation</summary>
    public interface IRedisRedlockBuilder
    {
        /// <summary>Di services</summary>
        IServiceCollection Services { get; }
    }
}