using System;
using System.Collections.Generic;
using System.Globalization;

namespace CS2DataExport;

public interface IMetricProbe
{
    CitySummary CollectCitySummary();
    PopulationSummary CollectPopulationSummary();
    EducationSummary CollectEducationSummary();
    TransportProxySummary CollectTransportProxySummary();
    WorkforceSummary CollectWorkforceSummary();
    WorkplacesSummary CollectWorkplacesSummary();
    MobilitySummary CollectMobilitySummary();
    EconomySignalsSummary CollectEconomySignalsSummary();
    ExternalConnectionsSummary CollectExternalConnectionsSummary();
    LaborMarketDetailSummary CollectLaborMarketDetailSummary();
    FacilityIdentitySummary CollectFacilityIdentitySummary();
    CompanyServiceSemanticsSummary CollectCompanyServiceSemanticsSummary();
    HousingPressureSemanticsSummary CollectHousingPressureSemanticsSummary();
    HouseholdPressureContextSummary CollectHouseholdPressureContextSummary();
    LaborPressureContextSummary CollectLaborPressureContextSummary();
    TransitLineDetailSemanticsSummary CollectTransitLineDetailSemanticsSummary();
    TransitAccessGapSemanticsSummary CollectTransitAccessGapSemanticsSummary();
    OfficialCityStatisticsSummary CollectOfficialCityStatisticsSummary();
}

public sealed class DefaultMetricProbe : IMetricProbe
{
    public CitySummary CollectCitySummary()
    {
        return new CitySummary
        {
            Status = MetricStatus.Unavailable,
            Notes = new[]
            {
                "ECS city summary provider is not wired in this build.",
                "Set status to ok/partial when runtime ECS queries are available."
            }
        };
    }

    public PopulationSummary CollectPopulationSummary()
    {
        return new PopulationSummary
        {
            Status = MetricStatus.Unavailable,
            Notes = new[]
            {
                "Population ECS query provider is not wired in this build.",
                "Fallback preserves schema contract with unavailable values."
            }
        };
    }

    public EducationSummary CollectEducationSummary()
    {
        return new EducationSummary
        {
            Status = MetricStatus.Unavailable,
            Notes = new[]
            {
                "Education and employment ECS query provider is not wired in this build."
            }
        };
    }

    public TransportProxySummary CollectTransportProxySummary()
    {
        return new TransportProxySummary
        {
            Status = MetricStatus.Unavailable,
            Notes = new[]
            {
                "Transport summary provider is not wired in this build."
            }
        };
    }

    public WorkforceSummary CollectWorkforceSummary()
    {
        return new WorkforceSummary
        {
            Status = MetricStatus.Unavailable,
            Notes = new[]
            {
                "Workforce ECS query provider is not wired in this build."
            }
        };
    }

    public WorkplacesSummary CollectWorkplacesSummary()
    {
        return new WorkplacesSummary
        {
            Status = MetricStatus.Unavailable,
            Notes = new[]
            {
                "Workplaces ECS query provider is not wired in this build."
            }
        };
    }

    public MobilitySummary CollectMobilitySummary()
    {
        return new MobilitySummary
        {
            Status = MetricStatus.Unavailable,
            Notes = new[]
            {
                "Mobility ECS query provider is not wired in this build."
            }
        };
    }

    public EconomySignalsSummary CollectEconomySignalsSummary()
    {
        return new EconomySignalsSummary
        {
            Status = MetricStatus.Unavailable,
            Notes = new[]
            {
                "Economy signal provider is not wired in this build."
            }
        };
    }

    public ExternalConnectionsSummary CollectExternalConnectionsSummary()
    {
        return new ExternalConnectionsSummary
        {
            Status = MetricStatus.Unavailable,
            Notes = new[]
            {
                "External connections provider is not wired in this build."
            }
        };
    }

    public LaborMarketDetailSummary CollectLaborMarketDetailSummary()
    {
        return new LaborMarketDetailSummary
        {
            Status = MetricStatus.Unavailable,
            Notes = new[]
            {
                "Labor market detail provider is not wired in this build."
            }
        };
    }

    public FacilityIdentitySummary CollectFacilityIdentitySummary()
    {
        return new FacilityIdentitySummary
        {
            Status = MetricStatus.Unavailable,
            Notes = new[]
            {
                "Facility identity provider is not wired in this build."
            }
        };
    }

