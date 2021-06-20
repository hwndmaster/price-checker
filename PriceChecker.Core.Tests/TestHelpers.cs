using System;
using Microsoft.Extensions.Logging;
using Moq;

namespace Genius.PriceChecker.Core.Tests
{
    public static class TestHelpers
    {
        public static void VerifyLogger<T>(Mock<ILogger<T>> loggerMock, LogLevel logLevel, Times? times = null)
        {
            if (times == null)
                times = Times.Once();
            loggerMock.Verify(x => x.Log(logLevel,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>) It.IsAny<object>()), times.Value);
        }
    }
}