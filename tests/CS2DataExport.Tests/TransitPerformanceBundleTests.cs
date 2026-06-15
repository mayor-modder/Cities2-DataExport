using Xunit;

namespace CS2DataExport.Tests;

public sealed class TransitPerformanceBundleTests
{
    [Fact]
    public void TransitPerformanceBundle_IsDerivedFromMobilityLines_AndIncludedInMetaStatus()
    {
        var collector = new MetricsCollector(new FakeMetricProbe());

        CitySnapshotV1 snapshot = collector.CollectSnapshot(
            exportedAtUtc: new DateTimeOffset(2026, 3, 28, 16, 0, 0, TimeSpan.Zero),
            modVersion: "1.0.0",
            gameBuild: "test-build");

        Assert.Equal(MetricStatus.Ok, snapshot.TransitPerformanceSemantics.Status);

        Assert.Equal(1, snapshot.TransitPerformanceSemantics.LinePressure.HighPressureLines);
        Assert.Equal("Harbor Express", snapshot.TransitPerformanceSemantics.LinePressure.TopPressureLines[0].LineName);

        Assert.Equal(1, snapshot.TransitPerformanceSemantics.ModePressure.HighPressureLinesByMode.Bus);
        Assert.Equal(82.5, snapshot.TransitPerformanceSemantics.ModePressure.AverageUsagePercentByMode.Bus);

        Assert.Equal(1, snapshot.TransitPerformanceSemantics.ServiceGaps.NoServiceLines);
        Assert.Equal(1, snapshot.TransitPerformanceSemantics.ServiceGaps.MissingUsageObservedLines);

        Assert.Equal(MetricStatus.Ok, snapshot.Meta.MetricStatus["transit_performance_semantics"]);
    }

    private sealed class FakeMetricProbe : IMetricProbe
    {
        public CitySummary CollectCitySummary() => new() { Status = MetricStatus.Ok };
        public PopulationSummary CollectPopulationSummary() => new() { Status = MetricStatus.Ok };
        public EducationSummary CollectEducationSummary() => new() { Status = MetricStatus.Ok };
        public TransportProxySummary CollectTransportProxySummary() => new() { Status = MetricStatus.Ok };
        public WorkforceSummary CollectWorkforceSummary() => new() { Status = MetricStatus.Ok };
        public WorkplacesSummary CollectWorkplacesSummary() => new() { Status = MetricStatus.Ok };
        public EconomySignalsSummary CollectEconomySignalsSummary() => new() { Status = MetricStatus.Ok };
        public ExternalConnectionsSummary CollectExternalConnectionsSummary() => new() { Status = MetricStatus.Ok };
        public LaborMarketDetailSummary CollectLaborMarketDetailSummary() => new() { Status = MetricStatus.Ok };
        public FacilityIdentitySummary CollectFacilityIdentitySummary() => new() { Status = MetricStatus.Ok };
        public CompanyServiceSemanticsSummary CollectCompanyServiceSemanticsSummary() => new() { Status = MetricStatus.Ok };
        public HousingPressureSemanticsSummary CollectHousingPressureSemanticsSummary() => new() { Status = MetricStatus.Ok };
        public HouseholdPressureContextSummary CollectHouseholdPressureContextSummary() => new() { Status = MetricStatus.Ok };
        public LaborPressureContextSummary CollectLaborPressureContextSummary() => new() { Status = MetricStatus.Ok };

        public TransitAccessGapSemanticsSummary CollectTransitAccessGapSemanticsSummary() => new()
        {
            Status = MetricStatus.Unavailable
        };

        public TransitLineDetailSemanticsSummary CollectTransitLineDetailSemanticsSummary() => new()
        {
            Status = MetricStatus.Unavailable
        };

        public OfficialCityStatisticsSummary CollectOfficialCityStatisticsSummary() => new()
        {
            Status = MetricStatus.Unavailable
        };

        public MobilitySummary CollectMobilitySummary() => new()
        {
            Status = MetricStatus.Ok,
            Lines = new[]
            {
                new MobilityLineRecord
                {
                    LineEntityIndex = 101,
                    LineEntityVersion = 1,
                    LineName = "Harbor Express",
                    Mode = "bus",
                    Active = true,
                    Stops = 8,
                    ActiveVehicleEntities = 3,
                    OnboardPassengerEntities = 66,
                    TotalPassengerCapacity = 80,
                    UsagePercent = 82.5,
                    LengthM = 5120.5
                },
                new MobilityLineRecord
                {
                    LineEntityIndex = 102,
                    LineEntityVersion = 1,
                    LineName = "Downtown Local",
                    Mode = "tram",
                    Active = false,
                    Stops = 10,
                    ActiveVehicleEntities = 0,
                    OnboardPassengerEntities = 0,
                    TotalPassengerCapacity = 240,
                    UsagePercent = 0,
                    LengthM = 4200.0
                },
                new MobilityLineRecord
                {
                    LineEntityIndex = 103,
                    LineEntityVersion = 1,
                    LineName = "Water Shuttle",
                    Mode = "ship",
                    Active = true,
                    Stops = 2,
                    ActiveVehicleEntities = 1,
                    OnboardPassengerEntities = 12,
                    TotalPassengerCapacity = null,
                    UsagePercent = null,
                    LengthM = 18200.0
                },
                new MobilityLineRecord
                {
                    LineEntityIndex = 104,
                    LineEntityVersion = 1,
                    LineName = "Crosstown",
                    Mode = "subway",
                    Active = true,
                    Stops = 12,
                    ActiveVehicleEntities = 2,
                    OnboardPassengerEntities = 64,
                    TotalPassengerCapacity = 160,
                    UsagePercent = 40.0,
                    LengthM = 6900.0
                }
            }
        };
    }
}