    public CompanyServiceSemanticsSummary CollectCompanyServiceSemanticsSummary()
    {
        return new CompanyServiceSemanticsSummary
        {
            Status = MetricStatus.Unavailable,
            Notes = new[]
            {
                "Company/service semantics provider is not wired in this build."
            }
        };
    }

    public HousingPressureSemanticsSummary CollectHousingPressureSemanticsSummary()
    {
        return new HousingPressureSemanticsSummary
        {
            Status = MetricStatus.Unavailable,
            Notes = new[]
            {
                "Housing pressure semantics provider is not wired in this build."
            }
        };
    }

    public HouseholdPressureContextSummary CollectHouseholdPressureContextSummary()
    {
        return new HouseholdPressureContextSummary
        {
            Status = MetricStatus.Unavailable,
            Notes = new[]
            {
                "Household pressure context provider is not wired in this build."
            }
        };
    }

    public LaborPressureContextSummary CollectLaborPressureContextSummary()
    {
        return new LaborPressureContextSummary
        {
            Status = MetricStatus.Unavailable,
            Notes = new[]
            {
                "Labor pressure context provider is not wired in this build."
            }
        };
    }

    public TransitAccessGapSemanticsSummary CollectTransitAccessGapSemanticsSummary()
    {
        return new TransitAccessGapSemanticsSummary
        {
            Status = MetricStatus.Unavailable,
            Notes = new[]
            {
                "Transit access gap provider is not wired in this build.",
                "Enable a completed capture window before expecting hotspot output."
            }
        };
    }

    public TransitLineDetailSemanticsSummary CollectTransitLineDetailSemanticsSummary()
    {
        return new TransitLineDetailSemanticsSummary
        {
            Status = MetricStatus.Unavailable,
            Notes = new[]
            {
                "Transit line detail provider is not wired in this build."
            }
        };
    }

    public OfficialCityStatisticsSummary CollectOfficialCityStatisticsSummary()
    {
        return new OfficialCityStatisticsSummary
        {
            Status = MetricStatus.Unavailable,
            Notes = new[]
            {
                "Official city statistics provider is not wired in this build."
            }
        };
    }
}

public sealed class MetricsCollector
{
    private readonly IMetricProbe _probe;

    public MetricsCollector(IMetricProbe? probe = null)
    {
        _probe = probe ?? new DefaultMetricProbe();
    }

    public CitySnapshotV1 CollectSnapshot(DateTimeOffset exportedAtUtc, string modVersion, string? gameBuild)
    {
        var city = SafeCollect(_probe.CollectCitySummary, CreateUnavailableCity);
        var population = SafeCollect(_probe.CollectPopulationSummary, CreateUnavailablePopulation);
        var education = SafeCollect(_probe.CollectEducationSummary, CreateUnavailableEducation);
        var transport = SafeCollect(_probe.CollectTransportProxySummary, CreateUnavailableTransportProxy);
        var workforce = SafeCollect(_probe.CollectWorkforceSummary, CreateUnavailableWorkforce);
        var workplaces = SafeCollect(_probe.CollectWorkplacesSummary, CreateUnavailableWorkplaces);
        var mobility = SafeCollect(_probe.CollectMobilitySummary, CreateUnavailableMobility);
        var economySignals = SafeCollect(_probe.CollectEconomySignalsSummary, CreateUnavailableEconomySignals);
        var externalConnections = SafeCollect(_probe.CollectExternalConnectionsSummary, CreateUnavailableExternalConnections);
        var laborMarketDetail = SafeCollect(_probe.CollectLaborMarketDetailSummary, CreateUnavailableLaborMarketDetail);
        var facilityIdentity = SafeCollect(_probe.CollectFacilityIdentitySummary, CreateUnavailableFacilityIdentity);
        var companyServiceSemantics = SafeCollect(_probe.CollectCompanyServiceSemanticsSummary, CreateUnavailableCompanyServiceSemantics);
        var housingPressureSemantics = SafeCollect(_probe.CollectHousingPressureSemanticsSummary, CreateUnavailableHousingPressureSemantics);
        var householdPressureContext = SafeCollect(_probe.CollectHouseholdPressureContextSummary, CreateUnavailableHouseholdPressureContext);
        var laborPressureContext = SafeCollect(_probe.CollectLaborPressureContextSummary, CreateUnavailableLaborPressureContext);
        var transitPerformanceSemantics = CreateTransitPerformanceSemantics(mobility);
        var transitLineDetailSemantics = SafeCollect(_probe.CollectTransitLineDetailSemanticsSummary, CreateUnavailableTransitLineDetailSemantics);
        var transitAccessGapSemantics = SafeCollect(_probe.CollectTransitAccessGapSemanticsSummary, CreateUnavailableTransitAccessGapSemantics);
        var officialCityStatistics = SafeCollect(
            _probe.CollectOfficialCityStatisticsSummary,
            CreateUnavailableOfficialCityStatistics);

        var metricStatus = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["city"] = city.Status,
            ["population"] = population.Status,
            ["education"] = education.Status,
            ["transport_proxies"] = transport.Status,
            ["workforce"] = workforce.Status,
            ["workplaces"] = workplaces.Status,
            ["mobility"] = mobility.Status,
            ["economy_signals"] = economySignals.Status,
            ["external_connections"] = externalConnections.Status,
            ["labor_market_detail"] = laborMarketDetail.Status,
            ["facility_identity"] = facilityIdentity.Status,
            ["company_service_semantics"] = companyServiceSemantics.Status,
            ["housing_pressure_semantics"] = housingPressureSemantics.Status,
            ["household_pressure_context"] = householdPressureContext.Status,
            ["labor_pressure_context"] = laborPressureContext.Status,
            ["transit_performance_semantics"] = transitPerformanceSemantics.Status,
            ["transit_line_detail_semantics"] = transitLineDetailSemantics.Status,
            ["transit_access_gap_semantics"] = transitAccessGapSemantics.Status,
            ["official_city_statistics"] = officialCityStatistics.Status
        };

