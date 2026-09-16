namespace Genius.PriceChecker.Core.Services;

/// <summary>
///   Awaits wall-clock delays. Abstracted away from <see cref="Task.Delay(TimeSpan, CancellationToken)"/>
///   so that the pacing of a scanning session can be asserted in tests without waiting for it.
/// </summary>
public interface IDelayService
{
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}

internal sealed class DelayService : IDelayService
{
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        => Task.Delay(delay, cancellationToken);
}
