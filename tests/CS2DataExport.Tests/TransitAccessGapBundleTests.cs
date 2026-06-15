using System;
using Xunit;

namespace CS2DataExport.Tests;

public sealed class TransitAccessGapBundleTests
{
    [Fact]
    public void CollectSnapshot_IncludesTransitAccessGapGroup_AndMetaStatus()
    {
        var collector = new MetricsCollector(new FakeMetricProbe());

        CitySnapshotV1 snapshot = collector.CollectSnapshot(
            exportedAtUtc: new DateTimeOffset(2026, 4, 5, 18, 0, 0, TimeSpan.Zero),
            modVersion: "1.0.0",
            gameBuild: "test-build");

        Assert.Equal("2.7.0", snapshot.SchemaVersion);
        Assert.Equal(MetricStatus.Ok, snapshot.TransitAccessGapSemantics.Status);
        Assert.Equal(2, snapshot.TransitAccessGapSemantics.Hotspots.Length);
        Assert.Equal("next_export_window", snapshot.TransitAccessGapSemantics.CaptureContext.CaptureMode);
        Assert.Equal(MetricTimeBasis.CaptureWindow, snapshot.TransitAccessGapSemantics.MetricMetadata["hotspots"].TimeBasis);
        Assert.Equal(MetricStatus.Ok, snapshot.Meta.MetricStatus["transit_access_gap_semantics"]);
    }

    [Fact]
    public void TransitAccessGapRouteSegment_UsesPathTargetFields_AndSupportsUnknownDirection()
    {
        var segment = new TransitAccessGapRouteSegment
        {
            PathTargetEntityIndex = 101,
            PathTargetEntityVersion = 2,
            IsForward = null
        };

        Assert.Equal(101, segment.PathTargetEntityIndex);
        Assert.Equal(2, segment.PathTargetEntityVersion);
        Assert.Null(segment.IsForward);
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

        public TransitLineDetailSemanticsSummary CollectTransitLineDetailSemanticsSummary() => new()
        {
            Status = MetricStatus.Unavailable
        };

        public TransitAccessGapSemanticsSummary CollectTransitAccessGapSemanticsSummary() => new()
        {
            Status = MetricStatus.Ok,
            CaptureContext = new TransitAccessGapCaptureContext
            {
                CaptureMode = "next_export_window"
            },
            Hotspots = new[]
            {
                new TransitAccessGapHotspot { HotspotId = "hotspot_1" },
                new TransitAccessGapHotspot { HotspotId = "hotspot_2" }
            }
        };

        public OfficialCityStatisticsSummary CollectOfficialCityStatisticsSummary() => new()
        {
            Status = MetricStatus.Unavailable
        };
    }
}
