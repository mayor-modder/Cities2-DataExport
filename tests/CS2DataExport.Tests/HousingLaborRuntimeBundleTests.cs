using Xunit;

namespace CS2DataExport.Tests;

public sealed class HousingLaborRuntimeBundleTests
{
    [Fact]
    public void CollectSnapshot_IncludesHousingLaborRuntimeBundleGroupsAndMetaStatus()
    {
        var collector = new MetricsCollector(new FakeMetricProbe());

        CitySnapshotV1 snapshot = collector.CollectSnapshot(
            exportedAtUtc: new DateTimeOffset(2026, 3, 27, 21, 0, 0, TimeSpan.Zero),
            modVersion: "1.0.0",
            gameBuild: "test-build");

        Assert.Equal(MetricStatus.Ok, snapshot.HousingPressureSemantics.Status);
        Assert.Equal(55.92, snapshot.HousingPressureSemantics.HouseholdsPerResidentialBuilding);
        Assert.Equal(742, snapshot.HouseholdPressureContext.HomelessHouseholds);
        Assert.Equal(9.48, snapshot.LaborPressureContext.OutsideWorkerSharePercent);
        Assert.Equal(MetricStatus.Ok, snapshot.Meta.MetricStatus["housing_pressure_semantics"]);
        Assert.Equal(MetricStatus.Ok, snapshot.Meta.MetricStatus["household_pressure_context"]);
        Assert.Equal(MetricStatus.Ok, snapshot.Meta.MetricStatus["labor_pressure_context"]);
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

        public HousingPressureSemanticsSummary CollectHousingPressureSemanticsSummary() => new()
        {
            Status = MetricStatus.Ok,
            HouseholdsPerResidentialBuilding = 55.92
        };

        public HouseholdPressureContextSummary CollectHouseholdPressureContextSummary() => new()
        {
            Status = MetricStatus.Ok,
            HomelessHouseholds = 742
        };

        public LaborPressureContextSummary CollectLaborPressureContextSummary() => new()
        {
            Status = MetricStatus.Ok,
            OutsideWorkerSharePercent = 9.48
        };

        public TransitLineDetailSemanticsSummary CollectTransitLineDetailSemanticsSummary() => new()
        {
            Status = MetricStatus.Unavailable
        };

        public TransitAccessGapSemanticsSummary CollectTransitAccessGapSemanticsSummary() => new()
        {
            Status = MetricStatus.Unavailable
        };

        public OfficialCityStatisticsSummary CollectOfficialCityStatisticsSummary() => new()
        {
            Status = MetricStatus.Unavailable
        };
    }
}
