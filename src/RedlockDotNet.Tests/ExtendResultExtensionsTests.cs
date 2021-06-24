using System;
using Xunit;

namespace RedlockDotNet
{
    public class ExtendResultExtensionsTests
    {
        [Theory]
        [InlineData(ExtendResult.Extend, false)]
        [InlineData(ExtendResult.IllegalReacquire, true)]
        [InlineData(ExtendResult.AlreadyAcquiredByAnotherOwner, true)]
        public void IsFail(ExtendResult result, bool expectedIsFail)
        {
            Assert.Equal(expectedIsFail, result.IsFail());
        }
        
        [Theory]
        [InlineData(ExtendResult.Extend, true)]
        [InlineData(ExtendResult.IllegalReacquire, false)]
        [InlineData(ExtendResult.AlreadyAcquiredByAnotherOwner, false)]
        public void IsSuccess(ExtendResult result, bool expectedIsSuccess)
        {
            Assert.Equal(expectedIsSuccess, result.IsSuccess());
        }
        
        [Fact]
        public void IsFail_UndefinedResult()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ((ExtendResult) 999).IsFail());
        }
        
        
        [Fact]
        public void IsSuccess_UndefinedResult()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ((ExtendResult) 999).IsSuccess());
        }
    }
}