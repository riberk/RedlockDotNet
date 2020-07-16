using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace RedLock.Redis.Tests
{
    public static class TestConfig
    {
        public static IConfiguration Instance { get; } = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
            .AddUserSecrets(typeof(TestConfig).Assembly, true)
            .AddEnvironmentVariables("REDLOCK_")
            .Build();
    }
}