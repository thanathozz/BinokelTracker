using BinokelTracker.Models;
using BinokelTracker.Services;

namespace BinokelTracker.Web.Services;

public class MeldScanService : IMeldScanService
{
    public bool IsAvailable => false;

    public Task<MeldScanResult> ScanHandAsync(CancellationToken ct = default) =>
        Task.FromResult(new MeldScanResult(false, new List<DetectedMeld>(), null, 0, "Kamera nicht verfügbar im Web"));
}
