using Xunit;
using System;
using System.IO;
using System.Xml.Linq;

namespace CS2DataExport.Tests;

public sealed class FacilityIdentitySummaryTests
{
    [Fact]
    public void CollectSnapshot_IncludesFacilityIdentityGroupAndMetaStatus()
    {
        var collector = new MetricsCollector(new FakeMetricProbe());

        CitySnapshotV1 snapshot = collector.CollectSnapshot(
            exportedAtUtc: new DateTimeOffset(2026, 3, 27, 18, 0, 0, TimeSpan.Zero),
            modVersion: "1.0.0",
            gameBuild: "test-build");

        Assert.Equal(MetricStatus.Ok, snapshot.FacilityIdentity.Status);
        Assert.Equal(4052, snapshot.FacilityIdentity.TotalBuildingEntities);
        Assert.Equal(213, snapshot.FacilityIdentity.OfficeProviderEntities);
        Assert.Equal(MetricStatus.Ok, snapshot.Meta.MetricStatus["facility_identity"]);
    }

    [Fact]
    public void ModProject_ExcludesToolsAndTestsFromCompileGlobs()
    {
        XDocument project = XDocument.Load(Path.Combine(FindRepositoryRoot(), "CS2DataExport.csproj"));

        var compileRemoveValues = project
            .Descendants()
            .Where(element => element.Name.LocalName == "Compile")
            .Select(element => (string?)element.Attribute("Remove"))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        Assert.Contains(compileRemoveValues, value => value!.Contains("tools\\**\\*.cs", StringComparison.Ordinal));
        Assert.Contains(compileRemoveValues, value => value!.Contains("tests\\**\\*.cs", StringComparison.Ordinal));
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CS2DataExport.csproj")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate CS2DataExport.csproj from the test output directory.");
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

        public FacilityIdentitySummary CollectFacilityIdentitySummary() => new()
        {
            Status = MetricStatus.Ok,
            TotalBuildingEntities = 4052,
            OfficeProviderEntities = 213
        };

        public CompanyServiceSemanticsSummary CollectCompanyServiceSemanticsSummary() => new()
        {
            Status = MetricStatus.Ok
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
