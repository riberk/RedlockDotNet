using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RedlockDotNet.Repeaters;
using TestUtils;
using Xunit;

namespace RedlockDotNet
{
    public class RedlockFactoryExtensionsTests
    {
        private readonly Mock<IRedlockFactory> _f;
        private readonly TimeSpan _defaultTtl;
        private readonly int _defaultMaxWait;
        private readonly TestRedlockImpl _impl;

        public RedlockFactoryExtensionsTests()
        {
            _f = new Mock<IRedlockFactory>(
                MockBehavior.Strict);
            _defaultTtl = TimeSpan.FromMinutes(10);
            _defaultMaxWait = 10;
            _impl = TestRedlockImpl.Create(TestRedlockImpl.CreateInstances(5, () => new MemoryRedlockInstance()));
        }

        [Fact]
        public void Create_AllDefaults()
        {
            var redlock = MockLock();
            _f.Setup(x => x.DefaultTtl("a")).Returns(_defaultTtl).Verifiable();
            _f.Setup(x => x.DefaultMaxWaitMsBetweenReplays("a", _defaultTtl)).Returns(_defaultMaxWait).Verifiable();
            _f.Setup(x => x.Create("a", _defaultTtl, It.IsAny<MaxRetriesRedlockRepeater>(), _defaultMaxWait))
                .Returns(redlock).Verifiable();
            Assert.Equal(redlock, _f.Object.Create("a"));
        }
        
        [Fact]
        public void Create_WithCancellation()
        {
            var redlock = MockLock();
            using var cts = new CancellationTokenSource();
            _f.Setup(x => x.DefaultTtl("a")).Returns(_defaultTtl).Verifiable();
            _f.Setup(x => x.DefaultMaxWaitMsBetweenReplays("a", _defaultTtl)).Returns(_defaultMaxWait).Verifiable();
            var expectedRepeater = new CancellationRedlockRepeater(cts.Token);
            _f.Setup(x => x.Create("a", _defaultTtl, expectedRepeater, _defaultMaxWait))
                .Returns(redlock).Verifiable();
            Assert.Equal(redlock, _f.Object.Create("a", cts.Token));
        }
        
        [Fact]
        public async Task CreateAsync_AllDefaults()
        {
            var redlock = MockLock();
            _f.Setup(x => x.DefaultTtl("a")).Returns(_defaultTtl).Verifiable();
            _f.Setup(x => x.DefaultMaxWaitMsBetweenReplays("a", _defaultTtl)).Returns(_defaultMaxWait).Verifiable();
            _f.Setup(x => x.CreateAsync("a", _defaultTtl, It.IsAny<MaxRetriesRedlockRepeater>(), _defaultMaxWait))
                .ReturnsAsync(redlock).Verifiable();
            Assert.Equal(redlock, await _f.Object.CreateAsync("a"));
        }
        
        [Fact]
        public async Task CreateAsync_WithCancellation()
        {
            var redlock = MockLock();
            using var cts = new CancellationTokenSource();
            _f.Setup(x => x.DefaultTtl("a")).Returns(_defaultTtl).Verifiable();
            _f.Setup(x => x.DefaultMaxWaitMsBetweenReplays("a", _defaultTtl)).Returns(_defaultMaxWait).Verifiable();
            var expectedRepeater = new CancellationRedlockRepeater(cts.Token);
            _f.Setup(x => x.CreateAsync("a", _defaultTtl, expectedRepeater, _defaultMaxWait))
                .ReturnsAsync(redlock).Verifiable();
            Assert.Equal(redlock, await _f.Object.CreateAsync("a", cts.Token));
        }

        private Redlock MockLock()
        {
            return new Redlock("a", "aa", _impl, NullLogger.Instance);
        }
    }
}