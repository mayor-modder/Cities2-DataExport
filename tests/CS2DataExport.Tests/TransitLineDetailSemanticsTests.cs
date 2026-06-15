using Xunit;

namespace CS2DataExport.Tests;

public sealed class TransitLineDetailSemanticsTests
{
    [Fact]
    public void SumLeafValues_DoesNotRevisitCycles()
    {
        var graph = new Dictionary<string, string[]>
        {
            ["root"] = new[] { "loop", "leaf" },
            ["loop"] = new[] { "root" },
            ["leaf"] = Array.Empty<string>()
        };

        int total = TransitLayoutTraversal.SumLeafValues(
            "root",
            node => graph[node],
            node => node == "leaf" ? 7 : 0);

        Assert.Equal(7, total);
    }

    [Fact]
    public void SumLeafValues_CountsDistinctLeavesAcrossSharedParents()
    {
        var graph = new Dictionary<string, string[]>
        {
            ["root"] = new[] { "branchA", "branchB" },
            ["branchA"] = new[] { "leaf" },
            ["branchB"] = new[] { "leaf" },
            ["leaf"] = Array.Empty<string>()
        };

        int total = TransitLayoutTraversal.SumLeafValues(
            "root",
            node => graph[node],
            node => node == "leaf" ? 5 : 0);

        Assert.Equal(5, total);
    }

    [Fact]
    public void CalculateLineDetailMetrics_MatchesXtmPanelAggregations()
    {
        TransitLineDetailCalculationResult result = TransitLineDetailCalculator.Calculate(
            new TransitLineDetailCalculationInput
            {
                Stops = new[]
                {
                    new TransitLineStopLoad(WaitingPassengers: 15),
                    new TransitLineStopLoad(WaitingPassengers: 30),
                    new TransitLineStopLoad(WaitingPassengers: 0)
                },
                Vehicles = new[]
                {
                    new TransitLineVehicleLoad(
                        EntityIndex: 11,
                        EntityVersion: 1,
                        PassengerCount: 30,
                        Capacity: 80,
                        OdometerMeters: 9200,
                        MaintenanceRangeMeters: 10000),
                    new TransitLineVehicleLoad(
                        EntityIndex: 12,
                        EntityVersion: 1,
                        PassengerCount: 70,
                        Capacity: 160,
                        OdometerMeters: 4500,
                        MaintenanceRangeMeters: 10000)
                },
                SegmentDurations = new[] { 400.0, 600.0 },
                TicksPerDay = 262144
            });

        Assert.Equal(45, result.WaitingPassengersAllStops);
        Assert.Equal(30, result.MaxWaitingPassengersAtStop);
        Assert.Equal(100, result.OnboardPassengersInVehicles);
        Assert.Equal(240, result.TotalPassengerCapacity);
        Assert.Equal(160, result.StopCapacity);
        Assert.Equal(40.63, result.AverageVehicleOccupancyPercent);
        Assert.Equal(9.38, result.AverageStopOccupancyPercent);
        Assert.Equal(3153.59, result.ExpectedRoundTripTimeTicks);
        Assert.Equal(1039.39, result.ExpectedRoundTripTimeMinutes);
        Assert.Equal(11, result.NextMaintenanceVehicleEntityIndex);
        Assert.Equal(800, result.NextMaintenanceDistanceMeters);
    }

    [Fact]
    public void Snapshot_IncludesTransitLineDetailSemanticsInMetaStatus()
    {
        var collector = new MetricsCollector(new FakeMetricProbe());

        CitySnapshotV1 snapshot = collector.CollectSnapshot(
            exportedAtUtc: new DateTimeOffset(2026, 4, 19, 21, 30, 0, TimeSpan.Zero),
            modVersion: "1.0.0",
            gameBuild: "test-build");

        Assert.Equal(MetricStatus.Ok, snapshot.TransitLineDetailSemantics.Status);
        Assert.Equal(1, snapshot.TransitLineDetailSemantics.LinesObserved);
        Assert.Equal("Isaiah Streetcar", snapshot.TransitLineDetailSemantics.Lines[0].LineName);
        Assert.Equal(585, snapshot.TransitLineDetailSemantics.Lines[0].WaitingPassengersAllStops);
        Assert.Equal(MetricStatus.Ok, snapshot.Meta.MetricStatus["transit_line_detail_semantics"]);
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
        public TransitAccessGapSemanticsSummary CollectTransitAccessGapSemanticsSummary() => new() { Status = MetricStatus.Unavailable };

        public TransitLineDetailSemanticsSummary CollectTransitLineDetailSemanticsSummary() => new()
        {
            Status = MetricStatus.Ok,
            LinesObserved = 1,
            PassengerLinesObserved = 1,
            TotalWaitingPassengers = 585,
            TotalOnboardPassengers = 249,
            MaxWaitingPassengersAtStop = 399,
            Lines = new[]
            {
                new TransitLineDetailRecord
                {
                    LineEntityIndex = 200,
                    LineEntityVersion = 1,
                    LineName = "Isaiah Streetcar",
                    Mode = "tram",
                    StopCount = 14,
                    ActiveVehicleEntities = 6,
                    WaitingPassengersAllStops = 585,
                    OnboardPassengersInVehicles = 249,
                    MaxWaitingPassengersAtStop = 399
                }
            }
        };

        public OfficialCityStatisticsSummary CollectOfficialCityStatisticsSummary() => new()
        {
            Status = MetricStatus.Unavailable
        };
    }
}
