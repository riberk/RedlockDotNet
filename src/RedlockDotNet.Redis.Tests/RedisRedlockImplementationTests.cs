using System;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace RedlockDotNet.Redis.Tests
{
    public class RedisRedlockImplementationTests
    {
        private readonly RedisRedlockOptions _opt;
        private readonly RedisRedlockImplementation _impl;

        public RedisRedlockImplementationTests()
        {
            _opt = new RedisRedlockOptions();
            _impl = new RedisRedlockImplementation(
                new[] {new Mock<IRedlockInstance>(MockBehavior.Strict).Object},
                Options.Create(_opt)
            );
        }

        [Fact]
        public void MinValidity_NoClockDriftFactor()
        {
            _opt.ClockDriftFactor = 0;
            var minValidity = _impl.MinValidity(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1));
            Assert.Equal(TimeSpan.FromMilliseconds(8998), minValidity);
        }
        
        [Fact]
        public void MinValidity_ClockDriftFactor()
        {
            _opt.ClockDriftFactor = 0.01f;
            var minValidity = _impl.MinValidity(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1));
            Assert.Equal(TimeSpan.FromMilliseconds(8898), minValidity);
        }

        [Fact]
        public void Instances()
        {
            var i1 = new Mock<IRedlockInstance>(MockBehavior.Strict);
            var i2 = new Mock<IRedlockInstance>(MockBehavior.Strict);
            var i3 = new Mock<IRedlockInstance>(MockBehavior.Strict);
            var instances = new[]
            {
                i1.Object,
                i2.Object,
                i3.Object
            };
            var impl = new RedisRedlockImplementation(instances, Options.Create(_opt));
            Assert.Equal(3, impl.Instances.Length);
            Assert.Equal(i1.Object, impl.Instances[0]);
            Assert.Equal(i2.Object, impl.Instances[1]);
            Assert.Equal(i3.Object, impl.Instances[2]);
        }
        
        [Fact]
        public void Instances_EmptyCollectionException()
        {
            var arr = Array.Empty<IRedlockInstance>();
            Assert.Throws<ArgumentException>(() => new RedisRedlockImplementation(arr, Options.Create(_opt)));
        }
    }
}