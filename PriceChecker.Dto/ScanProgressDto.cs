namespace Genius.PriceChecker.Dto;

/// <summary>
///   The overall progress of the currently running price scan,
///   pushed to the clients over the scan hub.
/// </summary>
public sealed record ScanProgressDto(
    int Finished,
    int Total,
    bool HasErrors,
    bool HasNewLowestPrice,
    bool IsFinished);
