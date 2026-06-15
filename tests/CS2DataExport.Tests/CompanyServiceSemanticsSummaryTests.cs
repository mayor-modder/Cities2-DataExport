using Xunit;

namespace CS2DataExport.Tests;

public sealed class CompanyServiceSemanticsSummaryTests
{
    [Fact]
    public void CollectSnapshot_IncludesCompanyServiceSemanticsGroupAndMetaStatus()
    {
        var collector = new MetricsCollector(new FakeMetricProbe());

        CitySnapshotV1 snapshot = collector.CollectSnapshot(
            exportedAtUtc: new DateTimeOffset(2026, 3, 27, 19, 0, 0, TimeSpan.Zero),
            modVersion: "1.0.0",
            gameBuild: "test-build");

        Assert.Equal(MetricStatus.Ok, snapshot.CompanyServiceSemantics.Status);
        Assert.Equal(209, snapshot.CompanyServiceSemantics.ProviderCounts.Office);
        Assert.Equal(74361, snapshot.CompanyServiceSemantics.JobsTotal.Total);
        Assert.Equal(99.98, snapshot.CompanyServiceSemantics.FillPercent.Total);
        Assert.Equal(MetricStatus.Ok, snapshot.Meta.MetricStatus["company_service_semantics"]);
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

        public CompanyServiceSemanticsSummary CollectCompanyServiceSemanticsSummary() => new()
        {
            Status = MetricStatus.Ok,
            ProviderCounts = new SectorIntSummary
            {
                Total = 1463,
                Service = 148,
                Commercial = 436,
                Leisure = 86,
                Extractor = 48,
                Industrial = 536,
                Office = 209
            },
            JobsTotal = new SectorIntSummary
            {
                Total = 74361,
                Service = 9054,
                Commercial = 14240,
                Leisure = 4268,
                Extractor = 1322,
                Industrial = 29248,
                Office = 16229
            },
            JobsFilled = new SectorIntSummary
            {
                Total = 74343,
                Service = 9048,
                Commercial = 14235,
                Leisure = 4262,
                Extractor = 1321,
                Industrial = 29248,
                Office = 16229
            },
            JobsOpen = new SectorIntSummary
            {
                Total = 18,
                Service = 6,
                Commercial = 5,
                Leisure = 6,
                Extractor = 1,
                Industrial = 0,
                Office = 0
            },
            FillPercent = new SectorDoubleSummary
            {
                Total = 99.98,
                Service = 99.93,
                Commercial = 99.96,
                Leisure = 99.86,
                Extractor = 99.92,
                Industrial = 100.0,
                Office = 100.0
            }
        };

        public HousingPressureSemanticsSummary CollectHousingPressureSemanticsSummary() => new()
        {
            Status = MetricStatus.Ok
        };

        public HouseholdPressureContextSummary CollectHouseholdPressureContextSummary() => new()
        {
            Status = MetricStatus.Ok
        };

        public LaborPressureContextSummary CollectLaborPressureContextSummary() => new()
        {
            Status = MetricStatus.Ok
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
