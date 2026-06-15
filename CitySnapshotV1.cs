using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CS2DataExport;

public static class MetricStatus
{
    public const string Ok = "ok";
    public const string Partial = "partial";
    public const string Unavailable = "unavailable";

    public static string Normalize(string value)
    {
        return value switch
        {
            Ok => Ok,
            Partial => Partial,
            _ => Unavailable
        };
    }
}

public static class MetricMeasurementKind
{
    public const string Observed = "observed";
    public const string Derived = "derived";
    public const string Proxy = "proxy";
}

public static class MetricTimeBasis
{
    public const string Instant = "instant";
    public const string Monthly = "monthly";
    public const string Rolling = "rolling";
    public const string CaptureWindow = "capture_window";
}

public sealed class CitySnapshotV1
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "2.7.0";

    [JsonPropertyName("exported_at_utc")]
    public string ExportedAtUtc { get; init; } = string.Empty;

    [JsonPropertyName("game_build")]
    public string? GameBuild { get; init; }

    [JsonPropertyName("mod_version")]
    public string ModVersion { get; init; } = string.Empty;

    [JsonPropertyName("city")]
    public CitySummary City { get; init; } = new();

    [JsonPropertyName("population")]
    public PopulationSummary Population { get; init; } = new();

    [JsonPropertyName("education")]
    public EducationSummary Education { get; init; } = new();

    [JsonPropertyName("transport_proxies")]
    public TransportProxySummary TransportProxies { get; init; } = new();

    [JsonPropertyName("workforce")]
    public WorkforceSummary Workforce { get; init; } = new();

    [JsonPropertyName("workplaces")]
    public WorkplacesSummary Workplaces { get; init; } = new();

    [JsonPropertyName("mobility")]
    public MobilitySummary Mobility { get; init; } = new();

    [JsonPropertyName("economy_signals")]
    public EconomySignalsSummary EconomySignals { get; init; } = new();

    [JsonPropertyName("external_connections")]
    public ExternalConnectionsSummary ExternalConnections { get; init; } = new();

    [JsonPropertyName("labor_market_detail")]
    public LaborMarketDetailSummary LaborMarketDetail { get; init; } = new();

    [JsonPropertyName("facility_identity")]
    public FacilityIdentitySummary FacilityIdentity { get; init; } = new();

    [JsonPropertyName("company_service_semantics")]
    public CompanyServiceSemanticsSummary CompanyServiceSemantics { get; init; } = new();

    [JsonPropertyName("housing_pressure_semantics")]
    public HousingPressureSemanticsSummary HousingPressureSemantics { get; init; } = new();

    [JsonPropertyName("household_pressure_context")]
    public HouseholdPressureContextSummary HouseholdPressureContext { get; init; } = new();

    [JsonPropertyName("labor_pressure_context")]
    public LaborPressureContextSummary LaborPressureContext { get; init; } = new();

    [JsonPropertyName("transit_performance_semantics")]
    public TransitPerformanceSemanticsSummary TransitPerformanceSemantics { get; init; } = new();

    [JsonPropertyName("transit_line_detail_semantics")]
    public TransitLineDetailSemanticsSummary TransitLineDetailSemantics { get; init; } = new();

    [JsonPropertyName("transit_access_gap_semantics")]
    public TransitAccessGapSemanticsSummary TransitAccessGapSemantics { get; init; } = new();

    [JsonPropertyName("official_city_statistics")]
    public OfficialCityStatisticsSummary OfficialCityStatistics { get; init; } = new();

    [JsonPropertyName("meta")]
    public SnapshotMeta Meta { get; init; } = new();
}

public abstract class MetricGroup
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = MetricStatus.Unavailable;

    [JsonPropertyName("notes")]
    public string[] Notes { get; init; } = Array.Empty<string>();
}

public sealed class CitySummary : MetricGroup
{
    [JsonPropertyName("city_name")]
    public string? CityName { get; init; }

    [JsonPropertyName("district_count")]
    public int? DistrictCount { get; init; }

    [JsonPropertyName("building_count")]
    public int? BuildingCount { get; init; }

    [JsonPropertyName("source_component")]
    public string SourceComponent { get; init; } = "ecs.city";
}

public sealed class PopulationSummary : MetricGroup
{
    [JsonPropertyName("total_population")]
    public int? TotalPopulation { get; init; }

    [JsonPropertyName("household_count")]
    public int? HouseholdCount { get; init; }

    [JsonPropertyName("birth_rate_per_month")]
    public double? BirthRatePerMonth { get; init; }

    [JsonPropertyName("death_rate_per_month")]
    public double? DeathRatePerMonth { get; init; }

    [JsonPropertyName("local_population")]
    public int? LocalPopulation { get; init; }

    [JsonPropertyName("tourist_population")]
    public int? TouristPopulation { get; init; }

    [JsonPropertyName("commuter_population")]
    public int? CommuterPopulation { get; init; }

    [JsonPropertyName("moving_away_population")]
    public int? MovingAwayPopulation { get; init; }

    [JsonPropertyName("homeless_population")]
    public int? HomelessPopulation { get; init; }

    [JsonPropertyName("working_age_population")]
    public int? WorkingAgePopulation { get; init; }

    [JsonPropertyName("children_population")]
    public int? ChildrenPopulation { get; init; }

    [JsonPropertyName("elderly_population")]
    public int? ElderlyPopulation { get; init; }

    [JsonPropertyName("source_component")]
    public string SourceComponent { get; init; } = "ecs.population";
}

public sealed class OfficialCityStatisticsSummary : MetricGroup
{
    [JsonPropertyName("time")]
    public OfficialTimeStatistics Time { get; init; } = new();

    [JsonPropertyName("finance")]
    public OfficialFinanceStatistics Finance { get; init; } = new();

    [JsonPropertyName("taxes")]
    public OfficialTaxStatistics Taxes { get; init; } = new();

    [JsonPropertyName("population_flow")]
    public OfficialPopulationFlowStatistics PopulationFlow { get; init; } = new();

    [JsonPropertyName("social")]
    public OfficialSocialStatistics Social { get; init; } = new();

    [JsonPropertyName("tourism")]
    public OfficialTourismStatistics Tourism { get; init; } = new();

    [JsonPropertyName("transport_totals")]
    public OfficialTransportTotalsStatistics TransportTotals { get; init; } = new();

    [JsonPropertyName("sectors")]
    public OfficialSectorStatistics Sectors { get; init; } = new();

    [JsonPropertyName("city_services")]
    public OfficialCityServiceStatistics CityServices { get; init; } = new();

    [JsonPropertyName("source_component")]
    public string SourceComponent { get; init; } = "official.city_statistics";

    [JsonPropertyName("metric_metadata")]
    public SortedDictionary<string, MetricDefinition> MetricMetadata { get; init; } =
        MetricMetadataDefaults.OfficialCityStatistics();
}

public sealed class OfficialTimeStatistics
{
    [JsonPropertyName("game_tick")]
    public ulong? GameTick { get; init; }

    [JsonPropertyName("game_year")]
    public int? GameYear { get; init; }

    [JsonPropertyName("game_month")]
    public int? GameMonth { get; init; }

    [JsonPropertyName("game_day")]
    public int? GameDay { get; init; }

    [JsonPropertyName("days_per_year")]
    public int? DaysPerYear { get; init; }

    [JsonPropertyName("sample_count")]
    public int? SampleCount { get; init; }

    [JsonPropertyName("k_updates_per_day")]
    public int? KUpdatesPerDay { get; init; }

    [JsonPropertyName("k_ticks_per_day")]
    public int? KTicksPerDay { get; init; }
}

public sealed class OfficialFinanceStatistics
{
    [JsonPropertyName("money")]
    public int? Money { get; init; }

    [JsonPropertyName("income")]
    public int? Income { get; init; }

    [JsonPropertyName("expense")]
    public int? Expense { get; init; }

    [JsonPropertyName("trade")]
    public int? Trade { get; init; }
}

public sealed class OfficialTaxStatistics
{
    [JsonPropertyName("residential_taxable_income")]
    public int? ResidentialTaxableIncome { get; init; }

    [JsonPropertyName("commercial_taxable_income")]
    public int? CommercialTaxableIncome { get; init; }

    [JsonPropertyName("industrial_taxable_income")]
    public int? IndustrialTaxableIncome { get; init; }

    [JsonPropertyName("office_taxable_income")]
    public int? OfficeTaxableIncome { get; init; }
}

public sealed class OfficialPopulationFlowStatistics
{
    [JsonPropertyName("population")]
    public int? Population { get; init; }

    [JsonPropertyName("population_with_move_in")]
    public int? PopulationWithMoveIn { get; init; }

