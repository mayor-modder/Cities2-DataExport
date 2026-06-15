using System;
using System.IO;
using Xunit;

namespace CS2DataExport.Tests;

public sealed class DataExportSystemTransitCaptureTests
{
    [Fact]
    public void Tick_ArmsCaptureAfterFirstExport_AndFinalizesOnSecondExport()
    {
        var settings = new ExportSettings
        {
            ExportEnabled = true,
            IntervalMinutes = 10,
            OutputRootOverride = CreateTempOutputRoot(),
            TransitTripCaptureMode = TransitTripCaptureMode.NextExportWindow
        };

        var captureCoordinator = new FakeTransitAccessGapCaptureCoordinator();
        var collector = new MetricsCollector(new FakeMetricProbe());
        var system = new DataExportSystem(
            settings,
            collector,
            new SnapshotWriter(),
            "1.0.0",
            "test-build",
            captureCoordinator,
            _ => { });

        ExportTickResult first = system.Tick(new DateTimeOffset(2026, 4, 5, 18, 0, 0, TimeSpan.Zero));
        ExportTickResult second = system.Tick(new DateTimeOffset(2026, 4, 5, 18, 10, 0, TimeSpan.Zero));

        Assert.True(first.DidExport);
        Assert.Equal(1, captureCoordinator.StartCaptureCalls);
        Assert.True(second.DidExport);
        Assert.Equal(1, captureCoordinator.FinalizeCaptureCalls);
        Assert.Equal(1, captureCoordinator.ClearCompletedCaptureCalls);
    }

    [Fact]
    public void Tick_DoesNotFinalizeCaptureUntilConfiguredWindowHasElapsed()
    {
        var settings = new ExportSettings
        {
            ExportEnabled = true,
            IntervalMinutes = 10,
            OutputRootOverride = CreateTempOutputRoot(),
            TransitTripCaptureMode = TransitTripCaptureMode.NextExportWindow,
            TransitTripCaptureWindowMinutes = 25
        };

        var captureCoordinator = new FakeTransitAccessGapCaptureCoordinator();
        var collector = new MetricsCollector(new FakeMetricProbe());
        var system = new DataExportSystem(
            settings,
            collector,
            new SnapshotWriter(),
            "1.0.0",
            "test-build",
            captureCoordinator,
            _ => { });

        _ = system.Tick(new DateTimeOffset(2026, 4, 5, 18, 0, 0, TimeSpan.Zero));
        _ = system.Tick(new DateTimeOffset(2026, 4, 5, 18, 10, 0, TimeSpan.Zero));
        _ = system.Tick(new DateTimeOffset(2026, 4, 5, 18, 20, 0, TimeSpan.Zero));
        _ = system.Tick(new DateTimeOffset(2026, 4, 5, 18, 30, 0, TimeSpan.Zero));

        Assert.Equal(1, captureCoordinator.StartCaptureCalls);
        Assert.Equal(1, captureCoordinator.FinalizeCaptureCalls);
        Assert.Equal(new DateTimeOffset(2026, 4, 5, 18, 30, 0, TimeSpan.Zero), captureCoordinator.LastFinalizeAtUtc);
    }

    private static string CreateTempOutputRoot()
    {
        return Path.Combine(Path.GetTempPath(), "CS2DataExport.Tests", Guid.NewGuid().ToString("N"));
    }

    private sealed class FakeTransitAccessGapCaptureCoordinator : ITransitAccessGapCaptureCoordinator
    {
        public int StartCaptureCalls { get; private set; }
        public int FinalizeCaptureCalls { get; private set; }
        public int ClearCompletedCaptureCalls { get; private set; }
        public DateTimeOffset? LastFinalizeAtUtc { get; private set; }

        public void StartCaptureWindow(DateTimeOffset startedAtUtc, ExportSettings settings) => StartCaptureCalls++;
        public void FinalizeCaptureWindow(DateTimeOffset finalizedAtUtc, ExportSettings settings)
        {
            FinalizeCaptureCalls++;
            LastFinalizeAtUtc = finalizedAtUtc;
        }
        public void ClearCompletedCapture() => ClearCompletedCaptureCalls++;
    }

    private sealed class FakeMetricProbe : IMetricProbe
    {
        public CitySummary CollectCitySummary() => new() { Status = MetricStatus.Ok };
        public PopulationSummary CollectPopulationSummary() => new() { Status = MetricStatus.Ok };
        public EducationSummary CollectEducationSummary() => new() { Status = MetricStatus.Ok };
        public TransportProxySummary CollectTransportProxySummary() => new() { Status = MetricStatus.Ok };
        public WorkforceSummary CollectWorkforceSummary() => new() { Status = MetricStatus.Ok };
        public WorkplacesSummary CollectWorkplacesSummary() => new() { Status = MetricStatus.Ok };
        public MobilitySummary CollectMobilitySummary() => new() { Status = MetricStatus.Ok };
        public EconomySignalsSummary CollectEconomySignalsSummary() => new() { Status = MetricStatus.Ok };
        public ExternalConnectionsSummary CollectExternalConnectionsSummary() => new() { Status = MetricStatus.Ok };
        public LaborMarketDetailSummary CollectLaborMarketDetailSummary() => new() { Status = MetricStatus.Ok };
        public FacilityIdentitySummary CollectFacilityIdentitySummary() => new() { Status = MetricStatus.Ok };
        public CompanyServiceSemanticsSummary CollectCompanyServiceSemanticsSummary() => new() { Status = MetricStatus.Ok };
        public HousingPressureSemanticsSummary CollectHousingPressureSemanticsSummary() => new() { Status = MetricStatus.Ok };
        public HouseholdPressureContextSummary CollectHouseholdPressureContextSummary() => new() { Status = MetricStatus.Ok };
        public LaborPressureContextSummary CollectLaborPressureContextSummary() => new() { Status = MetricStatus.Ok };
        public TransitLineDetailSemanticsSummary CollectTransitLineDetailSemanticsSummary() => new() { Status = MetricStatus.Unavailable };
        public TransitAccessGapSemanticsSummary CollectTransitAccessGapSemanticsSummary() => new() { Status = MetricStatus.Unavailable };
        public OfficialCityStatisticsSummary CollectOfficialCityStatisticsSummary() => new() { Status = MetricStatus.Unavailable };
    }
}
