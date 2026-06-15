using System;
using Xunit;

namespace CS2DataExport.Tests;

public sealed class TransitAccessGapCaptureCoordinatorTests
{
    [Fact]
    public void FinalizeCaptureWindow_BuildsCompletedSummary_AndClearRemovesIt()
    {
        var coordinator = new TransitAccessGapCaptureCoordinator(new TransitAccessGapAnalyzer());
        var settings = new ExportSettings
        {
            TransitTripCaptureMode = TransitTripCaptureMode.NextExportWindow,
            TransitTripCaptureClusterRadiusMeters = 192,
            TransitTripCaptureMaxSampleRoutesPerHotspot = 5
        };

        coordinator.StartCaptureWindow(new DateTimeOffset(2026, 4, 5, 18, 0, 0, TimeSpan.Zero), settings);
        coordinator.ReplaceStops(new[]
        {
            new TransitAccessGapStop(0, 0, 0, 100)
        });
        coordinator.RecordTrip(new CapturedTransitTrip
        {
            Anchors =
            {
                new TransitAccessGapAnchor(400, 0, 400)
            }
        });

        coordinator.FinalizeCaptureWindow(new DateTimeOffset(2026, 4, 5, 18, 10, 0, TimeSpan.Zero), settings);

        Assert.True(coordinator.TryGetCompletedSummary(out TransitAccessGapSemanticsSummary summary));
        Assert.Equal(MetricStatus.Ok, summary.Status);
        Assert.Single(summary.Hotspots);

        coordinator.ClearCompletedCapture();

        Assert.False(coordinator.TryGetCompletedSummary(out _));
    }

    [Fact]
    public void MarkPassengerTripCarrierUnavailable_WinsOverFinalizeUntilClear()
    {
        var coordinator = new TransitAccessGapCaptureCoordinator(new TransitAccessGapAnalyzer());
        var settings = new ExportSettings
        {
            TransitTripCaptureMode = TransitTripCaptureMode.NextExportWindow
        };

        coordinator.StartCaptureWindow(new DateTimeOffset(2026, 4, 5, 18, 0, 0, TimeSpan.Zero), settings);
        coordinator.MarkPassengerTripCarrierUnavailable("no proven passenger-trip runtime carrier");
        coordinator.RecordTrip(new CapturedTransitTrip
        {
            Anchors =
            {
                new TransitAccessGapAnchor(10, 0, 10)
            }
        });

        coordinator.FinalizeCaptureWindow(new DateTimeOffset(2026, 4, 5, 18, 10, 0, TimeSpan.Zero), settings);

        Assert.True(coordinator.TryGetCompletedSummary(out TransitAccessGapSemanticsSummary summary));
        Assert.Equal(MetricStatus.Unavailable, summary.Status);
        Assert.Contains("no proven passenger-trip runtime carrier", summary.Notes[0]);

        coordinator.ClearCompletedCapture();

        Assert.False(coordinator.TryGetCompletedSummary(out _));
    }

    [Fact]
    public void StartCaptureWindow_ClearsPassengerCarrierUnavailableLatch()
    {
        var coordinator = new TransitAccessGapCaptureCoordinator(new TransitAccessGapAnalyzer());
        var settings = new ExportSettings
        {
            TransitTripCaptureMode = TransitTripCaptureMode.NextExportWindow
        };

        coordinator.MarkPassengerTripCarrierUnavailable("no proven passenger-trip runtime carrier");
        coordinator.StartCaptureWindow(new DateTimeOffset(2026, 4, 5, 19, 0, 0, TimeSpan.Zero), settings);
        coordinator.RecordTrip(new CapturedTransitTrip
        {
            Anchors =
            {
                new TransitAccessGapAnchor(10, 0, 10)
            }
        });

        coordinator.FinalizeCaptureWindow(new DateTimeOffset(2026, 4, 5, 19, 10, 0, TimeSpan.Zero), settings);

        Assert.True(coordinator.TryGetCompletedSummary(out TransitAccessGapSemanticsSummary summary));
        Assert.Equal(MetricStatus.Ok, summary.Status);
        Assert.Single(summary.Hotspots);
    }
}