    [JsonPropertyName("citizens_moved_in")]
    public int? CitizensMovedIn { get; init; }

    [JsonPropertyName("citizens_moved_away")]
    public int? CitizensMovedAway { get; init; }

    [JsonPropertyName("birth_rate")]
    public int? BirthRate { get; init; }

    [JsonPropertyName("death_rate")]
    public int? DeathRate { get; init; }
}

public sealed class OfficialSocialStatistics
{
    [JsonPropertyName("wellbeing")]
    public double? Wellbeing { get; init; }

    [JsonPropertyName("health")]
    public double? Health { get; init; }

    [JsonPropertyName("wellbeing_level")]
    public int? WellbeingLevel { get; init; }

    [JsonPropertyName("health_level")]
    public int? HealthLevel { get; init; }

    [JsonPropertyName("homeless_count")]
    public int? HomelessCount { get; init; }

    [JsonPropertyName("crime_rate")]
    public int? CrimeRate { get; init; }

    [JsonPropertyName("crime_count")]
    public int? CrimeCount { get; init; }

    [JsonPropertyName("escaped_arrest_count")]
    public int? EscapedArrestCount { get; init; }

    [JsonPropertyName("collected_mail")]
    public int? CollectedMail { get; init; }

    [JsonPropertyName("delivered_mail")]
    public int? DeliveredMail { get; init; }
}

public sealed class OfficialTourismStatistics
{
    [JsonPropertyName("tourist_count")]
    public int? TouristCount { get; init; }

    [JsonPropertyName("tourist_income")]
    public int? TouristIncome { get; init; }

    [JsonPropertyName("lodging_used")]
    public int? LodgingUsed { get; init; }

    [JsonPropertyName("lodging_total")]
    public int? LodgingTotal { get; init; }

    [JsonPropertyName("current_tourists")]
    public int? CurrentTourists { get; init; }

    [JsonPropertyName("average_tourists")]
    public int? AverageTourists { get; init; }

    [JsonPropertyName("attractiveness")]
    public int? Attractiveness { get; init; }
}

public sealed class OfficialTransportTotalsStatistics
{
    [JsonPropertyName("passenger_count_bus")]
    public int? PassengerCountBus { get; init; }

    [JsonPropertyName("passenger_count_subway")]
    public int? PassengerCountSubway { get; init; }

    [JsonPropertyName("passenger_count_train")]
    public int? PassengerCountTrain { get; init; }

    [JsonPropertyName("passenger_count_tram")]
    public int? PassengerCountTram { get; init; }

    [JsonPropertyName("passenger_count_airplane")]
    public int? PassengerCountAirplane { get; init; }

    [JsonPropertyName("passenger_count_taxi")]
    public int? PassengerCountTaxi { get; init; }

    [JsonPropertyName("passenger_count_ship")]
    public int? PassengerCountShip { get; init; }

    [JsonPropertyName("cargo_count_truck")]
    public int? CargoCountTruck { get; init; }

    [JsonPropertyName("cargo_count_train")]
    public int? CargoCountTrain { get; init; }

    [JsonPropertyName("cargo_count_ship")]
    public int? CargoCountShip { get; init; }

    [JsonPropertyName("cargo_count_airplane")]
    public int? CargoCountAirplane { get; init; }
}

public sealed class OfficialSectorStatistics
{
    [JsonPropertyName("service")]
    public OfficialSectorMetric Service { get; init; } = new();

    [JsonPropertyName("processing")]
    public OfficialSectorMetric Processing { get; init; } = new();

    [JsonPropertyName("office")]
    public OfficialSectorMetric Office { get; init; } = new();
}

public sealed class OfficialSectorMetric
{
    [JsonPropertyName("wealth")]
    public int? Wealth { get; init; }

    [JsonPropertyName("count")]
    public int? Count { get; init; }

    [JsonPropertyName("workers")]
    public int? Workers { get; init; }

    [JsonPropertyName("max_workers")]
    public int? MaxWorkers { get; init; }
}

public sealed class OfficialCityServiceStatistics
{
    [JsonPropertyName("city_service_workers")]
    public int? CityServiceWorkers { get; init; }

    [JsonPropertyName("city_service_max_workers")]
    public int? CityServiceMaxWorkers { get; init; }

    [JsonPropertyName("senior_worker_in_demand_percentage")]
    public int? SeniorWorkerInDemandPercentage { get; init; }

    [JsonPropertyName("dev_tree_points")]
    public int? DevTreePoints { get; init; }
}

public sealed class EducationSummary : MetricGroup
{
    [JsonPropertyName("educated_percent")]
    public double? EducatedPercent { get; init; }

    [JsonPropertyName("highly_educated_percent")]
    public double? HighlyEducatedPercent { get; init; }

    [JsonPropertyName("employment_rate_percent")]
    public double? EmploymentRatePercent { get; init; }

    [JsonPropertyName("levels")]
    public WorkforceLevelSummary[] Levels { get; init; } = Array.Empty<WorkforceLevelSummary>();

    [JsonPropertyName("source_component")]
    public string SourceComponent { get; init; } = "ecs.education";
}

public sealed class TransportProxySummary : MetricGroup
{
    [JsonPropertyName("road_vehicle_entities")]
    public int? RoadVehicleEntities { get; init; }

    [JsonPropertyName("public_transport_vehicle_entities")]
    public int? PublicTransportVehicleEntities { get; init; }

    [JsonPropertyName("active_transport_lines")]
    public int? ActiveTransportLines { get; init; }

    [JsonPropertyName("congestion_index_0_to_1")]
    public double? CongestionIndex0To1 { get; init; }

    [JsonPropertyName("source_component")]
    public string SourceComponent { get; init; } = "ecs.transport_proxy";
}

public sealed class WorkforceSummary : MetricGroup
{
    [JsonPropertyName("total_potential_workers")]
    public int? TotalPotentialWorkers { get; init; }

    [JsonPropertyName("workers")]
    public int? Workers { get; init; }

    [JsonPropertyName("unemployed")]
    public int? Unemployed { get; init; }

    [JsonPropertyName("homeless_unemployed")]
    public int? HomelessUnemployed { get; init; }

    [JsonPropertyName("employable")]
    public int? Employable { get; init; }

    [JsonPropertyName("outside_workers")]
    public int? OutsideWorkers { get; init; }

    [JsonPropertyName("underemployed_workers")]
    public int? UnderemployedWorkers { get; init; }

    [JsonPropertyName("levels")]
    public WorkforceLevelSummary[] Levels { get; init; } = Array.Empty<WorkforceLevelSummary>();

    [JsonPropertyName("source_component")]
    public string SourceComponent { get; init; } = "ecs.workforce";
}

public sealed class WorkforceLevelSummary
{
    [JsonPropertyName("level")]
    public int Level { get; init; }

    [JsonPropertyName("total")]
    public int Total { get; init; }

    [JsonPropertyName("total_percent")]
    public double? TotalPercent { get; init; }

    [JsonPropertyName("workers")]
    public int Workers { get; init; }

    [JsonPropertyName("unemployed")]
    public int Unemployed { get; init; }

    [JsonPropertyName("unemployed_percent")]
    public double? UnemployedPercent { get; init; }

    [JsonPropertyName("homeless")]
    public int Homeless { get; init; }

    [JsonPropertyName("employable")]
    public int Employable { get; init; }

    [JsonPropertyName("outside")]
    public int Outside { get; init; }

    [JsonPropertyName("under")]
    public int Under { get; init; }
}

public sealed class WorkplacesSummary : MetricGroup
{
    [JsonPropertyName("total_workplaces")]
    public int? TotalWorkplaces { get; init; }

    [JsonPropertyName("filled_workplaces")]
    public int? FilledWorkplaces { get; init; }

    [JsonPropertyName("open_workplaces")]
    public int? OpenWorkplaces { get; init; }

    [JsonPropertyName("commuter_employees")]
    public int? CommuterEmployees { get; init; }

    [JsonPropertyName("work_providers_total")]
    public int? WorkProvidersTotal { get; init; }

    [JsonPropertyName("work_providers_service")]
    public int? WorkProvidersService { get; init; }

    [JsonPropertyName("work_providers_commercial")]
    public int? WorkProvidersCommercial { get; init; }

    [JsonPropertyName("work_providers_leisure")]
    public int? WorkProvidersLeisure { get; init; }

    [JsonPropertyName("work_providers_extractor")]
    public int? WorkProvidersExtractor { get; init; }

    [JsonPropertyName("work_providers_industrial")]
    public int? WorkProvidersIndustrial { get; init; }

    [JsonPropertyName("work_providers_office")]
    public int? WorkProvidersOffice { get; init; }

    [JsonPropertyName("levels")]
    public WorkplaceLevelSummary[] Levels { get; init; } = Array.Empty<WorkplaceLevelSummary>();

