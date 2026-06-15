using System;
using System.IO;

namespace CS2DataExport;

public enum TransitTripCaptureMode
{
    Off = 0,
    NextExportWindow = 1
}

public sealed class ExportSettings
{
    public const bool DefaultExportEnabled = true;
    public const int DefaultIntervalMinutes = 10;
    public const int DefaultRetentionCount = 500;

    public bool ExportEnabled { get; set; } = DefaultExportEnabled;
    public int IntervalMinutes { get; set; } = DefaultIntervalMinutes;
    public int RetentionCount { get; set; } = DefaultRetentionCount;
    public string OutputRootOverride { get; set; } = string.Empty;
    public TransitTripCaptureMode TransitTripCaptureMode { get; set; } = TransitTripCaptureMode.Off;
    public int TransitTripCaptureWindowMinutes { get; set; } = DefaultIntervalMinutes;
    public bool TransitTripCaptureIncludeOutsideTrips { get; set; }
    public int TransitTripCaptureClusterRadiusMeters { get; set; } = 192;
    public int TransitTripCaptureMaxSampleRoutesPerHotspot { get; set; } = 5;
    public int TransitTripCaptureMaxHotspots { get; set; } = 50;

    public int EffectiveIntervalMinutes => ClampInt(IntervalMinutes, 1, 720);

    public int EffectiveRetentionCount => ClampInt(RetentionCount, 1, 5000);

    public int EffectiveTransitTripCaptureWindowMinutes => ClampInt(TransitTripCaptureWindowMinutes, 1, 720);

    public int EffectiveTransitTripCaptureClusterRadiusMeters => ClampInt(TransitTripCaptureClusterRadiusMeters, 96, 512);

    public int EffectiveTransitTripCaptureMaxSampleRoutesPerHotspot => ClampInt(TransitTripCaptureMaxSampleRoutesPerHotspot, 1, 20);

    public int EffectiveTransitTripCaptureMaxHotspots => ClampInt(TransitTripCaptureMaxHotspots, 1, 200);

    public string ResolveOutputRoot()
    {
        if (!string.IsNullOrWhiteSpace(OutputRootOverride))
        {
            return Path.GetFullPath(OutputRootOverride);
        }

        // Windows target: %USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\ModsData\CS2DataExport
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var localLow = Path.GetFullPath(Path.Combine(localAppData, "..", "LocalLow"));

        return Path.Combine(
            localLow,
            "Colossal Order",
            "Cities Skylines II",
            "ModsData",
            "CS2DataExport");
    }

    public string ResolveSnapshotsDirectory()
    {
        return Path.Combine(ResolveOutputRoot(), "snapshots");
    }

    public string ResolveLatestFilePath()
    {
        return Path.Combine(ResolveOutputRoot(), "latest.json");
    }

    private static int ClampInt(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}
