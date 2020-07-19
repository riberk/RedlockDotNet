using Microsoft.Extensions.DependencyInjection;

namespace RedlockDotNet
{
    /// <summary>Di builder for redlock implementation</summary>
    public interface IRedlockBuilder
    {
        /// <summary>Di services</summary>
        IServiceCollection Services { get; }
    }
}