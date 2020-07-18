using System;
using Xunit;

namespace RedlockDotNet
{
    public static class RedlockExceptionTests
    {
        [Fact]
        public static void Ctor()
        {
            var _ = new RedlockException();
        }

        [Fact]
        public static void CtorWithMsg()
        {
            var withInner = new RedlockException("s");
            Assert.Equal("s", withInner.Message);
        }
        
        [Fact]
        public static void CtorWithInner()
        {
            var inner = new Exception();
            var withInner = new RedlockException("s", inner);
            Assert.Equal("s", withInner.Message);
            Assert.Equal(inner, withInner.InnerException);
        }
    }
}