        var metaNotes = BuildMetaNotes(metricStatus);

        return new CitySnapshotV1
        {
            SchemaVersion = "2.7.0",
            ExportedAtUtc = exportedAtUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
            GameBuild = gameBuild,
            ModVersion = modVersion,
            City = city,
            Population = population,
            Education = education,
            TransportProxies = transport,
            Workforce = workforce,
            Workplaces = workplaces,
            Mobility = mobility,
            EconomySignals = economySignals,
            ExternalConnections = externalConnections,
            LaborMarketDetail = laborMarketDetail,
            FacilityIdentity = facilityIdentity,
            CompanyServiceSemantics = companyServiceSemantics,
            HousingPressureSemantics = housingPressureSemantics,
            HouseholdPressureContext = householdPressureContext,
            LaborPressureContext = laborPressureContext,
            TransitPerformanceSemantics = transitPerformanceSemantics,
            TransitLineDetailSemantics = transitLineDetailSemantics,
            TransitAccessGapSemantics = transitAccessGapSemantics,
            OfficialCityStatistics = officialCityStatistics,
            Meta = new SnapshotMeta
            {
                Source = "ecs_observed",
                Notes = metaNotes,
                MetricStatus = metricStatus
            }
        };
    }

    private static T SafeCollect<T>(Func<T> collector, Func<Exception, T> fallbackFactory)
    {
        try
        {
            return collector();
        }
        catch (Exception ex)
        {
            return fallbackFactory(ex);
        }
    }

    private static string[] BuildMetaNotes(IReadOnlyDictionary<string, string> metricStatus)
    {
        var notes = new List<string>
        {
            "schema 2.7.0 exports observed and derived metrics only."
        };

        foreach (var pair in metricStatus)
        {
            if (pair.Value == MetricStatus.Ok)
            {
                continue;
            }

            notes.Add($"group '{pair.Key}' exported with status '{pair.Value}'.");
        }

        return notes.ToArray();
    }

    private static CitySummary CreateUnavailableCity(Exception exception)
    {
        return new CitySummary
        {
            Status = MetricStatus.Partial,
            Notes = new[] { $"city summary probe failed: {exception.Message}" }
        };
    }

    private static PopulationSummary CreateUnavailablePopulation(Exception exception)
    {
        return new PopulationSummary
        {
            Status = MetricStatus.Partial,
            Notes = new[] { $"population probe failed: {exception.Message}" }
        };
    }

    private static EducationSummary CreateUnavailableEducation(Exception exception)
    {
        return new EducationSummary
        {
            Status = MetricStatus.Partial,
            Notes = new[] { $"education probe failed: {exception.Message}" }
        };
    }

    private static TransportProxySummary CreateUnavailableTransportProxy(Exception exception)
    {
        return new TransportProxySummary
        {
            Status = MetricStatus.Partial,
            Notes = new[] { $"transport proxy probe failed: {exception.Message}" }
        };
    }

    private static WorkforceSummary CreateUnavailableWorkforce(Exception exception)
    {
        return new WorkforceSummary
        {
            Status = MetricStatus.Partial,
            Notes = new[] { $"workforce probe failed: {exception.Message}" }
        };
    }

    private static WorkplacesSummary CreateUnavailableWorkplaces(Exception exception)
    {
        return new WorkplacesSummary
        {
            Status = MetricStatus.Partial,
            Notes = new[] { $"workplaces probe failed: {exception.Message}" }
        };
    }

    private static MobilitySummary CreateUnavailableMobility(Exception exception)
    {
        return new MobilitySummary
        {
            Status = MetricStatus.Partial,
            Notes = new[] { $"mobility probe failed: {exception.Message}" }
        };
    }

    private static EconomySignalsSummary CreateUnavailableEconomySignals(Exception exception)
    {
        return new EconomySignalsSummary
        {
            Status = MetricStatus.Partial,
            Notes = new[] { $"economy signals probe failed: {exception.Message}" }
        };
    }

    private static ExternalConnectionsSummary CreateUnavailableExternalConnections(Exception exception)
    {
        return new ExternalConnectionsSummary
        {
            Status = MetricStatus.Partial,
            Notes = new[] { $"external connections probe failed: {exception.Message}" }
        };
    }

    private static LaborMarketDetailSummary CreateUnavailableLaborMarketDetail(Exception exception)
    {
        return new LaborMarketDetailSummary
        {
            Status = MetricStatus.Partial,
            Notes = new[] { $"labor market detail probe failed: {exception.Message}" }
        };
    }

    private static FacilityIdentitySummary CreateUnavailableFacilityIdentity(Exception exception)
    {
        return new FacilityIdentitySummary
        {
            Status = MetricStatus.Partial,
            Notes = new[] { $"facility identity probe failed: {exception.Message}" }
        };
    }

    private static CompanyServiceSemanticsSummary CreateUnavailableCompanyServiceSemantics(Exception exception)
    {
        return new CompanyServiceSemanticsSummary
        {
            Status = MetricStatus.Partial,
            Notes = new[] { $"company/service semantics probe failed: {exception.Message}" }
        };
    }

    private static HousingPressureSemanticsSummary CreateUnavailableHousingPressureSemantics(Exception exception)
    {
        return new HousingPressureSemanticsSummary
        {
            Status = MetricStatus.Partial,
            Notes = new[] { $"housing pressure semantics probe failed: {exception.Message}" }
        };
    }

    private static HouseholdPressureContextSummary CreateUnavailableHouseholdPressureContext(Exception exception)
    {
        return new HouseholdPressureContextSummary
        {
            Status = MetricStatus.Partial,
            Notes = new[] { $"household pressure context probe failed: {exception.Message}" }
        };
    }

    private static LaborPressureContextSummary CreateUnavailableLaborPressureContext(Exception exception)
    {
        return new LaborPressureContextSummary
        {
            Status = MetricStatus.Partial,
            Notes = new[] { $"labor pressure context probe failed: {exception.Message}" }
        };
    }

    private static TransitAccessGapSemanticsSummary CreateUnavailableTransitAccessGapSemantics(Exception exception)
    {
        return new TransitAccessGapSemanticsSummary
        {
            Status = MetricStatus.Partial,
            Notes = new[] { $"transit access gap semantics probe failed: {exception.Message}" }
        };
    }

    private static TransitLineDetailSemanticsSummary CreateUnavailableTransitLineDetailSemantics(Exception exception)
    {
        return new TransitLineDetailSemanticsSummary
        {
            Status = MetricStatus.Partial,
            Notes = new[] { $"transit line detail semantics probe failed: {exception.Message}" }
        };
    }

    private static OfficialCityStatisticsSummary CreateUnavailableOfficialCityStatistics(Exception exception)
    {
        return new OfficialCityStatisticsSummary
        {
            Status = MetricStatus.Partial,
            Notes = new[] { $"official city statistics probe failed: {exception.Message}" }
        };
    }

    private static TransitPerformanceSemanticsSummary CreateTransitPerformanceSemantics(MobilitySummary mobility)
    {
        if (mobility.Status == MetricStatus.Unavailable)
        {
            return new TransitPerformanceSemanticsSummary
            {
                Status = MetricStatus.Unavailable,
                Notes = new[]
                {
                    "transit performance semantics unavailable because mobility lines are unavailable."
                }
            };
        }

        MobilityLineRecord[] lines = mobility.Lines ?? Array.Empty<MobilityLineRecord>();
        if (lines.Length == 0)
        {
            return new TransitPerformanceSemanticsSummary
            {
                Status = MetricStatus.Partial,
                Notes = new[]
                {
                    "transit performance semantics unavailable because mobility exported no line records.",
                    "derive this group only from mobility.lines[]; do not fall back to transport-building guesses."
                }
            };
        }

        const double highPressureThresholdPercent = 75.0;
        const double criticalPressureThresholdPercent = 90.0;

        int usageObservedLines = 0;
        int capacityObservedLines = 0;
        int highPressureLines = 0;
        int criticalPressureLines = 0;
        int noServiceLines = 0;
        int thinServiceLines = 0;
        int missingUsageObservedLines = 0;
        int missingCapacityObservedLines = 0;

        var highPressureLinesByMode = CreateModeCounterDictionary();
        var usageObservedLinesByMode = CreateModeCounterDictionary();
        var usageValuesByMode = CreateModeValueDictionary();
        var topPressureLines = new List<TransitPressureLineSummary>();

        for (int i = 0; i < lines.Length; i++)
        {
            MobilityLineRecord line = lines[i];
            string mode = NormalizeModeKey(line.Mode);

            if (line.ActiveVehicleEntities <= 0)
            {
                noServiceLines++;
            }
            else if (line.ActiveVehicleEntities <= 1)
            {
                thinServiceLines++;
            }

            if (!line.UsagePercent.HasValue)
            {
                missingUsageObservedLines++;
            }
            else
            {
                usageObservedLines++;
                usageObservedLinesByMode[mode]++;
                usageValuesByMode[mode].Add(line.UsagePercent.Value);

                if (line.UsagePercent.Value >= highPressureThresholdPercent)
                {
                    highPressureLines++;
                    highPressureLinesByMode[mode]++;
                }

                if (line.UsagePercent.Value >= criticalPressureThresholdPercent)
                {
                    criticalPressureLines++;
                }

                topPressureLines.Add(
                    new TransitPressureLineSummary
                    {
                        LineEntityIndex = line.LineEntityIndex,
                        LineEntityVersion = line.LineEntityVersion,
                        LineName = line.LineName,
                        LineIdentifier = line.LineIdentifier,
                        Mode = mode,
                        IsCargo = line.IsCargo,
                        ActiveVehicleEntities = line.ActiveVehicleEntities,
                        OnboardPassengerEntities = line.OnboardPassengerEntities,
                        TotalPassengerCapacity = line.TotalPassengerCapacity,
                        UsagePercent = line.UsagePercent,
                        Stops = line.Stops,
                        LengthM = line.LengthM
                    });
            }

            if (line.TotalPassengerCapacity.HasValue)
            {
                capacityObservedLines++;
            }
            else
            {
                missingCapacityObservedLines++;
            }
        }

        topPressureLines.Sort(
            static (left, right) =>
            {
                int usageComparison = Nullable.Compare(right.UsagePercent, left.UsagePercent);
                if (usageComparison != 0)
                {
                    return usageComparison;
                }

                int vehicleComparison = right.ActiveVehicleEntities.CompareTo(left.ActiveVehicleEntities);
                if (vehicleComparison != 0)
                {
                    return vehicleComparison;
                }

                return left.LineEntityIndex.CompareTo(right.LineEntityIndex);
            });

        if (topPressureLines.Count > 5)
        {
            topPressureLines.RemoveRange(5, topPressureLines.Count - 5);
        }

        var notes = new List<string>
        {
            "transit performance semantics are derived from live mobility line records, not transport-building carriers.",
            "high_pressure_lines use usage_percent >= 75; critical_pressure_lines use usage_percent >= 90.",
            "thin_service_lines count lines with exactly one active vehicle; no_service_lines count lines with zero active vehicles.",
            "missing usage or capacity observability reflects current line-record coverage and should not be treated as proof of bad service by itself."
        };

        if (missingUsageObservedLines > 0 || missingCapacityObservedLines > 0)
        {
            notes.Add("some lines still lack complete capacity or usage observability, especially on carrier families that do not expose passenger-capacity cleanly.");
        }

        return new TransitPerformanceSemanticsSummary
        {
            Status = MetricStatus.Ok,
            LinePressure = new TransitLinePressureSummary
            {
                UsageObservedLines = usageObservedLines,
                CapacityObservedLines = capacityObservedLines,
                HighPressureLines = highPressureLines,
                CriticalPressureLines = criticalPressureLines,
                TopPressureLines = topPressureLines.ToArray()
            },
            ModePressure = new TransitModePressureSummary
            {
                AverageUsagePercentByMode = CreateModeDoubleValuesFromLists(usageValuesByMode),
                HighPressureLinesByMode = CreateModeEntityCountsFromDictionary(highPressureLinesByMode),
                UsageObservedLinesByMode = CreateModeEntityCountsFromDictionary(usageObservedLinesByMode)
            },
            ServiceGaps = new TransitServiceGapSummary
            {
                NoServiceLines = noServiceLines,
                ThinServiceLines = thinServiceLines,
                MissingUsageObservedLines = missingUsageObservedLines,
                MissingCapacityObservedLines = missingCapacityObservedLines
            },
            SourceComponent = "ecs.transit_performance_semantics:Game.UI.InGame.TransportUIUtils|Game.Routes.TransportLine|Game.Vehicles.PublicTransport",
            Notes = notes.ToArray()
        };
    }

    private static Dictionary<string, int> CreateModeCounterDictionary()
    {
        return new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["bus"] = 0,
            ["tram"] = 0,
            ["subway"] = 0,
            ["train"] = 0,
            ["ship"] = 0,
            ["ferry"] = 0,
            ["air"] = 0,
            ["taxi"] = 0,
            ["unknown"] = 0
        };
    }

    private static Dictionary<string, List<double>> CreateModeValueDictionary()
    {
        return new Dictionary<string, List<double>>(StringComparer.Ordinal)
        {
            ["bus"] = new List<double>(),
            ["tram"] = new List<double>(),
            ["subway"] = new List<double>(),
            ["train"] = new List<double>(),
            ["ship"] = new List<double>(),
            ["ferry"] = new List<double>(),
            ["air"] = new List<double>(),
            ["taxi"] = new List<double>(),
            ["unknown"] = new List<double>()
        };
    }

    private static string NormalizeModeKey(string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
        {
            return "unknown";
        }

        string normalized = mode!.Trim().ToLowerInvariant();
        return normalized switch
        {
            "bus" => "bus",
            "tram" => "tram",
            "subway" => "subway",
            "train" => "train",
            "ship" => "ship",
            "ferry" => "ferry",
            "air" => "air",
            "taxi" => "taxi",
            _ => "unknown"
        };
    }

    private static ModeEntityCounts CreateModeEntityCountsFromDictionary(IReadOnlyDictionary<string, int> values)
    {
        return new ModeEntityCounts
        {
            Bus = GetModeInt(values, "bus"),
            Tram = GetModeInt(values, "tram"),
            Subway = GetModeInt(values, "subway"),
            Train = GetModeInt(values, "train"),
            Ship = GetModeInt(values, "ship"),
            Ferry = GetModeInt(values, "ferry"),
            Air = GetModeInt(values, "air"),
            Taxi = GetModeInt(values, "taxi"),
            Unknown = GetModeInt(values, "unknown")
        };
    }

    private static ModeDoubleValues CreateModeDoubleValuesFromLists(IReadOnlyDictionary<string, List<double>> values)
    {
        return new ModeDoubleValues
        {
            Bus = AverageMode(values, "bus"),
            Tram = AverageMode(values, "tram"),
            Subway = AverageMode(values, "subway"),
            Train = AverageMode(values, "train"),
            Ship = AverageMode(values, "ship"),
            Ferry = AverageMode(values, "ferry"),
            Air = AverageMode(values, "air"),
            Taxi = AverageMode(values, "taxi"),
            Unknown = AverageMode(values, "unknown")
        };
    }

    private static int? GetModeInt(IReadOnlyDictionary<string, int> values, string key)
    {
        return values.TryGetValue(key, out int value) ? value : null;
    }

    private static double? AverageMode(IReadOnlyDictionary<string, List<double>> values, string key)
    {
        if (!values.TryGetValue(key, out List<double>? modeValues) || modeValues.Count == 0)
        {
            return null;
        }

        double total = 0;
        for (int i = 0; i < modeValues.Count; i++)
        {
            total += modeValues[i];
        }

        return Math.Round(total / modeValues.Count, 2, MidpointRounding.AwayFromZero);
    }
}
