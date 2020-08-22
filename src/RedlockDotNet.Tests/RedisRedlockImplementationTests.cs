using System;
using Moq;
using Xunit;

namespace RedlockDotNet
{
    public class RedisRedlockImplementationTests
    {
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
            var impl = new RedlockImplementation(instances);
            Assert.Equal(3, impl.Instances.Length);
            Assert.Equal(i1.Object, impl.Instances[0]);
            Assert.Equal(i2.Object, impl.Instances[1]);
            Assert.Equal(i3.Object, impl.Instances[2]);
        }
        
        [Fact]
        public void Instances_EmptyCollectionException()
        {
            var arr = Array.Empty<IRedlockInstance>();
            Assert.Throws<ArgumentException>(() => new RedlockImplementation(arr));
        }
    }
}