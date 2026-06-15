using System.Text.Json;
using Xunit;

namespace CS2DataExport.Tests;

public sealed class OfficialCityStatisticsSummaryTests
{
    [Fact]
    public void CollectSnapshot_IncludesOfficialCityStatisticsAndMetaStatus()
    {
        var collector = new MetricsCollector(new FakeMetricProbe());

        CitySnapshotV1 snapshot = collector.CollectSnapshot(
            exportedAtUtc: new DateTimeOffset(2026, 5, 21, 12, 0, 0, TimeSpan.Zero),
            modVersion: "1.0.0",
            gameBuild: "test-build");

        Assert.Equal("2.7.0", snapshot.SchemaVersion);
        Assert.Equal(MetricStatus.Ok, snapshot.OfficialCityStatistics.Status);
        Assert.Equal(1400000, snapshot.OfficialCityStatistics.Finance.Money);
        Assert.Equal(128, snapshot.OfficialCityStatistics.TransportTotals.PassengerCountBus);
        Assert.Equal(88.5, snapshot.OfficialCityStatistics.Social.Wellbeing);
        Assert.Equal(MetricStatus.Ok, snapshot.Meta.MetricStatus["official_city_statistics"]);
    }

    [Fact]
    public void SerializeSnapshot_UsesSnakeCaseOfficialCityStatisticsFields()
    {
        var snapshot = new CitySnapshotV1
        {
            SchemaVersion = "2.7.0",
            OfficialCityStatistics = new OfficialCityStatisticsSummary
            {
                Status = MetricStatus.Ok,
                Finance = new OfficialFinanceStatistics
                {
                    Money = 1400000,
                    Income = 12000,
                    Expense = 9000,
                    Trade = 3000
                },
                TransportTotals = new OfficialTransportTotalsStatistics
                {
                    PassengerCountBus = 128,
                    CargoCountTrain = 42
                }
            }
        };

        string json = JsonSerializer.Serialize(snapshot);

        Assert.Contains("\"official_city_statistics\"", json);
        Assert.Contains("\"transport_totals\"", json);
        Assert.Contains("\"passenger_count_bus\":128", json);
        Assert.Contains("\"cargo_count_train\":42", json);
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
        public TransitLineDetailSemanticsSummary CollectTransitLineDetailSemanticsSummary() => new() { Status = MetricStatus.Ok };
        public TransitAccessGapSemanticsSummary CollectTransitAccessGapSemanticsSummary() => new() { Status = MetricStatus.Unavailable };

        public OfficialCityStatisticsSummary CollectOfficialCityStatisticsSummary() => new()
        {
            Status = MetricStatus.Ok,
            Time = new OfficialTimeStatistics
            {
                GameTick = 987654,
                GameYear = 2032,
                GameMonth = 5,
                GameDay = 8,
                DaysPerYear = 12,
                SampleCount = 400,
                KUpdatesPerDay = 8192,
                KTicksPerDay = 262144
            },
            Finance = new OfficialFinanceStatistics
            {
                Money = 1400000,
                Income = 12000,
                Expense = 9000,
                Trade = 3000
            },
            Social = new OfficialSocialStatistics
            {
                Wellbeing = 88.5,
                Health = 91.0,
                HomelessCount = 12,
                CrimeRate = 3,
                CrimeCount = 8
            },
            TransportTotals = new OfficialTransportTotalsStatistics
            {
                PassengerCountBus = 128,
                CargoCountTrain = 42
            }
        };
    }
}
