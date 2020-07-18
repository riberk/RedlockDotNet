using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RedlockDotNet.Repeaters;
using TestUtils;
using Xunit;

namespace RedlockDotNet
{
    public class RedlockFactoryTests
    {
        private readonly MemoryRedlockInstance[] _mem;
        private readonly TestRedlockFactory _f;

        public RedlockFactoryTests()
        {
            _mem = TestRedlockImpl.CreateInstances(3, () => new MemoryRedlockInstance());
            _f = new TestRedlockFactory(TestRedlockImpl.Create(_mem), NullLogger<RedlockFactory>.Instance);
        }

        [Theory]
        [InlineData("a", 10)]
        [InlineData("b", 10000)]
        [InlineData("bawdawdawd", 10000)]
        public void Nonce(string resource, int seconds)
        {
            var nonce = _f.PubNonce(resource, TimeSpan.FromSeconds(seconds));
            Assert.True(Guid.TryParseExact(nonce, "N", out _), $"Unable to parse '{nonce}' as guid");
        }

        [Theory]
        [InlineData("a")]
        [InlineData("b")]
        [InlineData("bawdawdawd")]
        public void DefaultTtl(string resource)
        {
            Assert.Equal(TimeSpan.FromSeconds(30), _f.DefaultTtl(resource));
        }
        
        [Theory]
        [InlineData("a", 10)]
        [InlineData("b", 10000)]
        [InlineData("bawdawdawd", 10000)]
        public void DefaultMAxWaitMs(string resource, int seconds)
        {
            Assert.Equal(200, _f.DefaultMaxWaitMsBetweenReplays(resource, TimeSpan.FromSeconds(seconds)));
        }

        [Fact]
        public void TryCreate()
        {
            var l = _f.TryCreate("r", TimeSpan.FromSeconds(10));
            Assert.NotNull(l);
            Assert.All(_mem, m => Assert.True(m.Contains("r", l!.Value.Nonce)));
        }
        
        [Fact]
        public void TryCreate_Repeater()
        {
            var l = _f.TryCreate("r", TimeSpan.FromSeconds(10), NoopRedlockRepeater.Instance, 100);
            Assert.NotNull(l);
            Assert.All(_mem, m => Assert.True(m.Contains("r", l!.Value.Nonce)));
        }
        
        [Fact]
        public void Create_Repeater()
        {
            var l = _f.Create("r", TimeSpan.FromSeconds(10), NoopRedlockRepeater.Instance, 100);
            Assert.All(_mem, m => Assert.True(m.Contains("r", l.Nonce)));
        }
        
        [Fact]
        public async Task TryCreateAsync()
        {
            var l = await _f.TryCreateAsync("r", TimeSpan.FromSeconds(10));
            Assert.NotNull(l);
            Assert.All(_mem, m => Assert.True(m.Contains("r", l!.Value.Nonce)));
        }
        
        [Fact]
        public async Task TryCreateAsync_Repeater()
        {
            var l = await _f.TryCreateAsync("r", TimeSpan.FromSeconds(10), NoopRedlockRepeater.Instance, 100);
            Assert.NotNull(l);
            Assert.All(_mem, m => Assert.True(m.Contains("r", l!.Value.Nonce)));
        }
        
        [Fact]
        public async Task CreateAsync_Repeater()
        {
            var l = await _f.CreateAsync("r", TimeSpan.FromSeconds(10), NoopRedlockRepeater.Instance, 100);
            Assert.All(_mem, m => Assert.True(m.Contains("r", l.Nonce)));
        }

        
        private class TestRedlockFactory : RedlockFactory
        {
            public TestRedlockFactory(IRedlockImplementation impl, ILogger<RedlockFactory> logger) : base(impl, logger)
            {
            }

            public string PubNonce(string resource, TimeSpan lockTimeToLive) => base.Nonce(resource, lockTimeToLive);
        }
    }
}