    [JsonPropertyName("source_component")]
    public string SourceComponent { get; init; } = "ecs.workplaces";
}

public sealed class WorkplaceLevelSummary
{
    [JsonPropertyName("level")]
    public int Level { get; init; }

    [JsonPropertyName("total")]
    public int Total { get; init; }

    [JsonPropertyName("service")]
    public int Service { get; init; }

    [JsonPropertyName("commercial")]
    public int Commercial { get; init; }

    [JsonPropertyName("leisure")]
    public int Leisure { get; init; }

    [JsonPropertyName("extractor")]
    public int Extractor { get; init; }

    [JsonPropertyName("industrial")]
    public int Industrial { get; init; }

    [JsonPropertyName("office")]
    public int Office { get; init; }

    [JsonPropertyName("employees")]
    public int Employees { get; init; }

    [JsonPropertyName("open")]
    public int Open { get; init; }

    [JsonPropertyName("commuter")]
    public int Commuter { get; init; }
}

public sealed class MetricDefinition
{
    [JsonPropertyName("measurement_kind")]
    public string MeasurementKind { get; init; } = MetricMeasurementKind.Proxy;

    [JsonPropertyName("time_basis")]
    public string TimeBasis { get; init; } = MetricTimeBasis.Instant;

    [JsonPropertyName("units")]
    public string Units { get; init; } = "count";

    [JsonPropertyName("source_component")]
    public string SourceComponent { get; init; } = "unknown";
}

