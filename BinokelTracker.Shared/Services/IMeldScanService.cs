using BinokelTracker.Models;

namespace BinokelTracker.Services;

public interface IMeldScanService
{
    bool IsAvailable { get; }
    Task<MeldScanResult> ScanHandAsync(CancellationToken ct = default);
}

public record MeldScanResult(
    bool Success,
    List<DetectedMeld> Combinations,
    TrumpSuit? DetectedTrump,
    int TotalPoints,
    string? Error,
    string? RawResponse = null);

public record DetectedMeld(string Name, int Points);
