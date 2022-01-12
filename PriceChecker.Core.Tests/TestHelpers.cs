using System;
using Microsoft.Extensions.Logging;
using Moq;

namespace Genius.PriceChecker.Core.Tests;

public static class TestHelpers
{
    private static readonly Random _random = new();

    public static T TakeRandom<T>(this ICollection<T> source)
    {
        var index = _random.Next(0, source.Count);
        return source.ElementAt(index);
    }

    public static ICollection<T> RandomizeOrder<T>(this IEnumerable<T> source)
    {
        return source.OrderBy(_ => _random.Next()).ToArray();
    }

    public static void VerifyLogger<T>(Mock<ILogger<T>> loggerMock, LogLevel logLevel, Times? times = null)
    {
        if (times == null)
            times = Times.Once();
        loggerMock.Verify(x => x.Log(logLevel,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(),
            (Func<It.IsAnyType, Exception?, string>) It.IsAny<object>()), times.Value);
    }
}