public static class MetricMetadataDefaults
{
    public static SortedDictionary<string, MetricDefinition> Mobility()
    {
        return new SortedDictionary<string, MetricDefinition>(StringComparer.Ordinal)
        {
            ["traffic_volume_index"] = Def(MetricMeasurementKind.Derived, MetricTimeBasis.Instant, "index", "Game.Vehicles.Vehicle|Game.Vehicles.PublicTransport"),
            ["lines_total"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.Routes.TransportLine|Game.Routes.CargoTransportLine"),
            ["passenger_lines_total"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.Routes.TransportLine"),
            ["cargo_lines_total"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.Routes.CargoTransportLine"),
            ["lines_by_transport_type"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.UI.InGame.TransportUIUtils|Game.Routes.TransportLine|Game.Prefabs.PrefabRef|Game.Prefabs.TransportType"),
            ["active_vehicles_by_transport_type"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.UI.InGame.TransportUIUtils|Game.Vehicles.PublicTransport|Game.Routes.TransportLine"),
            ["lines_with_service_count"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.UI.InGame.TransportUIUtils|Game.Routes.TransportLine|Game.Vehicles.PublicTransport"),
            ["lines_without_service_count"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.UI.InGame.TransportUIUtils|Game.Routes.TransportLine|Game.Vehicles.PublicTransport"),
            ["lines_with_service_percent"] = Def(MetricMeasurementKind.Derived, MetricTimeBasis.Instant, "percent", "Game.UI.InGame.TransportUIUtils|Game.Routes.TransportLine|Game.Vehicles.PublicTransport"),
            ["line_vehicle_entities_p50"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.UI.InGame.TransportUIUtils|Game.Routes.TransportLine|Game.Vehicles.PublicTransport"),
            ["line_vehicle_entities_p95"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.UI.InGame.TransportUIUtils|Game.Routes.TransportLine|Game.Vehicles.PublicTransport"),
            ["top_lines_by_active_vehicles"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.UI.InGame.TransportUIUtils|Game.Routes.TransportLine|Game.Vehicles.PublicTransport"),
            ["lines"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "line_records", "Game.UI.InGame.TransportUIUtils|Game.UI.NameSystem|Game.Routes.Color|Game.Routes.RouteNumber|BelzontTLM.XTMRouteExtraData")
        };
    }

    public static SortedDictionary<string, MetricDefinition> EconomySignals()
    {
        return new SortedDictionary<string, MetricDefinition>(StringComparer.Ordinal)
        {
            ["land_value_avg"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "land_value_index", "Game.Buildings.LandValue"),
            ["land_value_p25"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "land_value_index", "Game.Buildings.LandValue"),
            ["land_value_p50"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "land_value_index", "Game.Buildings.LandValue"),
            ["land_value_p75"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "land_value_index", "Game.Buildings.LandValue"),
            ["citizen_wealth_avg"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "currency", "Game.Citizens.Household.m_Resources"),
            ["citizen_wealth_p25"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "currency", "Game.Citizens.Household.m_Resources"),
            ["citizen_wealth_p50"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "currency", "Game.Citizens.Household.m_Resources"),
            ["citizen_wealth_p75"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "currency", "Game.Citizens.Household.m_Resources")
        };
    }

    public static SortedDictionary<string, MetricDefinition> ExternalConnections()
    {
        return new SortedDictionary<string, MetricDefinition>(StringComparer.Ordinal)
        {
            ["imports_total_value"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Monthly, "currency_per_month", "Game.Economy.TradeCost"),
            ["exports_total_value"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Monthly, "currency_per_month", "Game.Economy.TradeCost"),
            ["imports_by_resource"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Monthly, "currency_per_month", "Game.Economy.TradeCost"),
            ["exports_by_resource"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Monthly, "currency_per_month", "Game.Economy.TradeCost"),
            ["service_trade"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Monthly, "service_units_per_month", "Game.Net.OutsideConnection|Game.Objects.OutsideConnection")
        };
    }

    public static SortedDictionary<string, MetricDefinition> LaborMarketDetail()
    {
        return new SortedDictionary<string, MetricDefinition>(StringComparer.Ordinal)
        {
            ["jobs_available_by_education_level"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "jobs", "Game.Companies.WorkProvider|Game.Companies.Employee|Game.Prefabs.WorkplaceData"),
            ["jobs_filled_by_education_level"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "jobs", "Game.Companies.WorkProvider|Game.Companies.Employee"),
            ["jobs_open_by_education_level"] = Def(MetricMeasurementKind.Derived, MetricTimeBasis.Instant, "jobs", "Game.Companies.WorkProvider|Game.Companies.Employee"),
            ["workforce_by_education_level"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "workers", "Game.Citizens.Citizen|Game.Citizens.Worker|Game.Citizens.Student")
        };
    }

    public static SortedDictionary<string, MetricDefinition> FacilityIdentity()
    {
        return new SortedDictionary<string, MetricDefinition>(StringComparer.Ordinal)
        {
            ["total_building_entities"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.Buildings.Building"),
            ["residential_building_entities"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.Buildings.ResidentialProperty|Game.Buildings.ResidentialBuilding"),
            ["transport_building_entities"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.Buildings.TransportStation|Game.Buildings.PublicTransportStation|Game.Buildings.TransportDepot"),
            ["active_work_provider_entities"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.Companies.WorkProvider|Game.Companies.Employee|Game.Prefabs.PrefabRef"),
            ["service_provider_entities"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.Companies.WorkProvider"),
            ["commercial_provider_entities"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.Companies.WorkProvider|Game.Companies.CommercialCompany"),
            ["leisure_provider_entities"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.Companies.WorkProvider|Game.Companies.CommercialCompany|Game.Prefabs.IndustrialProcessData"),
            ["extractor_provider_entities"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.Companies.WorkProvider|Game.Companies.ExtractorCompany"),
            ["industrial_provider_entities"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.Companies.WorkProvider|Game.Companies.IndustrialCompany"),
            ["office_provider_entities"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.Companies.WorkProvider|Game.Companies.IndustrialCompany|Game.Prefabs.IndustrialProcessData")
        };
    }

    public static SortedDictionary<string, MetricDefinition> CompanyServiceSemantics()
    {
        return new SortedDictionary<string, MetricDefinition>(StringComparer.Ordinal)
        {
            ["provider_counts"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "sector_counts", "Game.Companies.WorkProvider|Game.Companies.Employee|Game.Prefabs.PrefabRef"),
            ["jobs_total"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "jobs", "Game.Companies.WorkProvider|Game.Companies.Employee|Game.Prefabs.WorkplaceData"),
            ["jobs_filled"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "jobs", "Game.Companies.WorkProvider|Game.Companies.Employee"),
            ["jobs_open"] = Def(MetricMeasurementKind.Derived, MetricTimeBasis.Instant, "jobs", "Game.Companies.WorkProvider|Game.Companies.Employee"),
            ["fill_percent"] = Def(MetricMeasurementKind.Derived, MetricTimeBasis.Instant, "percent", "Game.Companies.WorkProvider|Game.Companies.Employee")
        };
    }

    public static SortedDictionary<string, MetricDefinition> TransitPerformanceSemantics()
    {
        return new SortedDictionary<string, MetricDefinition>(StringComparer.Ordinal)
        {
            ["usage_observed_lines"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.UI.InGame.TransportUIUtils|Game.Routes.TransportLine|Game.Vehicles.PublicTransport"),
            ["capacity_observed_lines"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.UI.InGame.TransportUIUtils|Game.Routes.TransportLine|Game.Vehicles.PublicTransport"),
            ["high_pressure_lines"] = Def(MetricMeasurementKind.Derived, MetricTimeBasis.Instant, "count", "Game.UI.InGame.TransportUIUtils|Game.Routes.TransportLine|Game.Vehicles.PublicTransport"),
            ["critical_pressure_lines"] = Def(MetricMeasurementKind.Derived, MetricTimeBasis.Instant, "count", "Game.UI.InGame.TransportUIUtils|Game.Routes.TransportLine|Game.Vehicles.PublicTransport"),
            ["top_pressure_lines"] = Def(MetricMeasurementKind.Derived, MetricTimeBasis.Instant, "line_records", "Game.UI.InGame.TransportUIUtils|Game.Routes.TransportLine|Game.Vehicles.PublicTransport"),
            ["average_usage_percent_by_mode"] = Def(MetricMeasurementKind.Derived, MetricTimeBasis.Instant, "percent", "Game.UI.InGame.TransportUIUtils|Game.Routes.TransportLine|Game.Vehicles.PublicTransport"),
            ["high_pressure_lines_by_mode"] = Def(MetricMeasurementKind.Derived, MetricTimeBasis.Instant, "count", "Game.UI.InGame.TransportUIUtils|Game.Routes.TransportLine|Game.Vehicles.PublicTransport"),
            ["usage_observed_lines_by_mode"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.UI.InGame.TransportUIUtils|Game.Routes.TransportLine|Game.Vehicles.PublicTransport"),
            ["no_service_lines"] = Def(MetricMeasurementKind.Derived, MetricTimeBasis.Instant, "count", "Game.UI.InGame.TransportUIUtils|Game.Routes.TransportLine|Game.Vehicles.PublicTransport"),
            ["thin_service_lines"] = Def(MetricMeasurementKind.Derived, MetricTimeBasis.Instant, "count", "Game.UI.InGame.TransportUIUtils|Game.Routes.TransportLine|Game.Vehicles.PublicTransport"),
            ["missing_usage_observed_lines"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.UI.InGame.TransportUIUtils|Game.Routes.TransportLine|Game.Vehicles.PublicTransport"),
            ["missing_capacity_observed_lines"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.UI.InGame.TransportUIUtils|Game.Routes.TransportLine|Game.Vehicles.PublicTransport")
        };
    }

    public static SortedDictionary<string, MetricDefinition> TransitLineDetailSemantics()
    {
        return new SortedDictionary<string, MetricDefinition>(StringComparer.Ordinal)
        {
            ["lines_observed"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.Routes.TransportLine|Game.Routes.RouteWaypoint|Game.Routes.RouteVehicle"),
            ["passenger_lines_observed"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.Routes.TransportLine"),
            ["cargo_lines_observed"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.Routes.CargoTransportLine"),
            ["total_waiting_passengers"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "passengers", "Game.Routes.RouteWaypoint|Game.Routes.WaitingPassengers"),
            ["total_onboard_passengers"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "passengers", "Game.Routes.RouteVehicle|Game.Vehicles.Passenger"),
            ["max_waiting_passengers_at_stop"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "passengers", "Game.Routes.RouteWaypoint|Game.Routes.WaitingPassengers"),
            ["lines"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "line_records", "Game.UI.InGame.TransportUIUtils|Game.Routes.RouteWaypoint|Game.Routes.RouteSegment|Game.Routes.RouteVehicle|Game.Pathfind.PathInformation|Game.Vehicles.Odometer")
        };
    }

    public static SortedDictionary<string, MetricDefinition> HousingPressureSemantics()
    {
        return new SortedDictionary<string, MetricDefinition>(StringComparer.Ordinal)
        {
            ["total_households"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.Citizens.Household"),
            ["residential_building_entities"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.Buildings.ResidentialProperty|Game.Buildings.ResidentialBuilding"),
            ["local_households"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.Citizens.Household|Game.Citizens.HouseholdMember"),
            ["homeless_households"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.Citizens.Household|Game.Citizens.HomelessHousehold|Game.Buildings.PropertyRenter"),
            ["moving_away_households"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.Citizens.Household|Game.Citizens.MovingAway"),
            ["households_per_residential_building"] = Def(MetricMeasurementKind.Derived, MetricTimeBasis.Instant, "households_per_building", "Game.Citizens.Household|Game.Buildings.ResidentialProperty"),
            ["local_households_per_residential_building"] = Def(MetricMeasurementKind.Derived, MetricTimeBasis.Instant, "households_per_building", "Game.Citizens.Household|Game.Citizens.HouseholdMember|Game.Buildings.ResidentialProperty")
        };
    }

    public static SortedDictionary<string, MetricDefinition> HouseholdPressureContext()
    {
        return new SortedDictionary<string, MetricDefinition>(StringComparer.Ordinal)
        {
            ["total_households"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.Citizens.Household"),
            ["local_households"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.Citizens.Household|Game.Citizens.HouseholdMember"),
            ["property_linked_households"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.Citizens.Household|Game.Buildings.PropertyRenter"),
            ["homeless_households"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.Citizens.Household|Game.Citizens.HomelessHousehold|Game.Buildings.PropertyRenter"),
            ["moving_away_households"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.Citizens.Household|Game.Citizens.MovingAway"),
            ["homeless_household_share_percent"] = Def(MetricMeasurementKind.Derived, MetricTimeBasis.Instant, "percent", "Game.Citizens.Household|Game.Citizens.HomelessHousehold"),
            ["moving_away_household_share_percent"] = Def(MetricMeasurementKind.Derived, MetricTimeBasis.Instant, "percent", "Game.Citizens.Household|Game.Citizens.MovingAway")
        };
    }

    public static SortedDictionary<string, MetricDefinition> LaborPressureContext()
    {
        return new SortedDictionary<string, MetricDefinition>(StringComparer.Ordinal)
        {
            ["total_potential_workers"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "workers", "Game.Citizens.Citizen|Game.Citizens.Worker|Game.Citizens.Student"),
            ["total_jobs"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "jobs", "Game.Companies.WorkProvider|Game.Companies.Employee|Game.Prefabs.WorkplaceData"),
            ["jobs_minus_potential_workers"] = Def(MetricMeasurementKind.Derived, MetricTimeBasis.Instant, "jobs", "Game.Companies.WorkProvider|Game.Citizens.Worker"),
            ["jobs_minus_current_workers"] = Def(MetricMeasurementKind.Derived, MetricTimeBasis.Instant, "jobs", "Game.Companies.WorkProvider|Game.Citizens.Worker"),
            ["outside_worker_share_percent"] = Def(MetricMeasurementKind.Derived, MetricTimeBasis.Instant, "percent", "Game.Citizens.Worker|Game.Objects.OutsideConnection"),
            ["underemployed_worker_share_percent"] = Def(MetricMeasurementKind.Derived, MetricTimeBasis.Instant, "percent", "Game.Citizens.Worker"),
            ["commuter_employee_share_percent"] = Def(MetricMeasurementKind.Derived, MetricTimeBasis.Instant, "percent", "Game.Companies.Employee|Game.Citizens.Citizen")
        };
    }

    public static SortedDictionary<string, MetricDefinition> TransitAccessGapSemantics()
    {
        return new SortedDictionary<string, MetricDefinition>(StringComparer.Ordinal)
        {
            ["capture_context"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.CaptureWindow, "capture_window", "Game.Citizens.Human|Game.Pathfind.PathOwner|Game.Pathfind.PathElement"),
            ["summary"] = Def(MetricMeasurementKind.Derived, MetricTimeBasis.CaptureWindow, "summary", "Game.Citizens.Human|Game.Pathfind.PathOwner|Game.Pathfind.PathElement|Game.Routes.TransportStop"),
            ["hotspots"] = Def(MetricMeasurementKind.Derived, MetricTimeBasis.CaptureWindow, "hotspot_records", "Game.Citizens.Human|Game.Pathfind.PathOwner|Game.Pathfind.PathElement|Game.Routes.TransportStop")
        };
    }

    public static SortedDictionary<string, MetricDefinition> OfficialCityStatistics()
    {
        return new SortedDictionary<string, MetricDefinition>(StringComparer.Ordinal)
        {
            ["time"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "game_time", "Game.Simulation.TimeSystem|Game.Simulation.SimulationSystem|Game.City.CityStatisticsSystem"),
            ["finance"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "currency", "Game.City.CitySystem|Game.City.CityStatisticsSystem"),
            ["taxes"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "currency", "Game.City.CityStatisticsSystem"),
            ["population_flow"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.City.CityStatisticsSystem|Game.City.Population"),
            ["social"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "mixed", "Game.City.CityStatisticsSystem"),
            ["tourism"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "mixed", "Game.City.CityStatisticsSystem|Game.City.Tourism"),
            ["transport_totals"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "count", "Game.City.CityStatisticsSystem"),
            ["sectors"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "mixed", "Game.City.CityStatisticsSystem"),
            ["city_services"] = Def(MetricMeasurementKind.Observed, MetricTimeBasis.Instant, "mixed", "Game.City.CityStatisticsSystem|Game.City.DevTreePoints")
        };
    }

    private static MetricDefinition Def(string kind, string timeBasis, string units, string source)
    {
        return new MetricDefinition
        {
            MeasurementKind = kind,
            TimeBasis = timeBasis,
            Units = units,
            SourceComponent = source
        };
    }
}

public sealed class ModeMonthlyCounts
{
    [JsonPropertyName("bus")]
    public int? Bus { get; init; }

    [JsonPropertyName("tram")]
    public int? Tram { get; init; }

    [JsonPropertyName("subway")]
    public int? Subway { get; init; }

    [JsonPropertyName("train")]
    public int? Train { get; init; }

    [JsonPropertyName("ship")]
    public int? Ship { get; init; }

    [JsonPropertyName("air")]
    public int? Air { get; init; }

    [JsonPropertyName("taxi")]
    public int? Taxi { get; init; }
}

public sealed class ModeMonthlyTons
{
    [JsonPropertyName("truck")]
    public double? Truck { get; init; }

    [JsonPropertyName("train")]
    public double? Train { get; init; }

    [JsonPropertyName("ship")]
    public double? Ship { get; init; }

    [JsonPropertyName("air")]
    public double? Air { get; init; }
}

public sealed class ModeEntityCounts
{
    [JsonPropertyName("bus")]
    public int? Bus { get; init; }

    [JsonPropertyName("tram")]
    public int? Tram { get; init; }

    [JsonPropertyName("subway")]
    public int? Subway { get; init; }

    [JsonPropertyName("train")]
    public int? Train { get; init; }

    [JsonPropertyName("ship")]
    public int? Ship { get; init; }

    [JsonPropertyName("ferry")]
    public int? Ferry { get; init; }

    [JsonPropertyName("air")]
    public int? Air { get; init; }

    [JsonPropertyName("taxi")]
    public int? Taxi { get; init; }

    [JsonPropertyName("unknown")]
    public int? Unknown { get; init; }
}

public sealed class ModeDoubleValues
{
    [JsonPropertyName("bus")]
    public double? Bus { get; init; }

    [JsonPropertyName("tram")]
    public double? Tram { get; init; }

    [JsonPropertyName("subway")]
    public double? Subway { get; init; }

    [JsonPropertyName("train")]
    public double? Train { get; init; }

    [JsonPropertyName("ship")]
    public double? Ship { get; init; }

    [JsonPropertyName("ferry")]
    public double? Ferry { get; init; }

    [JsonPropertyName("air")]
    public double? Air { get; init; }

    [JsonPropertyName("taxi")]
    public double? Taxi { get; init; }

    [JsonPropertyName("unknown")]
    public double? Unknown { get; init; }
}

public sealed class TransportLineUsageEntry
{
    [JsonPropertyName("line_entity_index")]
    public int LineEntityIndex { get; init; }

    [JsonPropertyName("line_entity_version")]
    public int LineEntityVersion { get; init; }

    [JsonPropertyName("line_name")]
    public string? LineName { get; init; }

    [JsonPropertyName("active_vehicle_entities")]
    public int ActiveVehicleEntities { get; init; }

    [JsonPropertyName("onboard_passenger_entities")]
    public int? OnboardPassengerEntities { get; init; }

    [JsonPropertyName("total_passenger_capacity")]
    public int? TotalPassengerCapacity { get; init; }

    [JsonPropertyName("usage_percent")]
    public double? UsagePercent { get; init; }

    [JsonPropertyName("usage_percent_proxy")]
    public double? UsagePercentProxy { get; init; }
}

public sealed class MobilityLineRecord
{
    [JsonPropertyName("line_entity_index")]
    public int LineEntityIndex { get; init; }

    [JsonPropertyName("line_entity_version")]
    public int LineEntityVersion { get; init; }

    [JsonPropertyName("line_name")]
    public string? LineName { get; init; }

    [JsonPropertyName("line_identifier")]
    public string? LineIdentifier { get; init; }

    [JsonPropertyName("line_identifier_source")]
    public string LineIdentifierSource { get; init; } = "none";

    [JsonPropertyName("route_number")]
    public int? RouteNumber { get; init; }

    [JsonPropertyName("mode")]
    public string Mode { get; init; } = "unknown";

    [JsonPropertyName("is_cargo")]
    public bool IsCargo { get; init; }

    [JsonPropertyName("line_color")]
    public string? LineColor { get; init; }

    [JsonPropertyName("active")]
    public bool Active { get; init; }

    [JsonPropertyName("visible")]
    public bool Visible { get; init; }

    [JsonPropertyName("schedule")]
    public string Schedule { get; init; } = "unknown";

    [JsonPropertyName("stops")]
    public int Stops { get; init; }

    [JsonPropertyName("active_vehicle_entities")]
    public int ActiveVehicleEntities { get; init; }

    [JsonPropertyName("onboard_passenger_entities")]
    public int? OnboardPassengerEntities { get; init; }

    [JsonPropertyName("total_passenger_capacity")]
    public int? TotalPassengerCapacity { get; init; }

    [JsonPropertyName("usage_percent")]
    public double? UsagePercent { get; init; }

    [JsonPropertyName("length_m")]
    public double? LengthM { get; init; }
}

public sealed class TransportLineTopSummary
{
    [JsonPropertyName("mode")]
    public string Mode { get; init; } = "unknown";

    [JsonPropertyName("line_entity_index")]
    public int LineEntityIndex { get; init; }

    [JsonPropertyName("line_entity_version")]
    public int LineEntityVersion { get; init; }

    [JsonPropertyName("line_name")]
    public string? LineName { get; init; }

    [JsonPropertyName("active_vehicle_entities")]
    public int ActiveVehicleEntities { get; init; }

    [JsonPropertyName("onboard_passenger_entities")]
    public int? OnboardPassengerEntities { get; init; }

    [JsonPropertyName("usage_percent_proxy")]
    public double? UsagePercentProxy { get; init; }
}

public sealed class TransportTypeLineUsageSummary
{
    [JsonPropertyName("line_count")]
    public int LineCount { get; init; }

    [JsonPropertyName("active_vehicle_entities")]
    public int ActiveVehicleEntities { get; init; }

    [JsonPropertyName("onboard_passenger_entities")]
    public int? OnboardPassengerEntities { get; init; }

    [JsonPropertyName("line_usage_avg_percent")]
    public double? LineUsageAvgPercent { get; init; }

    [JsonPropertyName("line_usage_p95_percent")]
    public double? LineUsageP95Percent { get; init; }

    [JsonPropertyName("lines_with_service_count")]
    public int LinesWithServiceCount { get; init; }

    [JsonPropertyName("lines_without_service_count")]
    public int LinesWithoutServiceCount { get; init; }

    [JsonPropertyName("vehicles_per_line_avg")]
    public double? VehiclesPerLineAvg { get; init; }

    [JsonPropertyName("lines")]
    public TransportLineUsageEntry[] Lines { get; init; } = Array.Empty<TransportLineUsageEntry>();
}

public sealed class LineUsageByTransportType
{
    [JsonPropertyName("bus")]
    public TransportTypeLineUsageSummary? Bus { get; init; }

    [JsonPropertyName("tram")]
    public TransportTypeLineUsageSummary? Tram { get; init; }

    [JsonPropertyName("subway")]
    public TransportTypeLineUsageSummary? Subway { get; init; }

    [JsonPropertyName("train")]
    public TransportTypeLineUsageSummary? Train { get; init; }

    [JsonPropertyName("ship")]
    public TransportTypeLineUsageSummary? Ship { get; init; }

    [JsonPropertyName("air")]
    public TransportTypeLineUsageSummary? Air { get; init; }

    [JsonPropertyName("taxi")]
    public TransportTypeLineUsageSummary? Taxi { get; init; }

    [JsonPropertyName("unknown")]
    public TransportTypeLineUsageSummary? Unknown { get; init; }
}

public sealed class MobilitySummary : MetricGroup
{
    [JsonPropertyName("traffic_volume_index")]
    public double? TrafficVolumeIndex { get; init; }

    [JsonPropertyName("lines_total")]
    public int? LinesTotal { get; init; }

    [JsonPropertyName("passenger_lines_total")]
    public int? PassengerLinesTotal { get; init; }

    [JsonPropertyName("cargo_lines_total")]
    public int? CargoLinesTotal { get; init; }

    [JsonPropertyName("lines_by_transport_type")]
    public ModeEntityCounts? LinesByTransportType { get; init; }

    [JsonPropertyName("active_vehicles_by_transport_type")]
    public ModeEntityCounts? ActiveVehiclesByTransportType { get; init; }

    [JsonPropertyName("lines_with_service_count")]
    public int? LinesWithServiceCount { get; init; }

    [JsonPropertyName("lines_without_service_count")]
    public int? LinesWithoutServiceCount { get; init; }

    [JsonPropertyName("lines_with_service_percent")]
    public double? LinesWithServicePercent { get; init; }

    [JsonPropertyName("line_vehicle_entities_p50")]
    public double? LineVehicleEntitiesP50 { get; init; }

    [JsonPropertyName("line_vehicle_entities_p95")]
    public double? LineVehicleEntitiesP95 { get; init; }

    [JsonPropertyName("top_lines_by_active_vehicles")]
    public MobilityLineRecord[] TopLinesByActiveVehicles { get; init; } = Array.Empty<MobilityLineRecord>();

    [JsonPropertyName("lines")]
    public MobilityLineRecord[] Lines { get; init; } = Array.Empty<MobilityLineRecord>();

    [JsonPropertyName("source_component")]
    public string SourceComponent { get; init; } = "ecs.mobility";

    [JsonPropertyName("metric_metadata")]
    public SortedDictionary<string, MetricDefinition> MetricMetadata { get; init; } = MetricMetadataDefaults.Mobility();
}

public sealed class EconomySignalsSummary : MetricGroup
{
    [JsonPropertyName("land_value_avg")]
    public double? LandValueAvg { get; init; }

    [JsonPropertyName("land_value_p25")]
    public double? LandValueP25 { get; init; }

    [JsonPropertyName("land_value_p50")]
    public double? LandValueP50 { get; init; }

    [JsonPropertyName("land_value_p75")]
    public double? LandValueP75 { get; init; }

    [JsonPropertyName("citizen_wealth_avg")]
    public double? CitizenWealthAvg { get; init; }

    [JsonPropertyName("citizen_wealth_p25")]
    public double? CitizenWealthP25 { get; init; }

    [JsonPropertyName("citizen_wealth_p50")]
    public double? CitizenWealthP50 { get; init; }

    [JsonPropertyName("citizen_wealth_p75")]
    public double? CitizenWealthP75 { get; init; }

    [JsonPropertyName("source_component")]
    public string SourceComponent { get; init; } = "ecs.economy_signals";

    [JsonPropertyName("metric_metadata")]
    public SortedDictionary<string, MetricDefinition> MetricMetadata { get; init; } = MetricMetadataDefaults.EconomySignals();
}

public sealed class ExternalConnectionsSummary : MetricGroup
{
    [JsonPropertyName("imports_total_value")]
    public double? ImportsTotalValue { get; init; }

    [JsonPropertyName("exports_total_value")]
    public double? ExportsTotalValue { get; init; }

    [JsonPropertyName("imports_by_resource")]
    public SortedDictionary<string, double?>? ImportsByResource { get; init; }

    [JsonPropertyName("exports_by_resource")]
    public SortedDictionary<string, double?>? ExportsByResource { get; init; }

    [JsonPropertyName("service_trade")]
    public SortedDictionary<string, double?>? ServiceTrade { get; init; }

    [JsonPropertyName("source_component")]
    public string SourceComponent { get; init; } = "ecs.external_connections";

    [JsonPropertyName("metric_metadata")]
    public SortedDictionary<string, MetricDefinition> MetricMetadata { get; init; } = MetricMetadataDefaults.ExternalConnections();
}

public sealed class LevelCountSummary
{
    [JsonPropertyName("level_0")]
    public int? Level0 { get; init; }

    [JsonPropertyName("level_1")]
    public int? Level1 { get; init; }

    [JsonPropertyName("level_2")]
    public int? Level2 { get; init; }

    [JsonPropertyName("level_3")]
    public int? Level3 { get; init; }

    [JsonPropertyName("level_4")]
    public int? Level4 { get; init; }

    [JsonPropertyName("total")]
    public int? Total { get; init; }
}

public sealed class WorkforceByEducationSummary
{
    [JsonPropertyName("potential")]
    public LevelCountSummary Potential { get; init; } = new();

    [JsonPropertyName("workers")]
    public LevelCountSummary Workers { get; init; } = new();

    [JsonPropertyName("unemployed")]
    public LevelCountSummary Unemployed { get; init; } = new();

    [JsonPropertyName("underemployed")]
    public LevelCountSummary Underemployed { get; init; } = new();
}

public sealed class LaborMarketDetailSummary : MetricGroup
{
    [JsonPropertyName("jobs_available_by_education_level")]
    public LevelCountSummary? JobsAvailableByEducationLevel { get; init; }

    [JsonPropertyName("jobs_filled_by_education_level")]
    public LevelCountSummary? JobsFilledByEducationLevel { get; init; }

    [JsonPropertyName("jobs_open_by_education_level")]
    public LevelCountSummary? JobsOpenByEducationLevel { get; init; }

    [JsonPropertyName("workforce_by_education_level")]
    public WorkforceByEducationSummary? WorkforceByEducationLevel { get; init; }

    [JsonPropertyName("source_component")]
    public string SourceComponent { get; init; } = "ecs.labor_market_detail";

    [JsonPropertyName("metric_metadata")]
    public SortedDictionary<string, MetricDefinition> MetricMetadata { get; init; } = MetricMetadataDefaults.LaborMarketDetail();
}

public sealed class FacilityIdentitySummary : MetricGroup
{
    [JsonPropertyName("total_building_entities")]
    public int? TotalBuildingEntities { get; init; }

    [JsonPropertyName("residential_building_entities")]
    public int? ResidentialBuildingEntities { get; init; }

    [JsonPropertyName("transport_building_entities")]
    public int? TransportBuildingEntities { get; init; }

    [JsonPropertyName("active_work_provider_entities")]
    public int? ActiveWorkProviderEntities { get; init; }

    [JsonPropertyName("service_provider_entities")]
    public int? ServiceProviderEntities { get; init; }

    [JsonPropertyName("commercial_provider_entities")]
    public int? CommercialProviderEntities { get; init; }

    [JsonPropertyName("leisure_provider_entities")]
    public int? LeisureProviderEntities { get; init; }

    [JsonPropertyName("extractor_provider_entities")]
    public int? ExtractorProviderEntities { get; init; }

    [JsonPropertyName("industrial_provider_entities")]
    public int? IndustrialProviderEntities { get; init; }

    [JsonPropertyName("office_provider_entities")]
    public int? OfficeProviderEntities { get; init; }

    [JsonPropertyName("source_component")]
    public string SourceComponent { get; init; } = "ecs.facility_identity";

    [JsonPropertyName("metric_metadata")]
    public SortedDictionary<string, MetricDefinition> MetricMetadata { get; init; } = MetricMetadataDefaults.FacilityIdentity();
}

public sealed class SectorIntSummary
{
    [JsonPropertyName("total")]
    public int? Total { get; init; }

    [JsonPropertyName("service")]
    public int? Service { get; init; }

    [JsonPropertyName("commercial")]
    public int? Commercial { get; init; }

    [JsonPropertyName("leisure")]
    public int? Leisure { get; init; }

    [JsonPropertyName("extractor")]
    public int? Extractor { get; init; }

    [JsonPropertyName("industrial")]
    public int? Industrial { get; init; }

    [JsonPropertyName("office")]
    public int? Office { get; init; }
}

public sealed class SectorDoubleSummary
{
    [JsonPropertyName("total")]
    public double? Total { get; init; }

    [JsonPropertyName("service")]
    public double? Service { get; init; }

    [JsonPropertyName("commercial")]
    public double? Commercial { get; init; }

    [JsonPropertyName("leisure")]
    public double? Leisure { get; init; }

    [JsonPropertyName("extractor")]
    public double? Extractor { get; init; }

    [JsonPropertyName("industrial")]
    public double? Industrial { get; init; }

    [JsonPropertyName("office")]
    public double? Office { get; init; }
}

public sealed class CompanyServiceSemanticsSummary : MetricGroup
{
    [JsonPropertyName("provider_counts")]
    public SectorIntSummary ProviderCounts { get; init; } = new();

    [JsonPropertyName("jobs_total")]
    public SectorIntSummary JobsTotal { get; init; } = new();

    [JsonPropertyName("jobs_filled")]
    public SectorIntSummary JobsFilled { get; init; } = new();

    [JsonPropertyName("jobs_open")]
    public SectorIntSummary JobsOpen { get; init; } = new();

    [JsonPropertyName("fill_percent")]
    public SectorDoubleSummary FillPercent { get; init; } = new();

    [JsonPropertyName("source_component")]
    public string SourceComponent { get; init; } = "ecs.company_service_semantics";

    [JsonPropertyName("metric_metadata")]
    public SortedDictionary<string, MetricDefinition> MetricMetadata { get; init; } = MetricMetadataDefaults.CompanyServiceSemantics();
}

public sealed class HousingPressureSemanticsSummary : MetricGroup
{
    [JsonPropertyName("total_households")]
    public int? TotalHouseholds { get; init; }

    [JsonPropertyName("residential_building_entities")]
    public int? ResidentialBuildingEntities { get; init; }

    [JsonPropertyName("local_households")]
    public int? LocalHouseholds { get; init; }

    [JsonPropertyName("homeless_households")]
    public int? HomelessHouseholds { get; init; }

    [JsonPropertyName("moving_away_households")]
    public int? MovingAwayHouseholds { get; init; }

    [JsonPropertyName("households_per_residential_building")]
    public double? HouseholdsPerResidentialBuilding { get; init; }

    [JsonPropertyName("local_households_per_residential_building")]
    public double? LocalHouseholdsPerResidentialBuilding { get; init; }

    [JsonPropertyName("source_component")]
    public string SourceComponent { get; init; } = "ecs.housing_pressure_semantics";

    [JsonPropertyName("metric_metadata")]
    public SortedDictionary<string, MetricDefinition> MetricMetadata { get; init; } = MetricMetadataDefaults.HousingPressureSemantics();
}

public sealed class HouseholdPressureContextSummary : MetricGroup
{
    [JsonPropertyName("total_households")]
    public int? TotalHouseholds { get; init; }

    [JsonPropertyName("local_households")]
    public int? LocalHouseholds { get; init; }

    [JsonPropertyName("property_linked_households")]
    public int? PropertyLinkedHouseholds { get; init; }

    [JsonPropertyName("homeless_households")]
    public int? HomelessHouseholds { get; init; }

    [JsonPropertyName("moving_away_households")]
    public int? MovingAwayHouseholds { get; init; }

    [JsonPropertyName("homeless_household_share_percent")]
    public double? HomelessHouseholdSharePercent { get; init; }

    [JsonPropertyName("moving_away_household_share_percent")]
    public double? MovingAwayHouseholdSharePercent { get; init; }

    [JsonPropertyName("source_component")]
    public string SourceComponent { get; init; } = "ecs.household_pressure_context";

    [JsonPropertyName("metric_metadata")]
    public SortedDictionary<string, MetricDefinition> MetricMetadata { get; init; } = MetricMetadataDefaults.HouseholdPressureContext();
}

public sealed class LaborPressureContextSummary : MetricGroup
{
    [JsonPropertyName("total_potential_workers")]
    public int? TotalPotentialWorkers { get; init; }

    [JsonPropertyName("total_jobs")]
    public int? TotalJobs { get; init; }

    [JsonPropertyName("jobs_minus_potential_workers")]
    public int? JobsMinusPotentialWorkers { get; init; }

    [JsonPropertyName("jobs_minus_current_workers")]
    public int? JobsMinusCurrentWorkers { get; init; }

    [JsonPropertyName("outside_worker_share_percent")]
    public double? OutsideWorkerSharePercent { get; init; }

    [JsonPropertyName("underemployed_worker_share_percent")]
    public double? UnderemployedWorkerSharePercent { get; init; }

    [JsonPropertyName("commuter_employee_share_percent")]
    public double? CommuterEmployeeSharePercent { get; init; }

    [JsonPropertyName("source_component")]
    public string SourceComponent { get; init; } = "ecs.labor_pressure_context";

    [JsonPropertyName("metric_metadata")]
    public SortedDictionary<string, MetricDefinition> MetricMetadata { get; init; } = MetricMetadataDefaults.LaborPressureContext();
}

public sealed class TransitPerformanceSemanticsSummary : MetricGroup
{
    [JsonPropertyName("line_pressure")]
    public TransitLinePressureSummary LinePressure { get; init; } = new();

    [JsonPropertyName("mode_pressure")]
    public TransitModePressureSummary ModePressure { get; init; } = new();

    [JsonPropertyName("service_gaps")]
    public TransitServiceGapSummary ServiceGaps { get; init; } = new();

    [JsonPropertyName("source_component")]
    public string SourceComponent { get; init; } = "ecs.transit_performance_semantics";

    [JsonPropertyName("metric_metadata")]
    public SortedDictionary<string, MetricDefinition> MetricMetadata { get; init; } = MetricMetadataDefaults.TransitPerformanceSemantics();
}

public sealed class TransitLineDetailSemanticsSummary : MetricGroup
{
    [JsonPropertyName("lines_observed")]
    public int? LinesObserved { get; init; }

    [JsonPropertyName("passenger_lines_observed")]
    public int? PassengerLinesObserved { get; init; }

    [JsonPropertyName("cargo_lines_observed")]
    public int? CargoLinesObserved { get; init; }

    [JsonPropertyName("total_waiting_passengers")]
    public int? TotalWaitingPassengers { get; init; }

    [JsonPropertyName("total_onboard_passengers")]
    public int? TotalOnboardPassengers { get; init; }

    [JsonPropertyName("max_waiting_passengers_at_stop")]
    public int? MaxWaitingPassengersAtStop { get; init; }

    [JsonPropertyName("lines")]
    public TransitLineDetailRecord[] Lines { get; init; } = Array.Empty<TransitLineDetailRecord>();

    [JsonPropertyName("source_component")]
    public string SourceComponent { get; init; } = "ecs.transit_line_detail";

    [JsonPropertyName("metric_metadata")]
    public SortedDictionary<string, MetricDefinition> MetricMetadata { get; init; } = MetricMetadataDefaults.TransitLineDetailSemantics();
}

public sealed class TransitLineDetailRecord
{
    [JsonPropertyName("line_entity_index")]
    public int LineEntityIndex { get; init; }

    [JsonPropertyName("line_entity_version")]
    public int LineEntityVersion { get; init; }

    [JsonPropertyName("line_name")]
    public string? LineName { get; init; }

    [JsonPropertyName("line_identifier")]
    public string? LineIdentifier { get; init; }

    [JsonPropertyName("line_identifier_source")]
    public string LineIdentifierSource { get; init; } = "none";

    [JsonPropertyName("route_number")]
    public int? RouteNumber { get; init; }

    [JsonPropertyName("mode")]
    public string Mode { get; init; } = "unknown";

    [JsonPropertyName("is_cargo")]
    public bool IsCargo { get; init; }

    [JsonPropertyName("line_color")]
    public string? LineColor { get; init; }

    [JsonPropertyName("active")]
    public bool Active { get; init; }

    [JsonPropertyName("visible")]
    public bool Visible { get; init; }

    [JsonPropertyName("stop_count")]
    public int StopCount { get; init; }

    [JsonPropertyName("segment_count")]
    public int SegmentCount { get; init; }

    [JsonPropertyName("active_vehicle_entities")]
    public int ActiveVehicleEntities { get; init; }

    [JsonPropertyName("stop_capacity")]
    public int? StopCapacity { get; init; }

    [JsonPropertyName("waiting_passengers_all_stops")]
    public int WaitingPassengersAllStops { get; init; }

    [JsonPropertyName("max_waiting_passengers_at_stop")]
    public int MaxWaitingPassengersAtStop { get; init; }

    [JsonPropertyName("onboard_passengers_in_vehicles")]
    public int OnboardPassengersInVehicles { get; init; }

    [JsonPropertyName("total_passenger_capacity")]
    public int? TotalPassengerCapacity { get; init; }

    [JsonPropertyName("average_vehicle_occupancy_percent")]
    public double? AverageVehicleOccupancyPercent { get; init; }

    [JsonPropertyName("average_stop_occupancy_percent")]
    public double? AverageStopOccupancyPercent { get; init; }

    [JsonPropertyName("expected_round_trip_time_ticks")]
    public double? ExpectedRoundTripTimeTicks { get; init; }

    [JsonPropertyName("expected_round_trip_time_minutes")]
    public double? ExpectedRoundTripTimeMinutes { get; init; }

    [JsonPropertyName("next_maintenance_vehicle_entity_index")]
    public int? NextMaintenanceVehicleEntityIndex { get; init; }

    [JsonPropertyName("next_maintenance_vehicle_entity_version")]
    public int? NextMaintenanceVehicleEntityVersion { get; init; }

    [JsonPropertyName("next_maintenance_distance_m")]
    public double? NextMaintenanceDistanceM { get; init; }

    [JsonPropertyName("stops")]
    public TransitLineStopDetailRecord[] Stops { get; init; } = Array.Empty<TransitLineStopDetailRecord>();
}

public sealed class TransitLineStopDetailRecord
{
    [JsonPropertyName("waypoint_entity_index")]
    public int WaypointEntityIndex { get; init; }

    [JsonPropertyName("waypoint_entity_version")]
    public int WaypointEntityVersion { get; init; }

    [JsonPropertyName("stop_name")]
    public string? StopName { get; init; }

    [JsonPropertyName("waiting_passengers")]
    public int WaitingPassengers { get; init; }

    [JsonPropertyName("route_position")]
    public double? RoutePosition { get; init; }
}

public sealed class TransitAccessGapSemanticsSummary : MetricGroup
{
    [JsonPropertyName("capture_context")]
    public TransitAccessGapCaptureContext CaptureContext { get; init; } = new();

    [JsonPropertyName("summary")]
    public TransitAccessGapSummary Summary { get; init; } = new();

    [JsonPropertyName("hotspots")]
    public TransitAccessGapHotspot[] Hotspots { get; init; } = Array.Empty<TransitAccessGapHotspot>();

    [JsonPropertyName("source_component")]
    public string SourceComponent { get; init; } = "ecs.transit_access_gap";

    [JsonPropertyName("metric_metadata")]
    public SortedDictionary<string, MetricDefinition> MetricMetadata { get; init; } = MetricMetadataDefaults.TransitAccessGapSemantics();
}

public sealed class TransitAccessGapCaptureContext
{
    [JsonPropertyName("capture_mode")]
    public string CaptureMode { get; init; } = "off";

    [JsonPropertyName("capture_duration_seconds")]
    public int? CaptureDurationSeconds { get; init; }

    [JsonPropertyName("recorded_trip_count")]
    public int? RecordedTripCount { get; init; }

    [JsonPropertyName("included_snapshot_count")]
    public int? IncludedSnapshotCount { get; init; }

    [JsonPropertyName("outside_trip_mode")]
    public string OutsideTripMode { get; init; } = "exclude";

    [JsonPropertyName("outside_trip_count")]
    public int? OutsideTripCount { get; init; }

    [JsonPropertyName("cluster_radius_m")]
    public int? ClusterRadiusM { get; init; }

    [JsonPropertyName("stop_coverage_radius_m")]
    public double? StopCoverageRadiusM { get; init; }
}

public sealed class TransitAccessGapSummary
{
    [JsonPropertyName("hotspots_total")]
    public int? HotspotsTotal { get; init; }

    [JsonPropertyName("hotspots_with_uncovered_demand")]
    public int? HotspotsWithUncoveredDemand { get; init; }

    [JsonPropertyName("high_priority_hotspots")]
    public int? HighPriorityHotspots { get; init; }

    [JsonPropertyName("critical_priority_hotspots")]
    public int? CriticalPriorityHotspots { get; init; }
}

public sealed class TransitAccessGapHotspot
{
    [JsonPropertyName("hotspot_id")]
    public string HotspotId { get; init; } = string.Empty;

    [JsonPropertyName("label")]
    public string? Label { get; init; }

    [JsonPropertyName("center_position")]
    public TransitAccessGapPosition CenterPosition { get; init; } = new();

    [JsonPropertyName("observed_trip_count")]
    public int? ObservedTripCount { get; init; }

    [JsonPropertyName("sample_route_count")]
    public int? SampleRouteCount { get; init; }

    [JsonPropertyName("bucket_index")]
    public int? BucketIndex { get; init; }

    [JsonPropertyName("priority_score")]
    public double? PriorityScore { get; init; }

    [JsonPropertyName("uncovered_share_percent")]
    public double? UncoveredSharePercent { get; init; }

    [JsonPropertyName("average_nearest_stop_distance_m")]
    public double? AverageNearestStopDistanceM { get; init; }

    [JsonPropertyName("average_uncovered_distance_m")]
    public double? AverageUncoveredDistanceM { get; init; }

    [JsonPropertyName("includes_outside_trips")]
    public bool IncludesOutsideTrips { get; init; }

    [JsonPropertyName("sample_routes")]
    public TransitAccessGapSampleRoute[] SampleRoutes { get; init; } = Array.Empty<TransitAccessGapSampleRoute>();
}

public sealed class TransitAccessGapSampleRoute
{
    [JsonPropertyName("sample_index")]
    public int SampleIndex { get; init; }

    [JsonPropertyName("segment_count")]
    public int SegmentCount { get; init; }

    [JsonPropertyName("segments")]
    public TransitAccessGapRouteSegment[] Segments { get; init; } = Array.Empty<TransitAccessGapRouteSegment>();
}

public sealed class TransitAccessGapRouteSegment
{
    [JsonPropertyName("path_target_entity_index")]
    public int PathTargetEntityIndex { get; init; }

    [JsonPropertyName("path_target_entity_version")]
    public int PathTargetEntityVersion { get; init; }

    [JsonPropertyName("is_forward")]
    public bool? IsForward { get; init; }
}

public sealed class TransitAccessGapPosition
{
    [JsonPropertyName("x")]
    public double X { get; init; }

    [JsonPropertyName("y")]
    public double Y { get; init; }

    [JsonPropertyName("z")]
    public double Z { get; init; }
}

public sealed class TransitLinePressureSummary
{
    [JsonPropertyName("usage_observed_lines")]
    public int? UsageObservedLines { get; init; }

    [JsonPropertyName("capacity_observed_lines")]
    public int? CapacityObservedLines { get; init; }

    [JsonPropertyName("high_pressure_lines")]
    public int? HighPressureLines { get; init; }

    [JsonPropertyName("critical_pressure_lines")]
    public int? CriticalPressureLines { get; init; }

    [JsonPropertyName("top_pressure_lines")]
    public TransitPressureLineSummary[] TopPressureLines { get; init; } = Array.Empty<TransitPressureLineSummary>();
}

public sealed class TransitModePressureSummary
{
    [JsonPropertyName("average_usage_percent_by_mode")]
    public ModeDoubleValues AverageUsagePercentByMode { get; init; } = new();

    [JsonPropertyName("high_pressure_lines_by_mode")]
    public ModeEntityCounts HighPressureLinesByMode { get; init; } = new();

    [JsonPropertyName("usage_observed_lines_by_mode")]
    public ModeEntityCounts UsageObservedLinesByMode { get; init; } = new();
}

public sealed class TransitServiceGapSummary
{
    [JsonPropertyName("no_service_lines")]
    public int? NoServiceLines { get; init; }

    [JsonPropertyName("thin_service_lines")]
    public int? ThinServiceLines { get; init; }

    [JsonPropertyName("missing_usage_observed_lines")]
    public int? MissingUsageObservedLines { get; init; }

    [JsonPropertyName("missing_capacity_observed_lines")]
    public int? MissingCapacityObservedLines { get; init; }
}

public sealed class TransitPressureLineSummary
{
    [JsonPropertyName("line_entity_index")]
    public int LineEntityIndex { get; init; }

    [JsonPropertyName("line_entity_version")]
    public int LineEntityVersion { get; init; }

    [JsonPropertyName("line_name")]
    public string? LineName { get; init; }

    [JsonPropertyName("line_identifier")]
    public string? LineIdentifier { get; init; }

    [JsonPropertyName("mode")]
    public string Mode { get; init; } = "unknown";

    [JsonPropertyName("is_cargo")]
    public bool IsCargo { get; init; }

    [JsonPropertyName("active_vehicle_entities")]
    public int ActiveVehicleEntities { get; init; }

    [JsonPropertyName("onboard_passenger_entities")]
    public int? OnboardPassengerEntities { get; init; }

    [JsonPropertyName("total_passenger_capacity")]
    public int? TotalPassengerCapacity { get; init; }

    [JsonPropertyName("usage_percent")]
    public double? UsagePercent { get; init; }

    [JsonPropertyName("stops")]
    public int Stops { get; init; }

    [JsonPropertyName("length_m")]
    public double? LengthM { get; init; }
}

public sealed class SnapshotMeta
{
    [JsonPropertyName("source")]
    public string Source { get; init; } = "ecs_observed";

    [JsonPropertyName("notes")]
    public string[] Notes { get; init; } = Array.Empty<string>();

    [JsonPropertyName("metric_status")]
    public SortedDictionary<string, string> MetricStatus { get; init; } = new(StringComparer.Ordinal);
}
