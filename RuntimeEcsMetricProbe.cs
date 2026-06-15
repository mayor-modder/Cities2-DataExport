using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Game.City;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Simulation;
using Game.Tools;
using Game.UI;
using Game.UI.InGame;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace CS2DataExport;

public sealed class RuntimeEcsMetricProbe : IMetricProbe
{
    private static readonly string[] s_cityBuildingCandidates =
    {
        "Game.Buildings.Building",
        "Game.Objects.Building"
    };

    private static readonly string[] s_cityDistrictCandidates =
    {
        "Game.Areas.District",
        "Game.Areas.DistrictArea"
    };

    private static readonly string[] s_residentialBuildingFamilyCandidates =
    {
        "Game.Buildings.ResidentialProperty",
        "Game.Buildings.ResidentialBuilding"
    };

    private static readonly string[] s_transportBuildingFamilyCandidates =
    {
        "Game.Buildings.TransportStation",
        "Game.Buildings.PublicTransportStation",
        "Game.Buildings.TransportDepot",
        "Game.Buildings.PublicTransportDepot",
        "Game.Buildings.CargoTransportStation",
        "Game.Buildings.CargoTransportDepot"
    };

    private static readonly string[] s_populationCitizenCandidates =
    {
        "Game.Citizens.Citizen"
    };

    private static readonly string[] s_populationHouseholdCandidates =
    {
        "Game.Citizens.Household"
    };

    private static readonly string[] s_populationWorkerCandidates =
    {
        "Game.Citizens.Worker"
    };

    private static readonly string[] s_transportRoadVehicleCandidates =
    {
        "Game.Vehicles.Vehicle",
        "Game.Vehicles.Car",
        "Game.Vehicles.Truck",
        "Game.Vehicles.PersonalCar"
    };

    private static readonly string[] s_transportPublicVehicleCandidates =
    {
        "Game.Vehicles.PublicTransport",
        "Game.Vehicles.Bus",
        "Game.Vehicles.Tram",
        "Game.Vehicles.Train",
        "Game.Vehicles.SubwayTrain",
        "Game.Vehicles.Taxi",
        "Game.Vehicles.Ship",
        "Game.Vehicles.Airplane",
        "Game.Vehicles.Helicopter"
    };

    private static readonly string[] s_transportLineCandidates =
    {
        "Game.Routes.TransportLine",
        "Game.Transport.TransportLine"
    };

    private static readonly string[] s_passengerLineCandidates =
    {
        "Game.Routes.TransportLine",
        "Game.Transport.TransportLine"
    };

    private static readonly string[] s_cargoLineCandidates =
    {
        "Game.Routes.CargoTransportLine",
        "Game.Routes.CargoLine",
        "Game.Transport.CargoLine"
    };

    private static readonly string[] s_transportCongestionCandidates =
    {
        "Game.Vehicles.TrafficJam",
        "Game.Vehicles.Congestion"
    };

    private static readonly string[] s_modeBusCandidates = { "Game.Vehicles.Bus" };
    private static readonly string[] s_modeTramCandidates = { "Game.Vehicles.Tram" };
    private static readonly string[] s_modeSubwayCandidates = { "Game.Vehicles.SubwayTrain" };
    private static readonly string[] s_modeTrainCandidates = { "Game.Vehicles.Train" };
    private static readonly string[] s_modeShipCandidates = { "Game.Vehicles.Ship" };
    private static readonly string[] s_modeAirCandidates = { "Game.Vehicles.Airplane", "Game.Vehicles.Helicopter" };
    private static readonly string[] s_modeTaxiCandidates = { "Game.Vehicles.Taxi" };
    private static readonly string[] s_transportLineDataCandidates =
    {
        "Game.Prefabs.TransportLineData",
        "Game.Prefabs.PublicTransportLineData",
        "Game.Routes.TransportLineData",
        "Game.Transport.TransportLineData"
    };

    private static readonly string[] s_transportLineNameCandidates =
    {
        "Game.Common.Name",
        "Game.Routes.TransportLineName",
        "Game.Transport.TransportLineName",
        "Game.Routes.RouteName",
        "Game.Transport.RouteName"
    };

    private static readonly string[] s_transportTypeEnumCandidates =
    {
        "Game.Prefabs.TransportType",
        "Game.Transport.TransportType"
    };

    private static readonly string[] s_vehiclePassengerBufferCandidates =
    {
        "Game.Vehicles.Passenger",
        "Game.Vehicles.PassengerData",
        "Game.Vehicles.PassengerElement"
    };

    private static readonly string[] s_vehicleLineOwnerReferenceCandidates =
    {
        "Game.Common.Owner",
        "Game.Common.Target"
    };

    private static readonly string[] s_creatureCurrentVehicleCandidates =
    {
        "Game.Creatures.CurrentVehicle",
        "Game.Citizens.CurrentVehicle"
    };

    private const string kUnknownTransportMode = "unknown";
    private const double kDefaultTicksPerDay = 262144.0;
    private const Resource kLeisureResources = Resource.Meals | Resource.Entertainment | Resource.Recreation | Resource.Lodging;
    private const Resource kOfficeResources = Resource.Software | Resource.Telecom | Resource.Financial | Resource.Media;

    private static readonly object s_typeCacheLock = new();
    private static readonly Dictionary<string, Type?> s_componentTypeCache = new(StringComparer.Ordinal);
    private static readonly MethodInfo? s_entityManagerHasComponentMethod = FindEntityManagerGenericMethod("HasComponent", minParameterCount: 1, maxParameterCount: 1);
    private static readonly MethodInfo? s_entityManagerGetComponentDataMethod = FindEntityManagerGenericMethod("GetComponentData", minParameterCount: 1, maxParameterCount: 1);
    private static readonly MethodInfo? s_entityManagerGetBufferMethod = FindEntityManagerGenericMethod("GetBuffer", minParameterCount: 1, maxParameterCount: 2);
    private static readonly MethodInfo? s_componentTypeGetManagedTypeMethod = typeof(ComponentType).GetMethod("GetManagedType", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
    private static readonly PropertyInfo? s_componentTypeManagedTypeProperty = typeof(ComponentType).GetProperty("Type", BindingFlags.Instance | BindingFlags.Public);

    private readonly Func<EntityManager?> _getEntityManager;
    private readonly Func<World?> _getWorld;
    private readonly ProbeSamplingOptions _sampling;
    private readonly TransitAccessGapCaptureCoordinator? _transitAccessGapCaptureCoordinator;

    public sealed class ProbeSamplingOptions
    {
        public int MaxPopulationEntities { get; init; } = 120000;
        public int MaxWorkplaceEntities { get; init; } = 70000;
        public int MaxHouseholdEntities { get; init; } = 120000;
    }

    public RuntimeEcsMetricProbe(
        Func<EntityManager?> getEntityManager,
        Func<World?>? getWorld = null,
        Action<string>? log = null,
        ProbeSamplingOptions? sampling = null,
        TransitAccessGapCaptureCoordinator? transitAccessGapCaptureCoordinator = null)
    {
        _getEntityManager = getEntityManager;
        _getWorld = getWorld ?? (() => null);
        _sampling = sampling ?? new ProbeSamplingOptions();
        _transitAccessGapCaptureCoordinator = transitAccessGapCaptureCoordinator;
        _ = log;
    }

    private sealed record OfficialCitySingletonValues(
        int? PopulationWithMoveIn,
        int? CurrentTourists,
        int? AverageTourists,
        int? Attractiveness,
        int? DevTreePoints);

    public CitySummary CollectCitySummary()
    {
        if (!TryGetEntityManager(out EntityManager entityManager, out string availabilityReason))
        {
            return new CitySummary
            {
                Status = MetricStatus.Unavailable,
                Notes = new[] { availabilityReason }
            };
        }

        CountResult buildingCount = TryCountByAll(entityManager, s_cityBuildingCandidates);
        CountResult districtCount = TryCountByAll(entityManager, s_cityDistrictCandidates);

        var notes = new List<string>
        {
            "live ECS counts resolved from runtime component queries."
        };

        AddResultNotes(notes, "building_count", buildingCount);
        AddResultNotes(notes, "district_count", districtCount);

        return new CitySummary
        {
            Status = ComputeStatus(
                availableMetrics: CountPresent(buildingCount.Count) + CountPresent(districtCount.Count),
                expectedMetrics: 2),
            CityName = null,
            BuildingCount = buildingCount.Count,
            DistrictCount = districtCount.Count,
            SourceComponent = BuildSourceComponent("ecs.city", buildingCount, districtCount),
            Notes = notes.ToArray()
        };
    }

    public PopulationSummary CollectPopulationSummary()
    {
        if (!TryGetEntityManager(out EntityManager entityManager, out string availabilityReason))
        {
            return new PopulationSummary
            {
                Status = MetricStatus.Unavailable,
                Notes = new[] { availabilityReason }
            };
        }

        CountResult citizenCount = TryCountByAll(entityManager, s_populationCitizenCandidates);
        CountResult householdCount = TryCountByAll(entityManager, s_populationHouseholdCandidates);
        bool hasDetailedScan = TryScanPopulationAndWorkforce(entityManager, out PopulationWorkforceScanResult scan, out string? scanError);

        var notes = new List<string>
        {
            "live ECS counts resolved from runtime component queries."
        };

        AddResultNotes(notes, "total_population", citizenCount);
        AddResultNotes(notes, "household_count", householdCount);

        if (hasDetailedScan)
        {
            notes.Add("population detail scan includes local/tourist/commuter and age/workforce proxy distributions.");
            if (scan.WasSampled)
            {
                notes.Add("population detail scan used sampling guardrails for large-entity coverage; values are scaled estimates.");
            }
        }
        else
        {
            notes.Add("population detail scan unavailable: " + (scanError ?? "unknown error"));
        }

        notes.Add("birth/death rates are not currently exposed by a stable ECS component contract in this build.");

        int availableMetrics = CountPresent(citizenCount.Count) + CountPresent(householdCount.Count) + (hasDetailedScan ? 1 : 0);

        return new PopulationSummary
        {
            Status = ComputeStatus(availableMetrics: availableMetrics, expectedMetrics: 3),
            TotalPopulation = citizenCount.Count,
            HouseholdCount = householdCount.Count,
            BirthRatePerMonth = null,
            DeathRatePerMonth = null,
            LocalPopulation = hasDetailedScan ? scan.LocalPopulation : null,
            TouristPopulation = hasDetailedScan ? scan.TouristPopulation : null,
            CommuterPopulation = hasDetailedScan ? scan.CommuterPopulation : null,
            MovingAwayPopulation = hasDetailedScan ? scan.MovingAwayPopulation : null,
            HomelessPopulation = hasDetailedScan ? scan.HomelessPopulation : null,
            WorkingAgePopulation = hasDetailedScan ? scan.WorkingAgePopulation : null,
            ChildrenPopulation = hasDetailedScan ? scan.ChildrenPopulation : null,
            ElderlyPopulation = hasDetailedScan ? scan.ElderlyPopulation : null,
            SourceComponent = BuildSourceComponent("ecs.population", citizenCount, householdCount),
            Notes = notes.ToArray()
        };
    }

    public TransitAccessGapSemanticsSummary CollectTransitAccessGapSemanticsSummary()
    {
        if (_transitAccessGapCaptureCoordinator != null &&
            _transitAccessGapCaptureCoordinator.TryGetCompletedSummary(out TransitAccessGapSemanticsSummary summary))
        {
            return summary;
        }

        return new TransitAccessGapSemanticsSummary
        {
            Status = MetricStatus.Unavailable,
            Notes = new[]
            {
                "capture mode disabled; no transit trip window recorded"
            }
        };
    }

    public OfficialCityStatisticsSummary CollectOfficialCityStatisticsSummary()
    {
        if (!TryGetEntityManager(out EntityManager entityManager, out string entityManagerReason))
        {
            return new OfficialCityStatisticsSummary
            {
                Status = MetricStatus.Unavailable,
                Notes = new[] { entityManagerReason }
            };
        }

        World? world = _getWorld();
        if (world == null || !world.IsCreated)
        {
            return new OfficialCityStatisticsSummary
            {
                Status = MetricStatus.Unavailable,
                Notes = new[] { "runtime World is unavailable; official managed game systems cannot be resolved." }
            };
        }

        var notes = new List<string>
        {
            "official city statistics are aggregate runtime counters from managed game systems and city singleton components."
        };

        CityStatisticsSystem? statistics = world.GetExistingSystemManaged<CityStatisticsSystem>();
        CitySystem? citySystem = world.GetExistingSystemManaged<CitySystem>();
        TimeSystem? timeSystem = world.GetExistingSystemManaged<TimeSystem>();
        SimulationSystem? simulationSystem = world.GetExistingSystemManaged<SimulationSystem>();

        if (statistics == null)
        {
            notes.Add("CityStatisticsSystem is unavailable.");
            return new OfficialCityStatisticsSummary
            {
                Status = MetricStatus.Unavailable,
                Notes = notes.ToArray(),
                SourceComponent = "official.city_statistics:missing.CityStatisticsSystem"
            };
        }

        if (citySystem == null)
        {
            notes.Add("CitySystem is unavailable; money is null.");
        }

        if (timeSystem == null)
        {
            notes.Add("TimeSystem is unavailable; game date and days_per_year are null.");
        }

        if (simulationSystem == null)
        {
            notes.Add("SimulationSystem is unavailable; game_tick is null.");
        }

        OfficialCitySingletonValues singletonValues = ReadOfficialCitySingletonValues(entityManager, notes);
        DateTime? gameDate = timeSystem?.GetCurrentDateTime();

        int availableSystemCount = 1
            + (citySystem == null ? 0 : 1)
            + (timeSystem == null ? 0 : 1)
            + (simulationSystem == null ? 0 : 1);

        return new OfficialCityStatisticsSummary
        {
            Status = availableSystemCount == 4 ? MetricStatus.Ok : MetricStatus.Partial,
            Notes = notes.ToArray(),
            SourceComponent = "official.city_statistics:Game.City.CityStatisticsSystem|Game.City.CitySystem|Game.Simulation.TimeSystem|Game.Simulation.SimulationSystem|Game.City.Population|Game.City.Tourism|Game.City.DevTreePoints",
            Time = new OfficialTimeStatistics
            {
                GameTick = simulationSystem == null ? null : (ulong?)simulationSystem.frameIndex,
                GameYear = gameDate?.Year,
                GameMonth = gameDate?.Month,
                GameDay = gameDate?.Day,
                DaysPerYear = timeSystem?.daysPerYear,
                SampleCount = statistics.sampleCount,
                KUpdatesPerDay = CityStatisticsSystem.kUpdatesPerDay,
                KTicksPerDay = TimeSystem.kTicksPerDay
            },
            Finance = new OfficialFinanceStatistics
            {
                Money = citySystem?.moneyAmount,
                Income = GetOfficialStatistic(statistics, StatisticType.Income),
                Expense = GetOfficialStatistic(statistics, StatisticType.Expense),
                Trade = GetOfficialStatistic(statistics, StatisticType.Trade)
            },
            Taxes = new OfficialTaxStatistics
            {
                ResidentialTaxableIncome = GetOfficialStatistic(statistics, StatisticType.ResidentialTaxableIncome),
                CommercialTaxableIncome = GetOfficialStatistic(statistics, StatisticType.CommercialTaxableIncome),
                IndustrialTaxableIncome = GetOfficialStatistic(statistics, StatisticType.IndustrialTaxableIncome),
                OfficeTaxableIncome = GetOfficialStatistic(statistics, StatisticType.OfficeTaxableIncome)
            },
            PopulationFlow = new OfficialPopulationFlowStatistics
            {
                Population = GetOfficialStatistic(statistics, StatisticType.Population),
                PopulationWithMoveIn = singletonValues.PopulationWithMoveIn,
                CitizensMovedIn = GetOfficialStatistic(statistics, StatisticType.CitizensMovedIn),
                CitizensMovedAway = GetOfficialStatistic(statistics, StatisticType.CitizensMovedAway),
                BirthRate = GetOfficialStatistic(statistics, StatisticType.BirthRate),
                DeathRate = GetOfficialStatistic(statistics, StatisticType.DeathRate)
            },
            Social = new OfficialSocialStatistics
            {
                Wellbeing = GetOfficialStatistic(statistics, StatisticType.Wellbeing),
                Health = GetOfficialStatistic(statistics, StatisticType.Health),
                WellbeingLevel = GetOfficialStatistic(statistics, StatisticType.WellbeingLevel),
                HealthLevel = GetOfficialStatistic(statistics, StatisticType.HealthLevel),
                HomelessCount = GetOfficialStatistic(statistics, StatisticType.HomelessCount),
                CrimeRate = GetOfficialStatistic(statistics, StatisticType.CrimeRate),
                CrimeCount = GetOfficialStatistic(statistics, StatisticType.CrimeCount),
                EscapedArrestCount = GetOfficialStatistic(statistics, StatisticType.EscapedArrestCount),
                CollectedMail = GetOfficialStatistic(statistics, StatisticType.CollectedMail),
                DeliveredMail = GetOfficialStatistic(statistics, StatisticType.DeliveredMail)
            },
            Tourism = new OfficialTourismStatistics
            {
                TouristCount = GetOfficialStatistic(statistics, StatisticType.TouristCount),
                TouristIncome = GetOfficialStatistic(statistics, StatisticType.TouristIncome),
                LodgingUsed = GetOfficialStatistic(statistics, StatisticType.LodgingUsed),
                LodgingTotal = GetOfficialStatistic(statistics, StatisticType.LodgingTotal),
                CurrentTourists = singletonValues.CurrentTourists,
                AverageTourists = singletonValues.AverageTourists,
                Attractiveness = singletonValues.Attractiveness
            },
            TransportTotals = new OfficialTransportTotalsStatistics
            {
                PassengerCountBus = GetOfficialStatistic(statistics, StatisticType.PassengerCountBus),
                PassengerCountSubway = GetOfficialStatistic(statistics, StatisticType.PassengerCountSubway),
                PassengerCountTrain = GetOfficialStatistic(statistics, StatisticType.PassengerCountTrain),
                PassengerCountTram = GetOfficialStatistic(statistics, StatisticType.PassengerCountTram),
                PassengerCountAirplane = GetOfficialStatistic(statistics, StatisticType.PassengerCountAirplane),
                PassengerCountTaxi = GetOfficialStatistic(statistics, StatisticType.PassengerCountTaxi),
                PassengerCountShip = GetOfficialStatistic(statistics, StatisticType.PassengerCountShip),
                CargoCountTruck = GetOfficialStatistic(statistics, StatisticType.CargoCountTruck),
                CargoCountTrain = GetOfficialStatistic(statistics, StatisticType.CargoCountTrain),
                CargoCountShip = GetOfficialStatistic(statistics, StatisticType.CargoCountShip),
                CargoCountAirplane = GetOfficialStatistic(statistics, StatisticType.CargoCountAirplane)
            },
            Sectors = new OfficialSectorStatistics
            {
                Service = new OfficialSectorMetric
                {
                    Wealth = GetOfficialStatistic(statistics, StatisticType.ServiceWealth),
                    Count = GetOfficialStatistic(statistics, StatisticType.ServiceCount),
                    Workers = GetOfficialStatistic(statistics, StatisticType.ServiceWorkers),
                    MaxWorkers = GetOfficialStatistic(statistics, StatisticType.ServiceMaxWorkers)
                },
                Processing = new OfficialSectorMetric
                {
                    Wealth = GetOfficialStatistic(statistics, StatisticType.ProcessingWealth),
                    Count = GetOfficialStatistic(statistics, StatisticType.ProcessingCount),
                    Workers = GetOfficialStatistic(statistics, StatisticType.ProcessingWorkers),
                    MaxWorkers = GetOfficialStatistic(statistics, StatisticType.ProcessingMaxWorkers)
                },
                Office = new OfficialSectorMetric
                {
                    Wealth = GetOfficialStatistic(statistics, StatisticType.OfficeWealth),
                    Count = GetOfficialStatistic(statistics, StatisticType.OfficeCount),
                    Workers = GetOfficialStatistic(statistics, StatisticType.OfficeWorkers),
                    MaxWorkers = GetOfficialStatistic(statistics, StatisticType.OfficeMaxWorkers)
                }
            },
            CityServices = new OfficialCityServiceStatistics
            {
                CityServiceWorkers = GetOfficialStatistic(statistics, StatisticType.CityServiceWorkers),
                CityServiceMaxWorkers = GetOfficialStatistic(statistics, StatisticType.CityServiceMaxWorkers),
                SeniorWorkerInDemandPercentage = GetOfficialStatistic(statistics, StatisticType.SeniorWorkerInDemandPercentage),
                DevTreePoints = singletonValues.DevTreePoints
            }
        };
    }

    public EducationSummary CollectEducationSummary()
    {
        if (!TryGetEntityManager(out EntityManager entityManager, out string availabilityReason))
        {
            return new EducationSummary
            {
                Status = MetricStatus.Unavailable,
                Notes = new[] { availabilityReason }
            };
        }

        CountResult citizenCount = TryCountByAll(entityManager, s_populationCitizenCandidates);
        CountResult workerCount = TryCountByAll(entityManager, s_populationWorkerCandidates);
        bool hasDetailedScan = TryScanPopulationAndWorkforce(entityManager, out PopulationWorkforceScanResult scan, out string? scanError);

        double? employmentRatePercent = null;
        if (hasDetailedScan && scan.TotalPotentialWorkers > 0)
        {
            employmentRatePercent = Math.Round(
                (scan.TotalWorkers * 100.0) / scan.TotalPotentialWorkers,
                2,
                MidpointRounding.AwayFromZero);
        }
        else if (citizenCount.Count.HasValue && citizenCount.Count.Value > 0 && workerCount.Count.HasValue)
        {
            employmentRatePercent = Math.Round(
                (workerCount.Count.Value * 100.0) / citizenCount.Count.Value,
                2,
                MidpointRounding.AwayFromZero);
        }

        double? educatedPercent = null;
        double? highlyEducatedPercent = null;
        WorkforceLevelSummary[] levels = Array.Empty<WorkforceLevelSummary>();

        if (hasDetailedScan)
        {
            int localPopulation = scan.LocalPopulation;
            if (localPopulation > 0)
            {
                int educated = scan.LocalByEducationLevel[2] + scan.LocalByEducationLevel[3] + scan.LocalByEducationLevel[4];
                int highlyEducated = scan.LocalByEducationLevel[4];
                educatedPercent = Math.Round((educated * 100.0) / localPopulation, 2, MidpointRounding.AwayFromZero);
                highlyEducatedPercent = Math.Round((highlyEducated * 100.0) / localPopulation, 2, MidpointRounding.AwayFromZero);
            }

            levels = scan.WorkforceLevels;
        }

        var notes = new List<string>();
        AddResultNotes(notes, "citizen_count_for_education_proxy", citizenCount);
        AddResultNotes(notes, "worker_count_for_education_proxy", workerCount);

        if (employmentRatePercent.HasValue)
        {
            notes.Add("employment_rate_percent is based on local working-age workforce proxy counts.");
        }
        else
        {
            notes.Add("employment_rate_percent unavailable: required ECS components were not all resolved.");
        }

        if (hasDetailedScan)
        {
            notes.Add("education levels are derived from Citizen.GetEducationLevel() across local moved-in population.");
            if (scan.WasSampled)
            {
                notes.Add("education metrics are based on sampled population entities and should be treated as proxy estimates.");
            }
        }
        else
        {
            notes.Add("education detail scan unavailable: " + (scanError ?? "unknown error"));
        }

        int availableMetrics = CountPresent(citizenCount.Count) + CountPresent(workerCount.Count) + (hasDetailedScan ? 1 : 0);

        return new EducationSummary
        {
            Status = ComputeStatus(availableMetrics: availableMetrics, expectedMetrics: 3),
            EducatedPercent = educatedPercent,
            HighlyEducatedPercent = highlyEducatedPercent,
            EmploymentRatePercent = employmentRatePercent,
            Levels = levels,
            SourceComponent = BuildSourceComponent("ecs.education_proxy", citizenCount, workerCount),
            Notes = notes.ToArray()
        };
    }

    public TransportProxySummary CollectTransportProxySummary()
    {
        if (!TryGetEntityManager(out EntityManager entityManager, out string availabilityReason))
        {
            return new TransportProxySummary
            {
                Status = MetricStatus.Unavailable,
                Notes = new[] { availabilityReason }
            };
        }

        CountResult roadVehicles = TryCountByAll(entityManager, s_transportRoadVehicleCandidates);
        CountResult publicVehicles = TryCountByAll(entityManager, s_transportPublicVehicleCandidates);
        CountResult activeLines = TryCountByAll(entityManager, s_transportLineCandidates);
        CountResult congestion = TryCountByAll(entityManager, s_transportCongestionCandidates);

        double? congestionIndex = null;
        if (congestion.Count.HasValue && roadVehicles.Count.HasValue && roadVehicles.Count.Value > 0)
        {
            double rawRatio = congestion.Count.Value / (double)roadVehicles.Count.Value;
            if (rawRatio < 0)
            {
                rawRatio = 0;
            }
            else if (rawRatio > 1)
            {
                rawRatio = 1;
            }

            congestionIndex = Math.Round(rawRatio, 4, MidpointRounding.AwayFromZero);
        }

        var notes = new List<string>
        {
            "transport metrics are ECS proxies, not guaranteed to match in-game UI panel values exactly."
        };

        AddResultNotes(notes, "road_vehicle_entities", roadVehicles);
        AddResultNotes(notes, "public_transport_vehicle_entities", publicVehicles);
        AddResultNotes(notes, "active_transport_lines", activeLines);

        if (congestionIndex.HasValue)
        {
            notes.Add("congestion_index_0_to_1 is derived from congestion-tagged vehicles divided by road vehicle entities.");
            AddResultNotes(notes, "congestion_entities", congestion);
        }
        else
        {
            notes.Add("congestion_index_0_to_1 unavailable: congestion-tag component was not resolved.");
        }

        return new TransportProxySummary
        {
            Status = ComputeStatus(
                availableMetrics: CountPresent(roadVehicles.Count) + CountPresent(publicVehicles.Count) + CountPresent(activeLines.Count),
                expectedMetrics: 3),
            RoadVehicleEntities = roadVehicles.Count,
            PublicTransportVehicleEntities = publicVehicles.Count,
            ActiveTransportLines = activeLines.Count,
            CongestionIndex0To1 = congestionIndex,
            SourceComponent = BuildSourceComponent("ecs.transport_proxy", roadVehicles, publicVehicles, activeLines, congestion),
            Notes = notes.ToArray()
        };
    }

    public WorkforceSummary CollectWorkforceSummary()
    {
        if (!TryGetEntityManager(out EntityManager entityManager, out string availabilityReason))
        {
            return new WorkforceSummary
            {
                Status = MetricStatus.Unavailable,
                Notes = new[] { availabilityReason }
            };
        }

        bool hasDetailedScan = TryScanPopulationAndWorkforce(entityManager, out PopulationWorkforceScanResult scan, out string? scanError);
        if (!hasDetailedScan)
        {
            return new WorkforceSummary
            {
                Status = MetricStatus.Unavailable,
                Notes = new[]
                {
                    "workforce scan failed: " + (scanError ?? "unknown error")
                },
                SourceComponent = "ecs.workforce:Citizen|Worker|Student|Household|HouseholdMember|MovingAway|HomelessHousehold|PropertyRenter|OutsideConnection"
            };
        }

        return new WorkforceSummary
        {
            Status = MetricStatus.Ok,
            TotalPotentialWorkers = scan.TotalPotentialWorkers,
            Workers = scan.TotalWorkers,
            Unemployed = scan.TotalUnemployed,
            HomelessUnemployed = scan.TotalHomelessUnemployed,
            Employable = scan.TotalEmployable,
            OutsideWorkers = scan.TotalOutsideWorkers,
            UnderemployedWorkers = scan.TotalUnderemployedWorkers,
            Levels = scan.WorkforceLevels,
            SourceComponent = "ecs.workforce:Citizen|Worker|Student|Household|HouseholdMember|MovingAway|HomelessHousehold|PropertyRenter|OutsideConnection",
            Notes = new[]
            {
                "workforce distribution uses local moved-in teens/adults excluding students, tourists, commuters, dead, and moving-away households.",
                "employable is defined as outside_workers + underemployed_workers (InfoLoom-style proxy).",
                scan.WasSampled
                    ? "workforce scan used sampling guardrails for large cities; counts are scaled estimates."
                    : "workforce scan covered full eligible entity set."
            }
        };
    }

    public WorkplacesSummary CollectWorkplacesSummary()
    {
        if (!TryGetEntityManager(out EntityManager entityManager, out string availabilityReason))
        {
            return new WorkplacesSummary
            {
                Status = MetricStatus.Unavailable,
                Notes = new[] { availabilityReason }
            };
        }

        bool hasWorkplaceScan = TryScanWorkplaces(entityManager, out WorkplacesScanResult scan, out string? scanError);
        if (!hasWorkplaceScan)
        {
            return new WorkplacesSummary
            {
                Status = MetricStatus.Unavailable,
                Notes = new[]
                {
                    "workplaces scan failed: " + (scanError ?? "unknown error")
                },
                SourceComponent = "ecs.workplaces:WorkProvider|Employee|PrefabRef|WorkplaceData|IndustrialProcessData|PropertyRenter|SpawnableBuildingData|Citizen"
            };
        }

        return new WorkplacesSummary
        {
            Status = MetricStatus.Ok,
            TotalWorkplaces = scan.TotalWorkplaces,
            FilledWorkplaces = scan.FilledWorkplaces,
            OpenWorkplaces = scan.OpenWorkplaces,
            CommuterEmployees = scan.CommuterEmployees,
            WorkProvidersTotal = scan.WorkProvidersTotal,
            WorkProvidersService = scan.WorkProvidersService,
            WorkProvidersCommercial = scan.WorkProvidersCommercial,
            WorkProvidersLeisure = scan.WorkProvidersLeisure,
            WorkProvidersExtractor = scan.WorkProvidersExtractor,
            WorkProvidersIndustrial = scan.WorkProvidersIndustrial,
            WorkProvidersOffice = scan.WorkProvidersOffice,
            Levels = scan.Levels,
            SourceComponent = "ecs.workplaces:WorkProvider|Employee|PrefabRef|WorkplaceData|IndustrialProcessData|PropertyRenter|SpawnableBuildingData|Citizen",
            Notes = new[]
            {
                "workplace distribution is calculated from active work providers and employee buffers.",
                "office/leisure split is inferred from IndustrialProcessData output resources (InfoLoom-style classification).",
                scan.WasSampled
                    ? "workplaces scan used sampling guardrails for large provider sets; counts are scaled estimates."
                    : "workplaces scan covered full active provider set."
            }
        };
    }

    public FacilityIdentitySummary CollectFacilityIdentitySummary()
    {
        if (!TryGetEntityManager(out EntityManager entityManager, out string availabilityReason))
        {
            return new FacilityIdentitySummary
            {
                Status = MetricStatus.Unavailable,
                Notes = new[] { availabilityReason }
            };
        }

        CountResult totalBuildings = TryCountByAll(entityManager, s_cityBuildingCandidates);
        CountResult residentialBuildings = TryCountByAny(entityManager, s_residentialBuildingFamilyCandidates);
        CountResult transportBuildings = TryCountByAny(entityManager, s_transportBuildingFamilyCandidates);
        bool hasWorkplaceScan = TryScanWorkplaces(entityManager, out WorkplacesScanResult workplaceScan, out string? workplaceError);

        var notes = new List<string>
        {
            "facility identity is a live runtime summary that combines direct building-family counts with active work-provider classification."
        };

        AddResultNotes(notes, "total_building_entities", totalBuildings);
        AddResultNotes(notes, "residential_building_entities", residentialBuildings);
        AddResultNotes(notes, "transport_building_entities", transportBuildings);

        if (hasWorkplaceScan)
        {
            notes.Add("active work-provider sector counts come from runtime provider classification and are stronger for live office/commercial/industrial/service meaning than current save-side broad counts.");
            if (workplaceScan.WasSampled)
            {
                notes.Add("facility sector counts used workplace sampling guardrails; values are scaled estimates for large provider sets.");
            }
        }
        else
        {
            notes.Add("active work-provider sector counts unavailable: " + (workplaceError ?? "unknown error"));
        }

        int availableMetrics = CountPresent(totalBuildings.Count)
            + CountPresent(residentialBuildings.Count)
            + CountPresent(transportBuildings.Count)
            + (hasWorkplaceScan ? 1 : 0);

        return new FacilityIdentitySummary
        {
            Status = ComputeStatus(availableMetrics, expectedMetrics: 4),
            TotalBuildingEntities = totalBuildings.Count,
            ResidentialBuildingEntities = residentialBuildings.Count,
            TransportBuildingEntities = transportBuildings.Count,
            ActiveWorkProviderEntities = hasWorkplaceScan ? workplaceScan.WorkProvidersTotal : null,
            ServiceProviderEntities = hasWorkplaceScan ? workplaceScan.WorkProvidersService : null,
            CommercialProviderEntities = hasWorkplaceScan ? workplaceScan.WorkProvidersCommercial : null,
            LeisureProviderEntities = hasWorkplaceScan ? workplaceScan.WorkProvidersLeisure : null,
            ExtractorProviderEntities = hasWorkplaceScan ? workplaceScan.WorkProvidersExtractor : null,
            IndustrialProviderEntities = hasWorkplaceScan ? workplaceScan.WorkProvidersIndustrial : null,
            OfficeProviderEntities = hasWorkplaceScan ? workplaceScan.WorkProvidersOffice : null,
            SourceComponent = BuildSourceComponent("ecs.facility_identity", totalBuildings, residentialBuildings, transportBuildings) +
                "|Game.Companies.WorkProvider|Game.Companies.Employee|Game.Prefabs.PrefabRef|Game.Prefabs.WorkplaceData|Game.Prefabs.IndustrialProcessData",
            Notes = notes.ToArray()
        };
    }

    public CompanyServiceSemanticsSummary CollectCompanyServiceSemanticsSummary()
    {
        if (!TryGetEntityManager(out EntityManager entityManager, out string availabilityReason))
        {
            return new CompanyServiceSemanticsSummary
            {
                Status = MetricStatus.Unavailable,
                Notes = new[] { availabilityReason }
            };
        }

        bool hasWorkplaceScan = TryScanWorkplaces(entityManager, out WorkplacesScanResult scan, out string? workplaceError);
        if (!hasWorkplaceScan)
        {
            return new CompanyServiceSemanticsSummary
            {
                Status = MetricStatus.Unavailable,
                Notes = new[]
                {
                    "company/service semantics scan failed: " + (workplaceError ?? "unknown error")
                },
                SourceComponent = "ecs.company_service_semantics:Game.Companies.WorkProvider|Game.Companies.Employee|Game.Prefabs.WorkplaceData|Game.Prefabs.IndustrialProcessData"
            };
        }

        SectorIntSummary providerCounts = CreateSectorIntSummary(
            total: scan.WorkProvidersTotal,
            service: scan.WorkProvidersService,
            commercial: scan.WorkProvidersCommercial,
            leisure: scan.WorkProvidersLeisure,
            extractor: scan.WorkProvidersExtractor,
            industrial: scan.WorkProvidersIndustrial,
            office: scan.WorkProvidersOffice);

        SectorIntSummary jobsTotal = CreateSectorIntSummary(
            total: scan.TotalWorkplaces,
            service: scan.ServiceWorkplacesTotal,
            commercial: scan.CommercialWorkplacesTotal,
            leisure: scan.LeisureWorkplacesTotal,
            extractor: scan.ExtractorWorkplacesTotal,
            industrial: scan.IndustrialWorkplacesTotal,
            office: scan.OfficeWorkplacesTotal);

        SectorIntSummary jobsFilled = CreateSectorIntSummary(
            total: scan.FilledWorkplaces,
            service: scan.ServiceEmployeesTotal,
            commercial: scan.CommercialEmployeesTotal,
            leisure: scan.LeisureEmployeesTotal,
            extractor: scan.ExtractorEmployeesTotal,
            industrial: scan.IndustrialEmployeesTotal,
            office: scan.OfficeEmployeesTotal);

        SectorIntSummary jobsOpen = CreateSectorIntSummary(
            total: Math.Max(0, scan.OpenWorkplaces),
            service: Math.Max(0, scan.ServiceWorkplacesTotal - scan.ServiceEmployeesTotal),
            commercial: Math.Max(0, scan.CommercialWorkplacesTotal - scan.CommercialEmployeesTotal),
            leisure: Math.Max(0, scan.LeisureWorkplacesTotal - scan.LeisureEmployeesTotal),
            extractor: Math.Max(0, scan.ExtractorWorkplacesTotal - scan.ExtractorEmployeesTotal),
            industrial: Math.Max(0, scan.IndustrialWorkplacesTotal - scan.IndustrialEmployeesTotal),
            office: Math.Max(0, scan.OfficeWorkplacesTotal - scan.OfficeEmployeesTotal));

        SectorDoubleSummary fillPercent = CreateSectorDoubleSummary(
            total: CalculatePercent(scan.FilledWorkplaces, scan.TotalWorkplaces),
            service: CalculatePercent(scan.ServiceEmployeesTotal, scan.ServiceWorkplacesTotal),
            commercial: CalculatePercent(scan.CommercialEmployeesTotal, scan.CommercialWorkplacesTotal),
            leisure: CalculatePercent(scan.LeisureEmployeesTotal, scan.LeisureWorkplacesTotal),
            extractor: CalculatePercent(scan.ExtractorEmployeesTotal, scan.ExtractorWorkplacesTotal),
            industrial: CalculatePercent(scan.IndustrialEmployeesTotal, scan.IndustrialWorkplacesTotal),
            office: CalculatePercent(scan.OfficeEmployeesTotal, scan.OfficeWorkplacesTotal));

        var notes = new List<string>
        {
            "company/service semantics summarize live provider-side staffing pressure by sector from the existing workplace scan.",
            "sector meaning is runtime-led here: provider tags and process data classify office, leisure, industrial, commercial, extractor, and service providers more directly than the current save path."
        };

        if (scan.WasSampled)
        {
            notes.Add("company/service sector counts used workplace sampling guardrails; job totals and fill percentages are scaled estimates.");
        }
        else
        {
            notes.Add("company/service sector counts covered the full active provider set.");
        }

        notes.Add("jobs_open is derived from sector job totals minus filled jobs and is clamped at zero to avoid negative outputs from scaling or rounding artifacts.");
        notes.Add("this group does not prove profits, resource shortages, trade flows, or production output.");

        return new CompanyServiceSemanticsSummary
        {
            Status = MetricStatus.Ok,
            ProviderCounts = providerCounts,
            JobsTotal = jobsTotal,
            JobsFilled = jobsFilled,
            JobsOpen = jobsOpen,
            FillPercent = fillPercent,
            SourceComponent = "ecs.company_service_semantics:Game.Companies.WorkProvider|Game.Companies.Employee|Game.Prefabs.WorkplaceData|Game.Prefabs.IndustrialProcessData|Game.Prefabs.PrefabRef",
            Notes = notes.ToArray()
        };
    }

    public HousingPressureSemanticsSummary CollectHousingPressureSemanticsSummary()
    {
        if (!TryGetEntityManager(out EntityManager entityManager, out string availabilityReason))
        {
            return new HousingPressureSemanticsSummary
            {
                Status = MetricStatus.Unavailable,
                Notes = new[] { availabilityReason }
            };
        }

        CountResult totalHouseholds = TryCountByAll(entityManager, s_populationHouseholdCandidates);
        CountResult residentialBuildings = TryCountByAny(entityManager, s_residentialBuildingFamilyCandidates);
        bool hasPopulationScan = TryScanPopulationAndWorkforce(entityManager, out PopulationWorkforceScanResult scan, out string? populationError);

        var notes = new List<string>
        {
            "housing pressure semantics combine residential building counts with household-side runtime context."
        };

        AddResultNotes(notes, "total_households", totalHouseholds);
        AddResultNotes(notes, "residential_building_entities", residentialBuildings);

        if (hasPopulationScan)
        {
            notes.Add("local, moving-away, and homeless household counts are deduplicated from the live population/workforce scan.");
            notes.Add(scan.WasSampled
                ? "housing pressure household-side counts used population sampling guardrails and should be treated as scaled estimates."
                : "housing pressure household-side counts covered the live population/workforce scan without additional sampling.");
        }
        else
        {
            notes.Add("household-side pressure context unavailable: " + (populationError ?? "population/workforce scan unavailable."));
        }

        notes.Add("this group does not prove exact residential occupancy or exact per-building capacity.");

        int availableMetrics = CountPresent(totalHouseholds.Count)
            + CountPresent(residentialBuildings.Count)
            + (hasPopulationScan ? 1 : 0);

        return new HousingPressureSemanticsSummary
        {
            Status = ComputeStatus(availableMetrics, expectedMetrics: 3),
            TotalHouseholds = totalHouseholds.Count,
            ResidentialBuildingEntities = residentialBuildings.Count,
            LocalHouseholds = hasPopulationScan ? scan.LocalHouseholds : null,
            HomelessHouseholds = hasPopulationScan ? scan.HomelessHouseholds : null,
            MovingAwayHouseholds = hasPopulationScan ? scan.MovingAwayHouseholds : null,
            HouseholdsPerResidentialBuilding = CalculateRatio(totalHouseholds.Count, residentialBuildings.Count),
            LocalHouseholdsPerResidentialBuilding = hasPopulationScan
                ? CalculateRatio(scan.LocalHouseholds, residentialBuildings.Count)
                : null,
            SourceComponent = BuildSourceComponent("ecs.housing_pressure_semantics", totalHouseholds, residentialBuildings) +
                "|Game.Citizens.Household|Game.Citizens.HouseholdMember|Game.Citizens.MovingAway|Game.Citizens.HomelessHousehold|Game.Buildings.PropertyRenter",
            Notes = notes.ToArray()
        };
    }

    public HouseholdPressureContextSummary CollectHouseholdPressureContextSummary()
    {
        if (!TryGetEntityManager(out EntityManager entityManager, out string availabilityReason))
        {
            return new HouseholdPressureContextSummary
            {
                Status = MetricStatus.Unavailable,
                Notes = new[] { availabilityReason }
            };
        }

        CountResult totalHouseholds = TryCountByAll(entityManager, s_populationHouseholdCandidates);
        bool hasPopulationScan = TryScanPopulationAndWorkforce(entityManager, out PopulationWorkforceScanResult scan, out string? populationError);
        if (!hasPopulationScan)
        {
            return new HouseholdPressureContextSummary
            {
                Status = MetricStatus.Unavailable,
                Notes = new[]
                {
                    "household pressure context unavailable: " + (populationError ?? "population/workforce scan unavailable.")
                },
                SourceComponent = "ecs.household_pressure_context:Game.Citizens.Household|Game.Citizens.HouseholdMember|Game.Citizens.MovingAway|Game.Citizens.HomelessHousehold|Game.Buildings.PropertyRenter"
            };
        }

        var notes = new List<string>
        {
            "household pressure context adds deduplicated household-side counts to the existing population summary.",
            scan.WasSampled
                ? "household pressure context used population sampling guardrails and should be treated as scaled estimates."
                : "household pressure context covered the live population/workforce scan without additional sampling."
        };

        if (!totalHouseholds.Count.HasValue)
        {
            notes.Add("total household count was not resolved directly; share percentages may be unavailable.");
        }

        return new HouseholdPressureContextSummary
        {
            Status = MetricStatus.Ok,
            TotalHouseholds = totalHouseholds.Count,
            LocalHouseholds = scan.LocalHouseholds,
            PropertyLinkedHouseholds = scan.PropertyLinkedHouseholds,
            HomelessHouseholds = scan.HomelessHouseholds,
            MovingAwayHouseholds = scan.MovingAwayHouseholds,
            HomelessHouseholdSharePercent = CalculatePercent(scan.HomelessHouseholds, totalHouseholds.Count ?? 0),
            MovingAwayHouseholdSharePercent = CalculatePercent(scan.MovingAwayHouseholds, totalHouseholds.Count ?? 0),
            SourceComponent = BuildSourceComponent("ecs.household_pressure_context", totalHouseholds) +
                "|Game.Citizens.Household|Game.Citizens.HouseholdMember|Game.Citizens.MovingAway|Game.Citizens.HomelessHousehold|Game.Buildings.PropertyRenter",
            Notes = notes.ToArray()
        };
    }

    public LaborPressureContextSummary CollectLaborPressureContextSummary()
    {
        if (!TryGetEntityManager(out EntityManager entityManager, out string availabilityReason))
        {
            return new LaborPressureContextSummary
            {
                Status = MetricStatus.Unavailable,
                Notes = new[] { availabilityReason }
            };
        }

        bool hasPopulationScan = TryScanPopulationAndWorkforce(entityManager, out PopulationWorkforceScanResult populationScan, out string? populationError);
        bool hasWorkplaceScan = TryScanWorkplaces(entityManager, out WorkplacesScanResult workplaceScan, out string? workplaceError);
        if (!hasPopulationScan || !hasWorkplaceScan)
        {
            var failureNotes = new List<string>();
            if (!hasPopulationScan)
            {
                failureNotes.Add("labor pressure population-side context unavailable: " + (populationError ?? "population/workforce scan unavailable."));
            }

            if (!hasWorkplaceScan)
            {
                failureNotes.Add("labor pressure workplace-side context unavailable: " + (workplaceError ?? "workplace scan unavailable."));
            }

            return new LaborPressureContextSummary
            {
                Status = MetricStatus.Unavailable,
                Notes = failureNotes.ToArray(),
                SourceComponent = "ecs.labor_pressure_context:Game.Citizens.Citizen|Game.Citizens.Worker|Game.Citizens.Student|Game.Companies.WorkProvider|Game.Companies.Employee"
            };
        }

        var notes = new List<string>
        {
            "labor pressure context combines workforce availability with live workplace demand signals.",
            populationScan.WasSampled || workplaceScan.WasSampled
                ? "labor pressure context used existing sampling guardrails from the population/workplace scans and should be treated as scaled estimates."
                : "labor pressure context reuses the existing live workforce and workplace scans without additional sampling.",
            "this group does not prove exact hiring blockers or job-suitability causality."
        };

        return new LaborPressureContextSummary
        {
            Status = MetricStatus.Ok,
            TotalPotentialWorkers = populationScan.TotalPotentialWorkers,
            TotalJobs = workplaceScan.TotalWorkplaces,
            JobsMinusPotentialWorkers = workplaceScan.TotalWorkplaces - populationScan.TotalPotentialWorkers,
            JobsMinusCurrentWorkers = workplaceScan.TotalWorkplaces - populationScan.TotalWorkers,
            OutsideWorkerSharePercent = CalculatePercent(populationScan.TotalOutsideWorkers, Math.Max(1, populationScan.TotalWorkers)),
            UnderemployedWorkerSharePercent = CalculatePercent(populationScan.TotalUnderemployedWorkers, Math.Max(1, populationScan.TotalPotentialWorkers)),
            CommuterEmployeeSharePercent = CalculatePercent(workplaceScan.CommuterEmployees, Math.Max(1, workplaceScan.FilledWorkplaces)),
            SourceComponent = "ecs.labor_pressure_context:Game.Citizens.Citizen|Game.Citizens.Worker|Game.Citizens.Student|Game.Companies.WorkProvider|Game.Companies.Employee",
            Notes = notes.ToArray()
        };
    }

    public MobilitySummary CollectMobilitySummary()
    {
        if (!TryGetEntityManager(out EntityManager entityManager, out string availabilityReason))
        {
            return new MobilitySummary
            {
                Status = MetricStatus.Unavailable,
                Notes = new[] { availabilityReason },
                MetricMetadata = CreateMobilityMetricMetadata()
            };
        }

        CountResult roadVehicles = TryCountByAll(entityManager, s_transportRoadVehicleCandidates);
        CountResult publicVehicles = TryCountByAll(entityManager, s_transportPublicVehicleCandidates);

        double? trafficVolumeIndex = null;
        if (roadVehicles.Count.HasValue && publicVehicles.Count.HasValue)
        {
            trafficVolumeIndex = Math.Round(
                roadVehicles.Count.Value / (double)Math.Max(1, publicVehicles.Count.Value),
                4,
                MidpointRounding.AwayFromZero);
        }

        var notes = new List<string>
        {
            "mobility metrics are observed or derived from observed ECS/UI line data."
        };

        if (!trafficVolumeIndex.HasValue)
        {
            notes.Add("traffic_volume_index unavailable: required vehicle entity counts were not both resolved.");
        }

        int? linesTotal = null;
        int? passengerLinesTotal = null;
        int? cargoLinesTotal = null;
        ModeEntityCounts? linesByTransportType = null;
        ModeEntityCounts? activeVehiclesByTransportType = null;
        int? linesWithServiceCount = null;
        int? linesWithoutServiceCount = null;
        double? linesWithServicePercent = null;
        double? lineVehicleEntitiesP50 = null;
        double? lineVehicleEntitiesP95 = null;
        IReadOnlyDictionary<long, TransportLineUsageEntry>? lineUsageByEntity = null;
        if (TryScanTransportLineUsage(entityManager, out TransportLineUsageScanResult lineUsageScan, out string? lineUsageError))
        {
            lineUsageByEntity = BuildLineUsageEntryLookup(lineUsageScan.LineUsageByTransportType);
            notes.Add("line usage now includes onboard passengers and total passenger capacity when those vehicle capacities can be resolved.");
        }
        else
        {
            notes.Add("line usage enrichment unavailable: " + (lineUsageError ?? "transport line usage scan unavailable."));
        }

        MobilityLineRecord[] topLinesByActiveVehicles = Array.Empty<MobilityLineRecord>();
        MobilityLineRecord[] lines = Array.Empty<MobilityLineRecord>();
        bool lineDataAvailable = false;
        if (TryCollectObservedMobilityLineData(entityManager, lineUsageByEntity, out ObservedMobilityLineData observedLines, out string? observedError))
        {
            lineDataAvailable = true;
            lines = observedLines.Lines;
            topLinesByActiveVehicles = observedLines.TopLinesByActiveVehicles;
            linesTotal = observedLines.LinesTotal;
            passengerLinesTotal = observedLines.PassengerLinesTotal;
            cargoLinesTotal = observedLines.CargoLinesTotal;
            linesByTransportType = observedLines.LinesByTransportType;
            activeVehiclesByTransportType = observedLines.ActiveVehiclesByTransportType;
            linesWithServiceCount = observedLines.LinesWithServiceCount;
            linesWithoutServiceCount = observedLines.LinesWithoutServiceCount;
            linesWithServicePercent = observedLines.LinesWithServicePercent;
            lineVehicleEntitiesP50 = observedLines.LineVehicleEntitiesP50;
            lineVehicleEntitiesP95 = observedLines.LineVehicleEntitiesP95;
            notes.Add("line-level mobility metrics are observed directly from transport line ECS/UI data.");
            if (observedLines.UsedXtmAcronym)
            {
                notes.Add("line_identifier values prioritize XTM acronyms when available, then route_number.");
            }
        }
        else
        {
            notes.Add("line-level mobility metrics unavailable: " + (observedError ?? "line scan unavailable."));
        }

        return new MobilitySummary
        {
            Status = ComputeStatus(
                availableMetrics: CountPresent(trafficVolumeIndex) +
                                  CountPresent(linesTotal) +
                                  CountPresent(passengerLinesTotal) +
                                  CountPresent(cargoLinesTotal) +
                                  CountPresent(linesByTransportType) +
                                  CountPresent(activeVehiclesByTransportType) +
                                  CountPresent(linesWithServiceCount) +
                                  CountPresent(linesWithoutServiceCount) +
                                  CountPresent(linesWithServicePercent) +
                                  CountPresent(lineVehicleEntitiesP50) +
                                  CountPresent(lineVehicleEntitiesP95) +
                                  (lineDataAvailable ? 2 : 0),
                expectedMetrics: 13),
            TrafficVolumeIndex = trafficVolumeIndex,
            LinesTotal = linesTotal,
            PassengerLinesTotal = passengerLinesTotal,
            CargoLinesTotal = cargoLinesTotal,
            LinesByTransportType = linesByTransportType,
            ActiveVehiclesByTransportType = activeVehiclesByTransportType,
            LinesWithServiceCount = linesWithServiceCount,
            LinesWithoutServiceCount = linesWithoutServiceCount,
            LinesWithServicePercent = linesWithServicePercent,
            LineVehicleEntitiesP50 = lineVehicleEntitiesP50,
            LineVehicleEntitiesP95 = lineVehicleEntitiesP95,
            TopLinesByActiveVehicles = topLinesByActiveVehicles,
            Lines = lines,
            SourceComponent = BuildSourceComponent(
                "ecs.mobility",
                roadVehicles,
                publicVehicles,
                TryCountByAll(entityManager, s_passengerLineCandidates),
                TryCountByAll(entityManager, s_cargoLineCandidates)),
            MetricMetadata = CreateMobilityMetricMetadata(),
            Notes = notes.ToArray()
        };
    }

    public TransitLineDetailSemanticsSummary CollectTransitLineDetailSemanticsSummary()
    {
        if (!TryGetEntityManager(out EntityManager entityManager, out string availabilityReason))
        {
            return new TransitLineDetailSemanticsSummary
            {
                Status = MetricStatus.Unavailable,
                Notes = new[] { availabilityReason },
                MetricMetadata = MetricMetadataDefaults.TransitLineDetailSemantics()
            };
        }

        try
        {
            PrefabSystem prefabSystem = entityManager.World.GetOrCreateSystemManaged<PrefabSystem>();
            NameSystem nameSystem = entityManager.World.GetOrCreateSystemManaged<NameSystem>();
            Type? lineComponentType = ResolveFirstComponentType(s_passengerLineCandidates);
            Type? xtmRouteExtraDataType = ResolveComponentType("BelzontTLM.XTMRouteExtraData");

            using EntityQuery lineQuery = entityManager.CreateEntityQuery(
                new EntityQueryDesc
                {
                    All = new[]
                    {
                        ComponentType.ReadOnly<Route>(),
                        ComponentType.ReadOnly<TransportLine>(),
                        ComponentType.ReadOnly<RouteWaypoint>(),
                        ComponentType.ReadOnly<PrefabRef>()
                    },
                    None = new[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>()
                    }
                });

            using NativeArray<UITransportLineData> sortedLines = TransportUIUtils.GetSortedLines(lineQuery, entityManager, prefabSystem);
            var lines = new List<TransitLineDetailRecord>(sortedLines.Length);
            int passengerLines = 0;
            int cargoLines = 0;
            int totalWaiting = 0;
            int totalOnboard = 0;
            int maxWaiting = 0;
            bool usedXtmAcronym = false;
            double ticksPerDay = TryResolveTicksPerDay(entityManager) ?? kDefaultTicksPerDay;

            for (int i = 0; i < sortedLines.Length; i++)
            {
                UITransportLineData lineData = sortedLines[i];
                Entity lineEntity = lineData.entity;

                if (lineData.isCargo)
                {
                    cargoLines++;
                }
                else
                {
                    passengerLines++;
                }

                TransitLineDetailRecord detail = CollectTransitLineDetailRecord(
                    entityManager,
                    nameSystem,
                    lineEntity,
                    lineData,
                    lineComponentType,
                    xtmRouteExtraDataType,
                    ticksPerDay,
                    ref usedXtmAcronym);

                lines.Add(detail);
                totalWaiting += detail.WaitingPassengersAllStops;
                totalOnboard += detail.OnboardPassengersInVehicles;
                if (detail.MaxWaitingPassengersAtStop > maxWaiting)
                {
                    maxWaiting = detail.MaxWaitingPassengersAtStop;
                }
            }

            lines.Sort(
                static (left, right) =>
                {
                    int waitingComparison = right.WaitingPassengersAllStops.CompareTo(left.WaitingPassengersAllStops);
                    if (waitingComparison != 0)
                    {
                        return waitingComparison;
                    }

                    int onboardComparison = right.OnboardPassengersInVehicles.CompareTo(left.OnboardPassengersInVehicles);
                    if (onboardComparison != 0)
                    {
                        return onboardComparison;
                    }

                    return left.LineEntityIndex.CompareTo(right.LineEntityIndex);
                });

            var notes = new List<string>
            {
                "line detail semantics mirror XTM's live line panel data path: route waypoints, route vehicles, route segments, waiting passengers, path information, vehicle capacity, and odometer.",
                "passengers into vehicles is exported as onboard_passengers_in_vehicles because XTM's UI computes it from current vehicle load, not a cumulative boarding counter.",
                "expected_round_trip_time_minutes uses XTM's formula with the current runtime ticks-per-day when available, otherwise the default 262144 ticks/day."
            };

            if (usedXtmAcronym)
            {
                notes.Add("line_identifier values prioritize XTM acronyms when available, then route_number.");
            }

            return new TransitLineDetailSemanticsSummary
            {
                Status = sortedLines.Length > 0 ? MetricStatus.Ok : MetricStatus.Partial,
                LinesObserved = lines.Count,
                PassengerLinesObserved = passengerLines,
                CargoLinesObserved = cargoLines,
                TotalWaitingPassengers = totalWaiting,
                TotalOnboardPassengers = totalOnboard,
                MaxWaitingPassengersAtStop = maxWaiting,
                Lines = lines.ToArray(),
                SourceComponent = "ecs.transit_line_detail:Game.Routes.RouteWaypoint|Game.Routes.RouteVehicle|Game.Routes.RouteSegment|Game.Routes.WaitingPassengers|Game.Pathfind.PathInformation|Game.Vehicles.Odometer",
                MetricMetadata = MetricMetadataDefaults.TransitLineDetailSemantics(),
                Notes = notes.ToArray()
            };
        }
        catch (Exception ex)
        {
            return new TransitLineDetailSemanticsSummary
            {
                Status = MetricStatus.Partial,
                SourceComponent = "ecs.transit_line_detail",
                MetricMetadata = MetricMetadataDefaults.TransitLineDetailSemantics(),
                Notes = new[] { "transit line detail scan failed: " + ex.GetType().Name + ": " + ex.Message }
            };
        }
    }

    private static double? TryResolveTicksPerDay(EntityManager entityManager)
    {
        try
        {
            Type? timeSettingsType = ResolveComponentType("Game.Simulation.TimeSettingsData")
                ?? ResolveComponentType("Game.Simulation.TimeSettings");
            if (timeSettingsType == null)
            {
                return null;
            }

            using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly(timeSettingsType));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.TempJob);
            if (entities.Length == 0)
            {
                return null;
            }

            if (!TryGetComponentDataBoxed(entityManager, entities[0], timeSettingsType, out object? settings, out _) ||
                settings == null)
            {
                return null;
            }

            return TryExtractNamedDouble(settings, "ticksPerDay", out double ticksPerDay) ||
                   TryExtractNamedDouble(settings, "m_TicksPerDay", out ticksPerDay)
                ? ticksPerDay
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool TryExtractNamedDouble(object source, string memberName, out double value)
    {
        value = 0.0;
        Type type = source.GetType();

        FieldInfo? field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (TryConvertToDouble(field?.GetValue(source), out value))
        {
            return true;
        }

        PropertyInfo? property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return TryConvertToDouble(property?.GetValue(source), out value);
    }

    private static bool TryConvertToDouble(object? rawValue, out double value)
    {
        value = 0.0;
        if (rawValue == null)
        {
            return false;
        }

        try
        {
            value = Convert.ToDouble(rawValue, CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private TransitLineDetailRecord CollectTransitLineDetailRecord(
        EntityManager entityManager,
        NameSystem nameSystem,
        Entity lineEntity,
        UITransportLineData lineData,
        Type? lineComponentType,
        Type? xtmRouteExtraDataType,
        double ticksPerDay,
        ref bool usedXtmAcronym)
    {
        int? routeNumber = null;
        if (entityManager.HasComponent<RouteNumber>(lineEntity))
        {
            RouteNumber routeNumberData = entityManager.GetComponentData<RouteNumber>(lineEntity);
            routeNumber = routeNumberData.m_Number;
        }

        string? xtmAcronym = TryResolveXtmLineAcronym(entityManager, lineEntity, xtmRouteExtraDataType);
        if (!string.IsNullOrWhiteSpace(xtmAcronym))
        {
            usedXtmAcronym = true;
        }

        string identifierSource = "none";
        string? identifier = null;
        if (!string.IsNullOrWhiteSpace(xtmAcronym))
        {
            identifier = xtmAcronym;
            identifierSource = "xtm_acronym";
        }
        else if (routeNumber.HasValue)
        {
            identifier = routeNumber.Value.ToString(CultureInfo.InvariantCulture);
            identifierSource = "route_number";
        }

        string? lineName = TryResolveObservedLineName(entityManager, nameSystem, lineEntity, lineComponentType);
        if (string.IsNullOrWhiteSpace(lineName) || string.Equals(lineName, "NUMBER", StringComparison.OrdinalIgnoreCase))
        {
            lineName = identifier;
        }

        TransitLineStopDetailRecord[] stopRecords = CollectTransitLineStops(entityManager, nameSystem, lineEntity, out TransitLineStopLoad[] stopLoads);
        TransitLineVehicleLoad[] vehicleLoads = CollectTransitLineVehicles(entityManager, lineEntity);
        double[] segmentDurations = CollectTransitLineSegmentDurations(entityManager, lineEntity);

        TransitLineDetailCalculationResult calculation = TransitLineDetailCalculator.Calculate(
            new TransitLineDetailCalculationInput
            {
                Stops = stopLoads,
                Vehicles = vehicleLoads,
                SegmentDurations = segmentDurations,
                TicksPerDay = ticksPerDay
            });

        return new TransitLineDetailRecord
        {
            LineEntityIndex = lineEntity.Index,
            LineEntityVersion = lineEntity.Version,
            LineName = lineName,
            LineIdentifier = identifier,
            LineIdentifierSource = identifierSource,
            RouteNumber = routeNumber,
            Mode = NormalizeMobilityMode(lineData.type.ToString()),
            IsCargo = lineData.isCargo,
            LineColor = TryResolveObservedLineColor(entityManager, lineEntity),
            Active = lineData.active,
            Visible = lineData.visible,
            StopCount = stopRecords.Length,
            SegmentCount = segmentDurations.Length,
            ActiveVehicleEntities = vehicleLoads.Length,
            StopCapacity = calculation.StopCapacity > 0 ? calculation.StopCapacity : null,
            WaitingPassengersAllStops = calculation.WaitingPassengersAllStops,
            MaxWaitingPassengersAtStop = calculation.MaxWaitingPassengersAtStop,
            OnboardPassengersInVehicles = calculation.OnboardPassengersInVehicles,
            TotalPassengerCapacity = calculation.TotalPassengerCapacity > 0 ? calculation.TotalPassengerCapacity : null,
            AverageVehicleOccupancyPercent = calculation.AverageVehicleOccupancyPercent,
            AverageStopOccupancyPercent = calculation.AverageStopOccupancyPercent,
            ExpectedRoundTripTimeTicks = calculation.ExpectedRoundTripTimeTicks,
            ExpectedRoundTripTimeMinutes = calculation.ExpectedRoundTripTimeMinutes,
            NextMaintenanceVehicleEntityIndex = calculation.NextMaintenanceVehicleEntityIndex,
            NextMaintenanceVehicleEntityVersion = calculation.NextMaintenanceVehicleEntityVersion,
            NextMaintenanceDistanceM = calculation.NextMaintenanceDistanceMeters,
            Stops = stopRecords
        };
    }

    private static TransitLineStopDetailRecord[] CollectTransitLineStops(
        EntityManager entityManager,
        NameSystem nameSystem,
        Entity lineEntity,
        out TransitLineStopLoad[] stopLoads)
    {
        if (!entityManager.HasComponent<RouteWaypoint>(lineEntity))
        {
            stopLoads = Array.Empty<TransitLineStopLoad>();
            return Array.Empty<TransitLineStopDetailRecord>();
        }

        DynamicBuffer<RouteWaypoint> waypoints = entityManager.GetBuffer<RouteWaypoint>(lineEntity, true);
        double?[] routePositions = CalculateRouteWaypointPositions(entityManager, lineEntity, waypoints.Length);
        var stops = new TransitLineStopDetailRecord[waypoints.Length];
        stopLoads = new TransitLineStopLoad[waypoints.Length];

        for (int i = 0; i < waypoints.Length; i++)
        {
            Entity waypoint = waypoints[i].m_Waypoint;
            int waiting = 0;
            if (entityManager.HasComponent<WaitingPassengers>(waypoint))
            {
                WaitingPassengers waitingPassengers = entityManager.GetComponentData<WaitingPassengers>(waypoint);
                waiting = Math.Max(0, waitingPassengers.m_Count);
            }

            stopLoads[i] = new TransitLineStopLoad(waiting);
            stops[i] = new TransitLineStopDetailRecord
            {
                WaypointEntityIndex = waypoint.Index,
                WaypointEntityVersion = waypoint.Version,
                StopName = TryResolveEntityDisplayName(nameSystem, waypoint),
                WaitingPassengers = waiting,
                RoutePosition = routePositions.Length > i ? routePositions[i] : null
            };
        }

        return stops;
    }

    private static double?[] CalculateRouteWaypointPositions(EntityManager entityManager, Entity lineEntity, int waypointCount)
    {
        var positions = new double?[waypointCount];
        if (waypointCount <= 0 || !entityManager.HasComponent<RouteSegment>(lineEntity))
        {
            return positions;
        }

        DynamicBuffer<RouteSegment> routeSegments = entityManager.GetBuffer<RouteSegment>(lineEntity, true);
        var cumulativeDistances = new double[waypointCount];
        double totalDistance = 0.0;
        int count = Math.Min(waypointCount, routeSegments.Length);
        for (int i = 0; i < count; i++)
        {
            cumulativeDistances[i] = totalDistance;
            Entity segment = routeSegments[i].m_Segment;
            if (entityManager.HasComponent<PathInformation>(segment))
            {
                PathInformation pathInformation = entityManager.GetComponentData<PathInformation>(segment);
                totalDistance += Math.Max(0.0, pathInformation.m_Distance);
            }
        }

        if (totalDistance <= 0.0)
        {
            return positions;
        }

        for (int i = 0; i < waypointCount; i++)
        {
            positions[i] = Math.Round(cumulativeDistances[i] / totalDistance, 4, MidpointRounding.AwayFromZero);
        }

        return positions;
    }

    private static double[] CollectTransitLineSegmentDurations(EntityManager entityManager, Entity lineEntity)
    {
        if (!entityManager.HasComponent<RouteSegment>(lineEntity))
        {
            return Array.Empty<double>();
        }

        DynamicBuffer<RouteSegment> routeSegments = entityManager.GetBuffer<RouteSegment>(lineEntity, true);
        var durations = new List<double>(routeSegments.Length);
        for (int i = 0; i < routeSegments.Length; i++)
        {
            Entity segment = routeSegments[i].m_Segment;
            if (!entityManager.HasComponent<PathInformation>(segment))
            {
                continue;
            }

            PathInformation pathInformation = entityManager.GetComponentData<PathInformation>(segment);
            durations.Add(Math.Max(0.0, pathInformation.m_Duration));
        }

        return durations.ToArray();
    }

    private static TransitLineVehicleLoad[] CollectTransitLineVehicles(EntityManager entityManager, Entity lineEntity)
    {
        if (!entityManager.HasComponent<RouteVehicle>(lineEntity))
        {
            return Array.Empty<TransitLineVehicleLoad>();
        }

        DynamicBuffer<RouteVehicle> routeVehicles = entityManager.GetBuffer<RouteVehicle>(lineEntity, true);
        var loads = new List<TransitLineVehicleLoad>(routeVehicles.Length);
        var capacityByPrefab = new Dictionary<Entity, int?>();
        for (int i = 0; i < routeVehicles.Length; i++)
        {
            Entity vehicle = routeVehicles[i].m_Vehicle;
            if (vehicle == Entity.Null || !entityManager.Exists(vehicle))
            {
                continue;
            }

            int passengers = CountVehiclePassengers(entityManager, vehicle);
            int capacity = ResolveVehicleCapacityWithLayout(entityManager, vehicle, capacityByPrefab);
            double? odometer = null;
            if (entityManager.HasComponent<Odometer>(vehicle))
            {
                odometer = entityManager.GetComponentData<Odometer>(vehicle).m_Distance;
            }

            loads.Add(
                new TransitLineVehicleLoad(
                    EntityIndex: vehicle.Index,
                    EntityVersion: vehicle.Version,
                    PassengerCount: passengers,
                    Capacity: capacity,
                    OdometerMeters: odometer,
                    MaintenanceRangeMeters: ResolveVehicleMaintenanceRange(entityManager, vehicle)));
        }

        return loads.ToArray();
    }

    private static int CountVehiclePassengers(EntityManager entityManager, Entity vehicle)
    {
        return TransitLayoutTraversal.SumLeafValues(
            vehicle,
            currentVehicle => EnumerateLayoutVehicles(entityManager, currentVehicle),
            currentVehicle => CountVehiclePassengersAtLeaf(entityManager, currentVehicle));
    }

    private static int ResolveVehicleCapacityWithLayout(
        EntityManager entityManager,
        Entity vehicle,
        IDictionary<Entity, int?> capacityByPrefab)
    {
        return TransitLayoutTraversal.SumLeafValues(
            vehicle,
            currentVehicle => EnumerateLayoutVehicles(entityManager, currentVehicle),
            currentVehicle => ResolveVehicleCapacityAtLeaf(entityManager, currentVehicle, capacityByPrefab));
    }

    private static IEnumerable<Entity> EnumerateLayoutVehicles(EntityManager entityManager, Entity vehicle)
    {
        if (vehicle == Entity.Null || !entityManager.Exists(vehicle) || !entityManager.HasComponent<LayoutElement>(vehicle))
        {
            yield break;
        }

        DynamicBuffer<LayoutElement> layoutElements = entityManager.GetBuffer<LayoutElement>(vehicle, true);
        for (int i = 0; i < layoutElements.Length; i++)
        {
            Entity childVehicle = layoutElements[i].m_Vehicle;
            if (childVehicle != Entity.Null && entityManager.Exists(childVehicle))
            {
                yield return childVehicle;
            }
        }
    }

    private static int CountVehiclePassengersAtLeaf(EntityManager entityManager, Entity vehicle)
    {
        if (vehicle == Entity.Null || !entityManager.Exists(vehicle))
        {
            return 0;
        }

        int passengers = 0;
        if (entityManager.HasComponent<Passenger>(vehicle))
        {
            DynamicBuffer<Passenger> passengerBuffer = entityManager.GetBuffer<Passenger>(vehicle, true);
            passengers += passengerBuffer.Length;
        }

        if (entityManager.HasComponent<Game.Economy.Resources>(vehicle))
        {
            DynamicBuffer<Game.Economy.Resources> resources = entityManager.GetBuffer<Game.Economy.Resources>(vehicle, true);
            for (int i = 0; i < resources.Length; i++)
            {
                passengers += Math.Max(0, resources[i].m_Amount);
            }
        }

        return passengers;
    }

    private static int ResolveVehicleCapacityAtLeaf(
        EntityManager entityManager,
        Entity vehicle,
        IDictionary<Entity, int?> capacityByPrefab)
    {
        if (vehicle == Entity.Null || !entityManager.Exists(vehicle))
        {
            return 0;
        }

        return TryResolveVehiclePassengerCapacity(entityManager, vehicle, capacityByPrefab, out int capacity)
            ? capacity
            : 0;
    }

    private static double? ResolveVehicleMaintenanceRange(EntityManager entityManager, Entity vehicle)
    {
        if (!entityManager.HasComponent<PrefabRef>(vehicle))
        {
            return null;
        }

        Entity prefab = entityManager.GetComponentData<PrefabRef>(vehicle).m_Prefab;
        if (prefab == Entity.Null || !entityManager.HasComponent<PublicTransportVehicleData>(prefab))
        {
            return null;
        }

        return entityManager.GetComponentData<PublicTransportVehicleData>(prefab).m_MaintenanceRange;
    }

    private static string? TryResolveEntityDisplayName(NameSystem nameSystem, Entity entity)
    {
        try
        {
            object lineName = nameSystem.GetName(entity);
            if (TryExtractDisplayNameFromObject(lineName, out string extracted))
            {
                return extracted;
            }

            if (TryNormalizeDisplayName(lineName.ToString(), out extracted))
            {
                return extracted;
            }
        }
        catch
        {
            // Best-effort stop/platform naming.
        }

        return null;
    }

    private bool TryCollectObservedMobilityLineData(
        EntityManager entityManager,
        IReadOnlyDictionary<long, TransportLineUsageEntry>? lineUsageByEntity,
        out ObservedMobilityLineData result,
        out string? error)
    {
        error = null;
        result = default;

        try
        {
            PrefabSystem prefabSystem = entityManager.World.GetOrCreateSystemManaged<PrefabSystem>();
            NameSystem nameSystem = entityManager.World.GetOrCreateSystemManaged<NameSystem>();
            Type? lineComponentType = ResolveFirstComponentType(s_passengerLineCandidates);
            Type? xtmRouteExtraDataType = ResolveComponentType("BelzontTLM.XTMRouteExtraData");

            using EntityQuery lineQuery = entityManager.CreateEntityQuery(
                new EntityQueryDesc
                {
                    All = new[]
                    {
                        ComponentType.ReadOnly<Route>(),
                        ComponentType.ReadOnly<TransportLine>(),
                        ComponentType.ReadOnly<RouteWaypoint>(),
                        ComponentType.ReadOnly<PrefabRef>()
                    },
                    None = new[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>()
                    }
                });

            using NativeArray<UITransportLineData> sortedLines = TransportUIUtils.GetSortedLines(lineQuery, entityManager, prefabSystem);
            var lineModeCounts = CreateMobilityModeCounterDictionary();
            var activeVehicleModeCounts = CreateMobilityModeCounterDictionary();
            var vehicleCounts = new List<int>(sortedLines.Length);
            var lineRecords = new List<MobilityLineRecord>(sortedLines.Length);
            bool usedXtmAcronym = false;
            int linesWithService = 0;
            int cargoLines = 0;

            for (int i = 0; i < sortedLines.Length; i++)
            {
                UITransportLineData lineData = sortedLines[i];
                Entity lineEntity = lineData.entity;
                string mode = NormalizeMobilityMode(lineData.type.ToString());
                int activeVehicles = Math.Max(0, lineData.vehicles);
                int stops = Math.Max(0, lineData.stops);

                if (lineData.isCargo)
                {
                    cargoLines++;
                }

                if (activeVehicles > 0)
                {
                    linesWithService++;
                }

                vehicleCounts.Add(activeVehicles);
                IncrementModeCounter(lineModeCounts, mode, 1);
                IncrementModeCounter(activeVehicleModeCounts, mode, activeVehicles);

                int? routeNumber = null;
                if (entityManager.HasComponent<RouteNumber>(lineEntity))
                {
                    RouteNumber routeNumberData = entityManager.GetComponentData<RouteNumber>(lineEntity);
                    routeNumber = routeNumberData.m_Number;
                }

                string? xtmAcronym = TryResolveXtmLineAcronym(entityManager, lineEntity, xtmRouteExtraDataType);
                if (!string.IsNullOrWhiteSpace(xtmAcronym))
                {
                    usedXtmAcronym = true;
                }

                string identifierSource = "none";
                string? identifier = null;
                if (!string.IsNullOrWhiteSpace(xtmAcronym))
                {
                    identifier = xtmAcronym;
                    identifierSource = "xtm_acronym";
                }
                else if (routeNumber.HasValue)
                {
                    identifier = routeNumber.Value.ToString(CultureInfo.InvariantCulture);
                    identifierSource = "route_number";
                }

                string? lineName = TryResolveObservedLineName(entityManager, nameSystem, lineEntity, lineComponentType);
                if (string.IsNullOrWhiteSpace(lineName) || string.Equals(lineName, "NUMBER", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(xtmAcronym))
                    {
                        lineName = xtmAcronym;
                    }
                    else if (routeNumber.HasValue)
                    {
                        lineName = routeNumber.Value.ToString(CultureInfo.InvariantCulture);
                    }
                }

                TryGetLineUsageEntry(lineUsageByEntity, lineEntity, out TransportLineUsageEntry? usageEntry);

                lineRecords.Add(
                    new MobilityLineRecord
                    {
                        LineEntityIndex = lineEntity.Index,
                        LineEntityVersion = lineEntity.Version,
                        LineName = lineName,
                        LineIdentifier = identifier,
                        LineIdentifierSource = identifierSource,
                        RouteNumber = routeNumber,
                        Mode = mode,
                        IsCargo = lineData.isCargo,
                        LineColor = TryResolveObservedLineColor(entityManager, lineEntity),
                        Active = lineData.active,
                        Visible = lineData.visible,
                        Schedule = NormalizeLineSchedule(lineData.schedule),
                        Stops = stops,
                        ActiveVehicleEntities = activeVehicles,
                        OnboardPassengerEntities = usageEntry?.OnboardPassengerEntities,
                        TotalPassengerCapacity = usageEntry?.TotalPassengerCapacity,
                        UsagePercent = usageEntry?.UsagePercent,
                        LengthM = lineData.length >= 0 ? Math.Round(lineData.length, 2, MidpointRounding.AwayFromZero) : null
                    });
            }

            double? lineVehicleEntitiesP50 = null;
            double? lineVehicleEntitiesP95 = null;
            if (vehicleCounts.Count > 0)
            {
                vehicleCounts.Sort();
                lineVehicleEntitiesP50 = Math.Round(Percentile(vehicleCounts, 0.50), 2, MidpointRounding.AwayFromZero);
                lineVehicleEntitiesP95 = Math.Round(Percentile(vehicleCounts, 0.95), 2, MidpointRounding.AwayFromZero);
            }

            var topLinesByActiveVehicles = new List<MobilityLineRecord>(lineRecords);
            topLinesByActiveVehicles.Sort(
                (left, right) =>
                {
                    int vehicleComparison = right.ActiveVehicleEntities.CompareTo(left.ActiveVehicleEntities);
                    if (vehicleComparison != 0)
                    {
                        return vehicleComparison;
                    }

                    return left.LineEntityIndex.CompareTo(right.LineEntityIndex);
                });
            if (topLinesByActiveVehicles.Count > 25)
            {
                topLinesByActiveVehicles.RemoveRange(25, topLinesByActiveVehicles.Count - 25);
            }

            int linesTotal = lineRecords.Count;
            int passengerLines = Math.Max(0, linesTotal - cargoLines);
            int linesWithoutService = Math.Max(0, linesTotal - linesWithService);

            result = new ObservedMobilityLineData(
                Lines: lineRecords.ToArray(),
                TopLinesByActiveVehicles: topLinesByActiveVehicles.ToArray(),
                LinesTotal: linesTotal,
                PassengerLinesTotal: passengerLines,
                CargoLinesTotal: cargoLines,
                LinesByTransportType: ToModeEntityCounts(lineModeCounts),
                ActiveVehiclesByTransportType: ToModeEntityCounts(activeVehicleModeCounts),
                LinesWithServiceCount: linesWithService,
                LinesWithoutServiceCount: linesWithoutService,
                LinesWithServicePercent: CalculatePercent(linesWithService, Math.Max(1, linesTotal)),
                LineVehicleEntitiesP50: lineVehicleEntitiesP50,
                LineVehicleEntitiesP95: lineVehicleEntitiesP95,
                UsedXtmAcronym: usedXtmAcronym);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.GetType().Name + ": " + ex.Message;
            result = default;
            return false;
        }
    }

    private static ModeEntityCounts ToModeEntityCounts(IReadOnlyDictionary<string, int> modeCounters)
    {
        return new ModeEntityCounts
        {
            Bus = GetModeCounter(modeCounters, "bus"),
            Tram = GetModeCounter(modeCounters, "tram"),
            Subway = GetModeCounter(modeCounters, "subway"),
            Train = GetModeCounter(modeCounters, "train"),
            Ship = GetModeCounter(modeCounters, "ship"),
            Ferry = GetModeCounter(modeCounters, "ferry"),
            Air = GetModeCounter(modeCounters, "air"),
            Taxi = GetModeCounter(modeCounters, "taxi"),
            Unknown = GetModeCounter(modeCounters, "unknown")
        };
    }

    private static string NormalizeMobilityMode(string? rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return kUnknownTransportMode;
        }

        if (ContainsIgnoreCase(rawToken, "bus"))
        {
            return "bus";
        }

        if (ContainsIgnoreCase(rawToken, "tram"))
        {
            return "tram";
        }

        if (ContainsIgnoreCase(rawToken, "subway") || ContainsIgnoreCase(rawToken, "metro"))
        {
            return "subway";
        }

        if (ContainsIgnoreCase(rawToken, "ferry"))
        {
            return "ferry";
        }

        if (ContainsIgnoreCase(rawToken, "ship"))
        {
            return "ship";
        }

        if (ContainsIgnoreCase(rawToken, "train") || ContainsIgnoreCase(rawToken, "rail"))
        {
            return "train";
        }

        if (ContainsIgnoreCase(rawToken, "air") || ContainsIgnoreCase(rawToken, "heli"))
        {
            return "air";
        }

        if (ContainsIgnoreCase(rawToken, "taxi"))
        {
            return "taxi";
        }

        return kUnknownTransportMode;
    }

    private static string NormalizeLineSchedule(int schedule)
    {
        return schedule switch
        {
            0 => "day",
            1 => "night",
            2 => "day_and_night",
            _ => "unknown"
        };
    }

    private static string? TryResolveObservedLineName(
        EntityManager entityManager,
        NameSystem nameSystem,
        Entity lineEntity,
        Type? lineComponentType)
    {
        try
        {
            object lineName = nameSystem.GetName(lineEntity);
            if (TryExtractDisplayNameFromObject(lineName, out string extracted))
            {
                return extracted;
            }

            if (TryNormalizeDisplayName(lineName.ToString(), out extracted))
            {
                return extracted;
            }
        }
        catch
        {
            // Best-effort NameSystem extraction, fallback below.
        }

        try
        {
            object keyboardName = nameSystem.GetNameForVirtualKeyboard(lineEntity);
            if (TryExtractDisplayNameFromObject(keyboardName, out string extracted))
            {
                return extracted;
            }

            if (TryNormalizeDisplayName(keyboardName.ToString(), out extracted))
            {
                return extracted;
            }
        }
        catch
        {
            // Best-effort NameSystem extraction, fallback below.
        }

        if (lineComponentType == null)
        {
            return null;
        }

        var resolutionState = new LineNameResolutionState();
        return TryResolveLineDisplayName(entityManager, lineEntity, lineComponentType, resolutionState);
    }

    private static string? TryResolveObservedLineColor(EntityManager entityManager, Entity lineEntity)
    {
        if (!entityManager.HasComponent<Game.Routes.Color>(lineEntity))
        {
            return null;
        }

        Game.Routes.Color colorData = entityManager.GetComponentData<Game.Routes.Color>(lineEntity);
        return TryToHexColor(colorData.m_Color);
    }

    private static string? TryToHexColor(object? rawColor)
    {
        if (rawColor == null)
        {
            return null;
        }

        Type colorType = rawColor.GetType();
        if (!TryGetColorChannelValue(rawColor, colorType, "r", out int r) ||
            !TryGetColorChannelValue(rawColor, colorType, "g", out int g) ||
            !TryGetColorChannelValue(rawColor, colorType, "b", out int b))
        {
            return null;
        }

        return "#" +
               r.ToString("X2", CultureInfo.InvariantCulture) +
               g.ToString("X2", CultureInfo.InvariantCulture) +
               b.ToString("X2", CultureInfo.InvariantCulture);
    }

    private static bool TryGetColorChannelValue(object rawColor, Type colorType, string channelName, out int value)
    {
        value = 0;

        FieldInfo? field = colorType.GetField(channelName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        object? fieldValue = field?.GetValue(rawColor);
        if (TryConvertColorChannel(fieldValue, out value))
        {
            return true;
        }

        PropertyInfo? property = colorType.GetProperty(channelName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        object? propertyValue = property?.GetValue(rawColor);
        return TryConvertColorChannel(propertyValue, out value);
    }

    private static bool TryConvertColorChannel(object? rawValue, out int value)
    {
        value = 0;
        if (rawValue == null)
        {
            return false;
        }

        try
        {
            double numeric = Convert.ToDouble(rawValue, CultureInfo.InvariantCulture);
            if (numeric <= 1.0)
            {
                numeric *= 255.0;
            }

            if (numeric < 0.0)
            {
                numeric = 0.0;
            }
            else if (numeric > 255.0)
            {
                numeric = 255.0;
            }

            value = (int)Math.Round(numeric, MidpointRounding.AwayFromZero);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? TryResolveXtmLineAcronym(EntityManager entityManager, Entity lineEntity, Type? xtmRouteExtraDataType)
    {
        if (xtmRouteExtraDataType == null)
        {
            return null;
        }

        if (!TryGetComponentDataBoxed(entityManager, lineEntity, xtmRouteExtraDataType, out object? componentData, out _))
        {
            return null;
        }

        if (componentData == null)
        {
            return null;
        }

        Type dataType = componentData.GetType();
        PropertyInfo? acronymProperty = dataType.GetProperty("Acronym", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (acronymProperty != null && acronymProperty.CanRead)
        {
            try
            {
                object? raw = acronymProperty.GetValue(componentData);
                string candidate = raw?.ToString()?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    return candidate;
                }
            }
            catch
            {
                // Fall through to field scan.
            }
        }

        FieldInfo[] fields = dataType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        for (int i = 0; i < fields.Length; i++)
        {
            FieldInfo field = fields[i];
            if (!ContainsIgnoreCase(field.Name, "acronym"))
            {
                continue;
            }

            string candidate = field.GetValue(componentData)?.ToString()?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static Dictionary<string, int> CreateMobilityModeCounterDictionary()
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

    public EconomySignalsSummary CollectEconomySignalsSummary()
    {
        if (!TryGetEntityManager(out EntityManager entityManager, out string availabilityReason))
        {
            return new EconomySignalsSummary
            {
                Status = MetricStatus.Unavailable,
                Notes = new[] { availabilityReason },
                MetricMetadata = CreateEconomyMetricMetadata()
            };
        }

        bool hasHouseholdEconomy = TryScanHouseholdEconomy(entityManager, out HouseholdEconomyScanResult householdEconomy, out string? householdError);

        var notes = new List<string>();
        if (hasHouseholdEconomy)
        {
            notes.Add("citizen wealth metrics are observed from Household.m_Resources across moved-in local households.");
            if (householdEconomy.WasSampled)
            {
                notes.Add("household economy scan used sampling guardrails; percentile outputs are sample-based estimates.");
            }
        }
        else
        {
            notes.Add("citizen wealth metrics unavailable: " + (householdError ?? "household economy scan unavailable."));
        }

        notes.Add("land value fields are currently unavailable pending stable ECS component mapping for policy-safe extraction.");

        return new EconomySignalsSummary
        {
            Status = ComputeStatus(
                availableMetrics: (hasHouseholdEconomy ? 4 : 0),
                expectedMetrics: 8),
            LandValueAvg = null,
            LandValueP25 = null,
            LandValueP50 = null,
            LandValueP75 = null,
            CitizenWealthAvg = hasHouseholdEconomy ? householdEconomy.Average : null,
            CitizenWealthP25 = hasHouseholdEconomy ? householdEconomy.P25 : null,
            CitizenWealthP50 = hasHouseholdEconomy ? householdEconomy.P50 : null,
            CitizenWealthP75 = hasHouseholdEconomy ? householdEconomy.P75 : null,
            SourceComponent = "ecs.economy_signals:Game.Citizens.Household.m_Resources|Game.Buildings.LandValue",
            MetricMetadata = CreateEconomyMetricMetadata(),
            Notes = notes.ToArray()
        };
    }

    public ExternalConnectionsSummary CollectExternalConnectionsSummary()
    {
        if (!TryGetEntityManager(out EntityManager entityManager, out string availabilityReason))
        {
            return new ExternalConnectionsSummary
            {
                Status = MetricStatus.Unavailable,
                Notes = new[] { availabilityReason },
                MetricMetadata = CreateExternalConnectionsMetricMetadata()
            };
        }

        CountResult outsideConnectionCount = TryCountByAll(
            entityManager,
            new[] { "Game.Objects.OutsideConnection", "Game.Net.OutsideConnection" });

        var notes = new List<string>
        {
            "external-connection value metrics remain unavailable until stable ECS mappings for trade value aggregates are verified.",
            "service_trade fields target electricity/water/sewage import-export values when a validated component contract is available."
        };
        AddResultNotes(notes, "outside_connection_entities", outsideConnectionCount);

        return new ExternalConnectionsSummary
        {
            Status = outsideConnectionCount.Count.HasValue ? MetricStatus.Partial : MetricStatus.Unavailable,
            ImportsTotalValue = null,
            ExportsTotalValue = null,
            ImportsByResource = null,
            ExportsByResource = null,
            ServiceTrade = null,
            SourceComponent = BuildSourceComponent("ecs.external_connections", outsideConnectionCount),
            MetricMetadata = CreateExternalConnectionsMetricMetadata(),
            Notes = notes.ToArray()
        };
    }

    public LaborMarketDetailSummary CollectLaborMarketDetailSummary()
    {
        if (!TryGetEntityManager(out EntityManager entityManager, out string availabilityReason))
        {
            return new LaborMarketDetailSummary
            {
                Status = MetricStatus.Unavailable,
                Notes = new[] { availabilityReason },
                MetricMetadata = CreateLaborMarketMetricMetadata()
            };
        }

        bool hasWorkforceScan = TryScanPopulationAndWorkforce(entityManager, out PopulationWorkforceScanResult workforceScan, out string? workforceError);
        bool hasWorkplaceScan = TryScanWorkplaces(entityManager, out WorkplacesScanResult workplaceScan, out string? workplaceError);

        LevelCountSummary? jobsAvailableByLevel = null;
        LevelCountSummary? jobsFilledByLevel = null;
        LevelCountSummary? jobsOpenByLevel = null;
        WorkforceByEducationSummary? workforceByLevel = null;

        var notes = new List<string>();

        if (hasWorkplaceScan)
        {
            jobsAvailableByLevel = CreateLevelCountSummary(
                workplaceScan.Levels[0].Total,
                workplaceScan.Levels[1].Total,
                workplaceScan.Levels[2].Total,
                workplaceScan.Levels[3].Total,
                workplaceScan.Levels[4].Total);

            jobsFilledByLevel = CreateLevelCountSummary(
                workplaceScan.Levels[0].Employees,
                workplaceScan.Levels[1].Employees,
                workplaceScan.Levels[2].Employees,
                workplaceScan.Levels[3].Employees,
                workplaceScan.Levels[4].Employees);

            jobsOpenByLevel = CreateLevelCountSummary(
                workplaceScan.Levels[0].Open,
                workplaceScan.Levels[1].Open,
                workplaceScan.Levels[2].Open,
                workplaceScan.Levels[3].Open,
                workplaceScan.Levels[4].Open);

            notes.Add("job availability/fill/open by education level is observed from WorkProvider+Employee workplace buffers.");
            if (workplaceScan.WasSampled)
            {
                notes.Add("job-level workplace metrics used sampling guardrails; values are scaled estimates.");
            }
        }
        else
        {
            notes.Add("jobs_*_by_education_level unavailable: " + (workplaceError ?? "workplace scan unavailable."));
        }

        if (hasWorkforceScan)
        {
            workforceByLevel = new WorkforceByEducationSummary
            {
                Potential = CreateLevelCountSummary(
                    workforceScan.WorkforceLevels[0].Total,
                    workforceScan.WorkforceLevels[1].Total,
                    workforceScan.WorkforceLevels[2].Total,
                    workforceScan.WorkforceLevels[3].Total,
                    workforceScan.WorkforceLevels[4].Total),
                Workers = CreateLevelCountSummary(
                    workforceScan.WorkforceLevels[0].Workers,
                    workforceScan.WorkforceLevels[1].Workers,
                    workforceScan.WorkforceLevels[2].Workers,
                    workforceScan.WorkforceLevels[3].Workers,
                    workforceScan.WorkforceLevels[4].Workers),
                Unemployed = CreateLevelCountSummary(
                    workforceScan.WorkforceLevels[0].Unemployed,
                    workforceScan.WorkforceLevels[1].Unemployed,
                    workforceScan.WorkforceLevels[2].Unemployed,
                    workforceScan.WorkforceLevels[3].Unemployed,
                    workforceScan.WorkforceLevels[4].Unemployed),
                Underemployed = CreateLevelCountSummary(
                    workforceScan.WorkforceLevels[0].Under,
                    workforceScan.WorkforceLevels[1].Under,
                    workforceScan.WorkforceLevels[2].Under,
                    workforceScan.WorkforceLevels[3].Under,
                    workforceScan.WorkforceLevels[4].Under)
            };

            notes.Add("workforce_by_education_level is observed from Citizen/Worker/Student state and Worker.m_Level mismatch logic.");
            if (workforceScan.WasSampled)
            {
                notes.Add("workforce_by_education_level used sampling guardrails; values are scaled estimates.");
            }
        }
        else
        {
            notes.Add("workforce_by_education_level unavailable: " + (workforceError ?? "workforce scan unavailable."));
        }

        return new LaborMarketDetailSummary
        {
            Status = ComputeStatus(
                availableMetrics: CountPresent(jobsAvailableByLevel) +
                                  CountPresent(jobsFilledByLevel) +
                                  CountPresent(jobsOpenByLevel) +
                                  CountPresent(workforceByLevel),
                expectedMetrics: 4),
            JobsAvailableByEducationLevel = jobsAvailableByLevel,
            JobsFilledByEducationLevel = jobsFilledByLevel,
            JobsOpenByEducationLevel = jobsOpenByLevel,
            WorkforceByEducationLevel = workforceByLevel,
            SourceComponent = "ecs.labor_market_detail:Game.Companies.WorkProvider|Game.Companies.Employee|Game.Citizens.Citizen|Game.Citizens.Worker|Game.Citizens.Student",
            MetricMetadata = CreateLaborMarketMetricMetadata(),
            Notes = notes.ToArray()
        };
    }

    private bool TryScanTransportLineUsage(
        EntityManager entityManager,
        out TransportLineUsageScanResult result,
        out string? error)
    {
        error = null;
        Type? lineComponentType = ResolveFirstComponentType(s_passengerLineCandidates);
        if (lineComponentType == null)
        {
            result = default;
            error = "transport line component type was not resolved.";
            return false;
        }

        Type? lineDataType = ResolveFirstComponentType(s_transportLineDataCandidates);
        Type? transportTypeEnumType = ResolveFirstComponentType(s_transportTypeEnumCandidates);
        Type? passengerBufferType = ResolveFirstComponentType(s_vehiclePassengerBufferCandidates);
        Type? vehicleLineOwnerReferenceType = ResolveFirstComponentType(s_vehicleLineOwnerReferenceCandidates);
        Type? publicTransportVehicleType = ResolveFirstComponentType(s_transportPublicVehicleCandidates);

        bool usedFallbackTransportTypeClassification = false;
        bool passengerBufferUnavailable = passengerBufferType == null;
        bool vehicleLineMappingUnavailable = publicTransportVehicleType == null;
        bool usedCurrentVehicleOccupancyFallback = false;
        bool currentVehicleOccupancyUnavailable = false;
        int totalPublicVehiclesSeen = 0;
        int mappedPublicTransportVehicles = 0;
        int mappedPublicTransportVehiclesWithPassengers = 0;
        var lineNameResolution = new LineNameResolutionState();
        var mappedVehiclesWithPassengersByMode = CreateModeCounterDictionary();
        var passengerCapacityByPrefab = new Dictionary<Entity, int?>();
        IReadOnlyDictionary<Entity, int>? occupancyByVehicle = null;

        if (TryBuildCurrentVehicleOccupancyMap(entityManager, out Dictionary<Entity, int> occupancyMap, out string? occupancyMapError))
        {
            occupancyByVehicle = occupancyMap;
        }
        else if (!string.IsNullOrWhiteSpace(occupancyMapError))
        {
            currentVehicleOccupancyUnavailable = true;
        }

        var linesByEntity = new Dictionary<Entity, MutableTransportLineUsage>();
        var lineEntities = new HashSet<Entity>();

        try
        {
            var lineQuery = entityManager.CreateEntityQuery(
                new EntityQueryDesc
                {
                    All = new[]
                    {
                        ComponentType.ReadOnly(lineComponentType)
                    },
                    None = new[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>()
                    }
                });

            NativeArray<Entity> lineArray = lineQuery.ToEntityArray(Allocator.TempJob);
            try
            {
                for (int i = 0; i < lineArray.Length; i++)
                {
                    Entity lineEntity = lineArray[i];
                    lineEntities.Add(lineEntity);
                    string lineMode = ResolveLineTransportMode(
                        entityManager,
                        lineEntity,
                        lineComponentType,
                        lineDataType,
                        transportTypeEnumType);
                    string? lineName = TryResolveLineDisplayName(entityManager, lineEntity, lineComponentType, lineNameResolution);
                    lineNameResolution.TotalLineCount++;
                    if (!string.IsNullOrWhiteSpace(lineName))
                    {
                        lineNameResolution.ResolvedLineNameCount++;
                    }

                    linesByEntity[lineEntity] = new MutableTransportLineUsage
                    {
                        LineEntity = lineEntity,
                        Mode = lineMode,
                        LineName = lineName
                    };
                }
            }
            finally
            {
                lineArray.Dispose();
            }
        }
        catch (Exception ex)
        {
            result = default;
            error = ex.GetType().Name + ": " + ex.Message;
            return false;
        }

        if (lineEntities.Count == 0)
        {
            result = new TransportLineUsageScanResult(
                LineUsageByTransportType: CreateEmptyLineUsageByTransportType(),
                LinesByTransportType: CreateEmptyModeEntityCounts(),
                ActiveVehiclesByTransportType: CreateEmptyModeEntityCounts(),
                OnboardPassengersByTransportType: CreateEmptyModeEntityCounts(),
                LineUsageAvgPercent: null,
                LineUsageP95Percent: null,
                LinesWithServiceCount: 0,
                LinesWithoutServiceCount: 0,
                LinesWithServicePercent: null,
                MappedPublicTransportVehicles: 0,
                UnmappedPublicTransportVehicles: 0,
                MappedPublicTransportVehiclesWithPassengers: 0,
                MappedPublicTransportVehiclesWithPassengersPercent: null,
                LineVehicleEntitiesP50: null,
                LineVehicleEntitiesP95: null,
                LineOnboardPassengersP50: null,
                LineOnboardPassengersP95: null,
                VehiclesWithPassengersCount: 0,
                VehiclesWithPassengersPercent: null,
                VehiclesWithPassengersByTransportType: CreateEmptyModeEntityCounts(),
                VehiclesWithPassengersPercentByTransportType: CreateEmptyModeDoubleValues(),
                AvgOnboardPassengersPerActiveVehicle: null,
                AvgOnboardPassengersPerActiveVehicleByTransportType: CreateEmptyModeDoubleValues(),
                TopLinesByUsageProxy: Array.Empty<TransportLineTopSummary>(),
                TopLinesByOnboardPassengers: Array.Empty<TransportLineTopSummary>(),
                TopLinesByActiveVehicles: Array.Empty<TransportLineTopSummary>(),
                LineNameTotalCount: 0,
                LineNameResolvedCount: 0,
                LineNameResolvedFromLineComponentCount: 0,
                LineNameResolvedFromLineNameComponentCount: 0,
                LineNameResolvedFromPrefabNameComponentCount: 0,
                LineNameResolvedFromLineComponentScanCount: 0,
                LineNameResolvedFromPrefabComponentScanCount: 0,
                LineNameResolvedComponentTypes: Array.Empty<string>(),
                LineNameLineCandidatePresenceSummary: Array.Empty<string>(),
                LineNamePrefabCandidatePresenceSummary: Array.Empty<string>(),
                UsedFallbackTransportTypeClassification: false,
                PassengerBufferUnavailable: passengerBufferUnavailable,
                VehicleLineMappingUnavailable: vehicleLineMappingUnavailable,
                UsedCurrentVehicleOccupancyFallback: false,
                CurrentVehicleOccupancyUnavailable: currentVehicleOccupancyUnavailable);
            return true;
        }

        if (publicTransportVehicleType != null)
        {
            try
            {
                var vehicleQuery = entityManager.CreateEntityQuery(
                    new EntityQueryDesc
                    {
                        All = new[]
                        {
                            ComponentType.ReadOnly(publicTransportVehicleType)
                        },
                        None = new[]
                        {
                            ComponentType.ReadOnly<Deleted>(),
                            ComponentType.ReadOnly<Temp>()
                        }
                    });

                NativeArray<Entity> vehicleArray = vehicleQuery.ToEntityArray(Allocator.TempJob);
                try
                {
                    int sampleStride = ComputeSamplingStride(vehicleArray.Length, _sampling.MaxWorkplaceEntities);
                    for (int i = 0; i < vehicleArray.Length; i += sampleStride)
                    {
                        Entity vehicleEntity = vehicleArray[i];
                        totalPublicVehiclesSeen++;
                        bool hasLineUsage = TryExtractLineUsageFromVehicle(
                            entityManager,
                            vehicleEntity,
                            lineEntities,
                            vehicleLineOwnerReferenceType,
                            passengerBufferType,
                            out Entity lineEntity,
                            out int? onboardPassengerCount,
                            out string vehicleMode,
                            out string? vehicleScanError);

                        if (!string.IsNullOrWhiteSpace(vehicleScanError))
                        {
                            passengerBufferUnavailable = true;
                        }

                        if (!hasLineUsage)
                        {
                            continue;
                        }

                        mappedPublicTransportVehicles++;
                        if (!linesByEntity.TryGetValue(lineEntity, out MutableTransportLineUsage? lineUsage))
                        {
                            continue;
                        }

                        if ((!onboardPassengerCount.HasValue || onboardPassengerCount.Value <= 0) &&
                            occupancyByVehicle != null &&
                            occupancyByVehicle.TryGetValue(vehicleEntity, out int fallbackPassengerCount) &&
                            fallbackPassengerCount > 0)
                        {
                            onboardPassengerCount = fallbackPassengerCount;
                            usedCurrentVehicleOccupancyFallback = true;
                        }

                        if (string.Equals(lineUsage.Mode, kUnknownTransportMode, StringComparison.Ordinal) &&
                            !string.Equals(vehicleMode, kUnknownTransportMode, StringComparison.Ordinal))
                        {
                            lineUsage.Mode = vehicleMode;
                            usedFallbackTransportTypeClassification = true;
                        }

                        lineUsage.ActiveVehicleEntities++;
                        string normalizedLineMode = NormalizeTransportMode(lineUsage.Mode);
                        if (TryResolveVehiclePassengerCapacity(entityManager, vehicleEntity, passengerCapacityByPrefab, out int passengerCapacity))
                        {
                            lineUsage.TotalPassengerCapacity = (lineUsage.TotalPassengerCapacity ?? 0) + passengerCapacity;
                        }
                        else
                        {
                            lineUsage.MissingCapacityVehicleCount++;
                        }

                        if (onboardPassengerCount.HasValue)
                        {
                            lineUsage.OnboardPassengerEntities = (lineUsage.OnboardPassengerEntities ?? 0) + onboardPassengerCount.Value;
                            if (onboardPassengerCount.Value > 0)
                            {
                                mappedPublicTransportVehiclesWithPassengers++;
                                IncrementModeCounter(mappedVehiclesWithPassengersByMode, normalizedLineMode, 1);
                            }
                        }
                        else if (passengerBufferType != null)
                        {
                            lineUsage.OnboardPassengerEntities ??= 0;
                        }
                    }

                    if (sampleStride > 1)
                    {
                        ScaleLineUsageCounters(linesByEntity, sampleStride, vehicleArray.Length);
                        totalPublicVehiclesSeen = ScaleSampledCount(totalPublicVehiclesSeen, sampleStride, vehicleArray.Length);
                        mappedPublicTransportVehicles = ScaleSampledCount(mappedPublicTransportVehicles, sampleStride, vehicleArray.Length);
                        mappedPublicTransportVehiclesWithPassengers = ScaleSampledCount(mappedPublicTransportVehiclesWithPassengers, sampleStride, vehicleArray.Length);
                        ScaleModeCounterDictionary(mappedVehiclesWithPassengersByMode, sampleStride, vehicleArray.Length);
                    }
                }
                finally
                {
                    vehicleArray.Dispose();
                }
            }
            catch (Exception ex)
            {
                vehicleLineMappingUnavailable = true;
                error = ex.GetType().Name + ": " + ex.Message;
            }
        }

        var groupedByMode = new Dictionary<string, List<MutableTransportLineUsage>>(StringComparer.Ordinal);
        foreach (MutableTransportLineUsage lineUsage in linesByEntity.Values)
        {
            string mode = NormalizeTransportMode(lineUsage.Mode);
            lineUsage.Mode = mode;
            if (!groupedByMode.TryGetValue(mode, out List<MutableTransportLineUsage>? list))
            {
                list = new List<MutableTransportLineUsage>();
                groupedByMode[mode] = list;
            }

            list.Add(lineUsage);
        }

        var allUsageValues = new List<double>();

        TransportTypeLineUsageSummary bus = BuildTransportTypeLineUsageSummary(groupedByMode, "bus", allUsageValues);
        TransportTypeLineUsageSummary tram = BuildTransportTypeLineUsageSummary(groupedByMode, "tram", allUsageValues);
        TransportTypeLineUsageSummary subway = BuildTransportTypeLineUsageSummary(groupedByMode, "subway", allUsageValues);
        TransportTypeLineUsageSummary train = BuildTransportTypeLineUsageSummary(groupedByMode, "train", allUsageValues);
        TransportTypeLineUsageSummary ship = BuildTransportTypeLineUsageSummary(groupedByMode, "ship", allUsageValues);
        TransportTypeLineUsageSummary air = BuildTransportTypeLineUsageSummary(groupedByMode, "air", allUsageValues);
        TransportTypeLineUsageSummary taxi = BuildTransportTypeLineUsageSummary(groupedByMode, "taxi", allUsageValues);
        TransportTypeLineUsageSummary unknown = BuildTransportTypeLineUsageSummary(groupedByMode, "unknown", allUsageValues);

        ModeEntityCounts linesByTransportType = CreateModeEntityCounts(
            bus.LineCount,
            tram.LineCount,
            subway.LineCount,
            train.LineCount,
            ship.LineCount,
            air.LineCount,
            taxi.LineCount,
            unknown.LineCount);
        ModeEntityCounts activeVehiclesByTransportType = CreateModeEntityCounts(
            bus.ActiveVehicleEntities,
            tram.ActiveVehicleEntities,
            subway.ActiveVehicleEntities,
            train.ActiveVehicleEntities,
            ship.ActiveVehicleEntities,
            air.ActiveVehicleEntities,
            taxi.ActiveVehicleEntities,
            unknown.ActiveVehicleEntities);
        ModeEntityCounts onboardPassengersByTransportType = CreateModeEntityCounts(
            bus.OnboardPassengerEntities,
            tram.OnboardPassengerEntities,
            subway.OnboardPassengerEntities,
            train.OnboardPassengerEntities,
            ship.OnboardPassengerEntities,
            air.OnboardPassengerEntities,
            taxi.OnboardPassengerEntities,
            unknown.OnboardPassengerEntities);
        if (passengerBufferUnavailable)
        {
            onboardPassengersByTransportType = CreateUnavailableModeEntityCounts();
        }

        int linesWithServiceCount = CountLinesWithService(linesByEntity.Values);
        int linesWithoutServiceCount = Math.Max(0, linesByEntity.Count - linesWithServiceCount);
        double? linesWithServicePercent = CalculatePercent(linesWithServiceCount, linesByEntity.Count);
        int unmappedPublicTransportVehicles = Math.Max(0, totalPublicVehiclesSeen - mappedPublicTransportVehicles);
        double? mappedPublicTransportVehiclesWithPassengersPercent = CalculatePercent(
            mappedPublicTransportVehiclesWithPassengers,
            mappedPublicTransportVehicles);

        int? mappedPublicTransportVehiclesWithPassengersValue = passengerBufferUnavailable
            ? (int?)null
            : mappedPublicTransportVehiclesWithPassengers;
        double? mappedPublicTransportVehiclesWithPassengersPercentValue = passengerBufferUnavailable
            ? null
            : mappedPublicTransportVehiclesWithPassengersPercent;

        int? vehiclesWithPassengersCount = passengerBufferUnavailable
            ? (int?)null
            : mappedPublicTransportVehiclesWithPassengers;
        double? vehiclesWithPassengersPercent = passengerBufferUnavailable
            ? null
            : mappedPublicTransportVehiclesWithPassengersPercent;
        ModeEntityCounts vehiclesWithPassengersByTransportType = passengerBufferUnavailable
            ? CreateUnavailableModeEntityCounts()
            : CreateModeEntityCounts(
                GetModeCounter(mappedVehiclesWithPassengersByMode, "bus"),
                GetModeCounter(mappedVehiclesWithPassengersByMode, "tram"),
                GetModeCounter(mappedVehiclesWithPassengersByMode, "subway"),
                GetModeCounter(mappedVehiclesWithPassengersByMode, "train"),
                GetModeCounter(mappedVehiclesWithPassengersByMode, "ship"),
                GetModeCounter(mappedVehiclesWithPassengersByMode, "air"),
                GetModeCounter(mappedVehiclesWithPassengersByMode, "taxi"),
                GetModeCounter(mappedVehiclesWithPassengersByMode, "unknown"));

        ModeDoubleValues vehiclesWithPassengersPercentByTransportType = passengerBufferUnavailable
            ? CreateEmptyModeDoubleValues()
            : CreateModeDoubleValues(
                CalculatePercent(GetModeCounter(mappedVehiclesWithPassengersByMode, "bus"), bus.ActiveVehicleEntities),
                CalculatePercent(GetModeCounter(mappedVehiclesWithPassengersByMode, "tram"), tram.ActiveVehicleEntities),
                CalculatePercent(GetModeCounter(mappedVehiclesWithPassengersByMode, "subway"), subway.ActiveVehicleEntities),
                CalculatePercent(GetModeCounter(mappedVehiclesWithPassengersByMode, "train"), train.ActiveVehicleEntities),
                CalculatePercent(GetModeCounter(mappedVehiclesWithPassengersByMode, "ship"), ship.ActiveVehicleEntities),
                CalculatePercent(GetModeCounter(mappedVehiclesWithPassengersByMode, "air"), air.ActiveVehicleEntities),
                CalculatePercent(GetModeCounter(mappedVehiclesWithPassengersByMode, "taxi"), taxi.ActiveVehicleEntities),
                CalculatePercent(GetModeCounter(mappedVehiclesWithPassengersByMode, "unknown"), unknown.ActiveVehicleEntities));

        double? avgOnboardPassengersPerActiveVehicle = passengerBufferUnavailable
            ? null
            : CalculateAveragePerVehicle(
                onboardPassengersTotal: SumModeCountValues(
                    bus.OnboardPassengerEntities,
                    tram.OnboardPassengerEntities,
                    subway.OnboardPassengerEntities,
                    train.OnboardPassengerEntities,
                    ship.OnboardPassengerEntities,
                    air.OnboardPassengerEntities,
                    taxi.OnboardPassengerEntities,
                    unknown.OnboardPassengerEntities),
                activeVehicleCount: mappedPublicTransportVehicles);

        ModeDoubleValues avgOnboardPassengersPerActiveVehicleByTransportType = passengerBufferUnavailable
            ? CreateEmptyModeDoubleValues()
            : CreateModeDoubleValues(
                CalculateAveragePerVehicle(bus.OnboardPassengerEntities, bus.ActiveVehicleEntities),
                CalculateAveragePerVehicle(tram.OnboardPassengerEntities, tram.ActiveVehicleEntities),
                CalculateAveragePerVehicle(subway.OnboardPassengerEntities, subway.ActiveVehicleEntities),
                CalculateAveragePerVehicle(train.OnboardPassengerEntities, train.ActiveVehicleEntities),
                CalculateAveragePerVehicle(ship.OnboardPassengerEntities, ship.ActiveVehicleEntities),
                CalculateAveragePerVehicle(air.OnboardPassengerEntities, air.ActiveVehicleEntities),
                CalculateAveragePerVehicle(taxi.OnboardPassengerEntities, taxi.ActiveVehicleEntities),
                CalculateAveragePerVehicle(unknown.OnboardPassengerEntities, unknown.ActiveVehicleEntities));

        var lineVehicleEntityCounts = new List<int>(linesByEntity.Count);
        var lineOnboardPassengerCounts = new List<int>(linesByEntity.Count);
        foreach (MutableTransportLineUsage lineUsage in linesByEntity.Values)
        {
            lineVehicleEntityCounts.Add(lineUsage.ActiveVehicleEntities);
            if (lineUsage.OnboardPassengerEntities.HasValue)
            {
                lineOnboardPassengerCounts.Add(lineUsage.OnboardPassengerEntities.Value);
            }
        }

        double? lineVehicleEntitiesP50 = null;
        double? lineVehicleEntitiesP95 = null;
        if (lineVehicleEntityCounts.Count > 0)
        {
            lineVehicleEntityCounts.Sort();
            lineVehicleEntitiesP50 = Math.Round(Percentile(lineVehicleEntityCounts, 0.50), 2, MidpointRounding.AwayFromZero);
            lineVehicleEntitiesP95 = Math.Round(Percentile(lineVehicleEntityCounts, 0.95), 2, MidpointRounding.AwayFromZero);
        }

        double? lineOnboardPassengersP50 = null;
        double? lineOnboardPassengersP95 = null;
        if (lineOnboardPassengerCounts.Count > 0)
        {
            lineOnboardPassengerCounts.Sort();
            lineOnboardPassengersP50 = Math.Round(Percentile(lineOnboardPassengerCounts, 0.50), 2, MidpointRounding.AwayFromZero);
            lineOnboardPassengersP95 = Math.Round(Percentile(lineOnboardPassengerCounts, 0.95), 2, MidpointRounding.AwayFromZero);
        }

        double? lineUsageAvgPercent = null;
        double? lineUsageP95Percent = null;
        if (allUsageValues.Count > 0)
        {
            lineUsageAvgPercent = Math.Round(Average(allUsageValues), 2, MidpointRounding.AwayFromZero);
            lineUsageP95Percent = Math.Round(Percentile(allUsageValues, 0.95), 2, MidpointRounding.AwayFromZero);
        }

        TransportLineTopSummary[] topLinesByUsageProxy = CreateTopLineSummariesByUsage(linesByEntity.Values, maxCount: 25);
        TransportLineTopSummary[] topLinesByOnboardPassengers = CreateTopLineSummariesByOnboardPassengers(linesByEntity.Values, maxCount: 25);
        TransportLineTopSummary[] topLinesByActiveVehicles = CreateTopLineSummariesByActiveVehicles(linesByEntity.Values, maxCount: 25);
        string[] lineNameResolvedComponentTypes = lineNameResolution.BuildResolvedComponentTypes();
        string[] lineNameLineCandidatePresenceSummary = lineNameResolution.BuildLineCandidatePresenceSummary();
        string[] lineNamePrefabCandidatePresenceSummary = lineNameResolution.BuildPrefabCandidatePresenceSummary();

        result = new TransportLineUsageScanResult(
            LineUsageByTransportType: new LineUsageByTransportType
            {
                Bus = bus,
                Tram = tram,
                Subway = subway,
                Train = train,
                Ship = ship,
                Air = air,
                Taxi = taxi,
                Unknown = unknown
            },
            LinesByTransportType: linesByTransportType,
            ActiveVehiclesByTransportType: activeVehiclesByTransportType,
            OnboardPassengersByTransportType: onboardPassengersByTransportType,
            LineUsageAvgPercent: lineUsageAvgPercent,
            LineUsageP95Percent: lineUsageP95Percent,
            LinesWithServiceCount: linesWithServiceCount,
            LinesWithoutServiceCount: linesWithoutServiceCount,
            LinesWithServicePercent: linesWithServicePercent,
            MappedPublicTransportVehicles: mappedPublicTransportVehicles,
            UnmappedPublicTransportVehicles: unmappedPublicTransportVehicles,
            MappedPublicTransportVehiclesWithPassengers: mappedPublicTransportVehiclesWithPassengersValue,
            MappedPublicTransportVehiclesWithPassengersPercent: mappedPublicTransportVehiclesWithPassengersPercentValue,
            LineVehicleEntitiesP50: lineVehicleEntitiesP50,
            LineVehicleEntitiesP95: lineVehicleEntitiesP95,
            LineOnboardPassengersP50: lineOnboardPassengersP50,
            LineOnboardPassengersP95: lineOnboardPassengersP95,
            VehiclesWithPassengersCount: vehiclesWithPassengersCount,
            VehiclesWithPassengersPercent: vehiclesWithPassengersPercent,
            VehiclesWithPassengersByTransportType: vehiclesWithPassengersByTransportType,
            VehiclesWithPassengersPercentByTransportType: vehiclesWithPassengersPercentByTransportType,
            AvgOnboardPassengersPerActiveVehicle: avgOnboardPassengersPerActiveVehicle,
            AvgOnboardPassengersPerActiveVehicleByTransportType: avgOnboardPassengersPerActiveVehicleByTransportType,
            TopLinesByUsageProxy: topLinesByUsageProxy,
            TopLinesByOnboardPassengers: topLinesByOnboardPassengers,
            TopLinesByActiveVehicles: topLinesByActiveVehicles,
            LineNameTotalCount: lineNameResolution.TotalLineCount,
            LineNameResolvedCount: lineNameResolution.ResolvedLineNameCount,
            LineNameResolvedFromLineComponentCount: lineNameResolution.ResolvedFromLineComponentCount,
            LineNameResolvedFromLineNameComponentCount: lineNameResolution.ResolvedFromLineNameComponentCount,
            LineNameResolvedFromPrefabNameComponentCount: lineNameResolution.ResolvedFromPrefabNameComponentCount,
            LineNameResolvedFromLineComponentScanCount: lineNameResolution.ResolvedFromLineComponentScanCount,
            LineNameResolvedFromPrefabComponentScanCount: lineNameResolution.ResolvedFromPrefabComponentScanCount,
            LineNameResolvedComponentTypes: lineNameResolvedComponentTypes,
            LineNameLineCandidatePresenceSummary: lineNameLineCandidatePresenceSummary,
            LineNamePrefabCandidatePresenceSummary: lineNamePrefabCandidatePresenceSummary,
            UsedFallbackTransportTypeClassification: usedFallbackTransportTypeClassification,
            PassengerBufferUnavailable: passengerBufferUnavailable,
            VehicleLineMappingUnavailable: vehicleLineMappingUnavailable,
            UsedCurrentVehicleOccupancyFallback: usedCurrentVehicleOccupancyFallback,
            CurrentVehicleOccupancyUnavailable: currentVehicleOccupancyUnavailable);

        return true;
    }

    private static LineUsageByTransportType CreateEmptyLineUsageByTransportType()
    {
        return new LineUsageByTransportType
        {
            Bus = BuildEmptyTransportTypeLineUsageSummary(),
            Tram = BuildEmptyTransportTypeLineUsageSummary(),
            Subway = BuildEmptyTransportTypeLineUsageSummary(),
            Train = BuildEmptyTransportTypeLineUsageSummary(),
            Ship = BuildEmptyTransportTypeLineUsageSummary(),
            Air = BuildEmptyTransportTypeLineUsageSummary(),
            Taxi = BuildEmptyTransportTypeLineUsageSummary(),
            Unknown = BuildEmptyTransportTypeLineUsageSummary()
        };
    }

    private static ModeEntityCounts CreateEmptyModeEntityCounts()
    {
        return new ModeEntityCounts
        {
            Bus = 0,
            Tram = 0,
            Subway = 0,
            Train = 0,
            Ship = 0,
            Air = 0,
            Taxi = 0,
            Unknown = 0
        };
    }

    private static ModeEntityCounts CreateUnavailableModeEntityCounts()
    {
        return new ModeEntityCounts
        {
            Bus = null,
            Tram = null,
            Subway = null,
            Train = null,
            Ship = null,
            Air = null,
            Taxi = null,
            Unknown = null
        };
    }

    private static ModeEntityCounts CreateModeEntityCounts(
        int? bus,
        int? tram,
        int? subway,
        int? train,
        int? ship,
        int? air,
        int? taxi,
        int? unknown)
    {
        return new ModeEntityCounts
        {
            Bus = bus,
            Tram = tram,
            Subway = subway,
            Train = train,
            Ship = ship,
            Air = air,
            Taxi = taxi,
            Unknown = unknown
        };
    }

    private static ModeDoubleValues CreateEmptyModeDoubleValues()
    {
        return new ModeDoubleValues
        {
            Bus = null,
            Tram = null,
            Subway = null,
            Train = null,
            Ship = null,
            Air = null,
            Taxi = null,
            Unknown = null
        };
    }

    private static ModeDoubleValues CreateModeDoubleValues(
        double? bus,
        double? tram,
        double? subway,
        double? train,
        double? ship,
        double? air,
        double? taxi,
        double? unknown)
    {
        return new ModeDoubleValues
        {
            Bus = bus,
            Tram = tram,
            Subway = subway,
            Train = train,
            Ship = ship,
            Air = air,
            Taxi = taxi,
            Unknown = unknown
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
            ["air"] = 0,
            ["taxi"] = 0,
            ["unknown"] = 0
        };
    }

    private static void IncrementModeCounter(Dictionary<string, int> counters, string mode, int increment)
    {
        string normalized = NormalizeTransportMode(mode);
        if (!counters.TryGetValue(normalized, out int current))
        {
            current = 0;
        }

        counters[normalized] = current + increment;
    }

    private static int GetModeCounter(IReadOnlyDictionary<string, int> counters, string mode)
    {
        string normalized = NormalizeTransportMode(mode);
        return counters.TryGetValue(normalized, out int value) ? value : 0;
    }

    private static void ScaleModeCounterDictionary(Dictionary<string, int> counters, int sampleStride, int maxCap)
    {
        if (sampleStride <= 1)
        {
            return;
        }

        string[] keys = new string[counters.Count];
        counters.Keys.CopyTo(keys, 0);
        for (int i = 0; i < keys.Length; i++)
        {
            string key = keys[i];
            counters[key] = ScaleSampledCount(GetModeCounter(counters, key), sampleStride, maxCap);
        }
    }

    private static void ScaleEntityCountDictionary(Dictionary<Entity, int> counters, int sampleStride, int maxCap)
    {
        if (sampleStride <= 1)
        {
            return;
        }

        Entity[] keys = new Entity[counters.Count];
        counters.Keys.CopyTo(keys, 0);
        for (int i = 0; i < keys.Length; i++)
        {
            Entity key = keys[i];
            counters[key] = ScaleSampledCount(counters[key], sampleStride, maxCap);
        }
    }

    private static int? SumModeCountValues(
        int? bus,
        int? tram,
        int? subway,
        int? train,
        int? ship,
        int? air,
        int? taxi,
        int? unknown)
    {
        bool hasAny = false;
        int total = 0;
        hasAny |= AddIfPresent(ref total, bus);
        hasAny |= AddIfPresent(ref total, tram);
        hasAny |= AddIfPresent(ref total, subway);
        hasAny |= AddIfPresent(ref total, train);
        hasAny |= AddIfPresent(ref total, ship);
        hasAny |= AddIfPresent(ref total, air);
        hasAny |= AddIfPresent(ref total, taxi);
        hasAny |= AddIfPresent(ref total, unknown);
        return hasAny ? total : (int?)null;
    }

    private static bool AddIfPresent(ref int total, int? value)
    {
        if (!value.HasValue)
        {
            return false;
        }

        total += value.Value;
        return true;
    }

    private static double? CalculateAveragePerVehicle(int? onboardPassengersTotal, int activeVehicleCount)
    {
        if (!onboardPassengersTotal.HasValue || activeVehicleCount <= 0)
        {
            return null;
        }

        return Math.Round(
            onboardPassengersTotal.Value / (double)activeVehicleCount,
            2,
            MidpointRounding.AwayFromZero);
    }

    private static TransportTypeLineUsageSummary BuildEmptyTransportTypeLineUsageSummary()
    {
        return new TransportTypeLineUsageSummary
        {
            LineCount = 0,
            ActiveVehicleEntities = 0,
            OnboardPassengerEntities = 0,
            LineUsageAvgPercent = null,
            LineUsageP95Percent = null,
            LinesWithServiceCount = 0,
            LinesWithoutServiceCount = 0,
            VehiclesPerLineAvg = null,
            Lines = Array.Empty<TransportLineUsageEntry>()
        };
    }

    private static TransportTypeLineUsageSummary BuildTransportTypeLineUsageSummary(
        IReadOnlyDictionary<string, List<MutableTransportLineUsage>> groupedByMode,
        string mode,
        List<double> allUsageValues)
    {
        if (!groupedByMode.TryGetValue(mode, out List<MutableTransportLineUsage>? lines) || lines.Count == 0)
        {
            return BuildEmptyTransportTypeLineUsageSummary();
        }

        int maxOnboard = 0;
        bool hasOnboardData = false;
        int activeVehicleTotal = 0;
        int onboardPassengerTotal = 0;
        for (int i = 0; i < lines.Count; i++)
        {
            MutableTransportLineUsage line = lines[i];
            activeVehicleTotal += line.ActiveVehicleEntities;
            if (line.OnboardPassengerEntities.HasValue)
            {
                hasOnboardData = true;
                onboardPassengerTotal += line.OnboardPassengerEntities.Value;
                if (line.OnboardPassengerEntities.Value > maxOnboard)
                {
                    maxOnboard = line.OnboardPassengerEntities.Value;
                }
            }
        }

        var modeUsageValues = new List<double>(lines.Count);
        var entries = new List<TransportLineUsageEntry>(lines.Count);
        int linesWithServiceCount = 0;
        for (int i = 0; i < lines.Count; i++)
        {
            MutableTransportLineUsage line = lines[i];
            if (line.ActiveVehicleEntities > 0)
            {
                linesWithServiceCount++;
            }

            double? usagePercentProxy = null;
            double? usagePercent = null;
            if (hasOnboardData)
            {
                int onboard = line.OnboardPassengerEntities ?? 0;
                if (maxOnboard > 0)
                {
                    usagePercentProxy = Math.Round((onboard * 100.0) / maxOnboard, 2, MidpointRounding.AwayFromZero);
                }
                else
                {
                    usagePercentProxy = 0;
                }

                modeUsageValues.Add(usagePercentProxy.Value);
                allUsageValues.Add(usagePercentProxy.Value);
            }

            if (line.TotalPassengerCapacity.HasValue && line.TotalPassengerCapacity.Value > 0 && line.MissingCapacityVehicleCount == 0)
            {
                int onboard = line.OnboardPassengerEntities ?? 0;
                usagePercent = Math.Round((onboard * 100.0) / line.TotalPassengerCapacity.Value, 2, MidpointRounding.AwayFromZero);
            }

            line.UsagePercent = usagePercent;
            line.UsagePercentProxy = usagePercentProxy;
            entries.Add(new TransportLineUsageEntry
            {
                LineEntityIndex = line.LineEntity.Index,
                LineEntityVersion = line.LineEntity.Version,
                LineName = line.LineName,
                ActiveVehicleEntities = line.ActiveVehicleEntities,
                OnboardPassengerEntities = line.OnboardPassengerEntities,
                TotalPassengerCapacity = line.TotalPassengerCapacity,
                UsagePercent = usagePercent,
                UsagePercentProxy = usagePercentProxy
            });
        }

        entries.Sort(
            (left, right) =>
            {
                int usageComparison = Nullable.Compare(right.UsagePercentProxy, left.UsagePercentProxy);
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

        return new TransportTypeLineUsageSummary
        {
            LineCount = lines.Count,
            ActiveVehicleEntities = activeVehicleTotal,
            OnboardPassengerEntities = hasOnboardData ? onboardPassengerTotal : null,
            LineUsageAvgPercent = modeUsageValues.Count > 0
                ? Math.Round(Average(modeUsageValues), 2, MidpointRounding.AwayFromZero)
                : null,
            LineUsageP95Percent = modeUsageValues.Count > 0
                ? Math.Round(Percentile(modeUsageValues, 0.95), 2, MidpointRounding.AwayFromZero)
                : null,
            LinesWithServiceCount = linesWithServiceCount,
            LinesWithoutServiceCount = Math.Max(0, lines.Count - linesWithServiceCount),
            VehiclesPerLineAvg = lines.Count > 0
                ? Math.Round(activeVehicleTotal / (double)lines.Count, 2, MidpointRounding.AwayFromZero)
                : (double?)null,
            Lines = entries.ToArray()
        };
    }

    private static int CountLinesWithService(IEnumerable<MutableTransportLineUsage> lines)
    {
        int count = 0;
        foreach (MutableTransportLineUsage line in lines)
        {
            if (line.ActiveVehicleEntities > 0)
            {
                count++;
            }
        }

        return count;
    }

    private static TransportLineTopSummary[] CreateTopLineSummariesByUsage(IEnumerable<MutableTransportLineUsage> lines, int maxCount)
    {
        var list = new List<MutableTransportLineUsage>();
        foreach (MutableTransportLineUsage line in lines)
        {
            if (line.UsagePercentProxy.HasValue)
            {
                list.Add(line);
            }
        }

        list.Sort(
            (left, right) =>
            {
                int usageComparison = Nullable.Compare(right.UsagePercentProxy, left.UsagePercentProxy);
                if (usageComparison != 0)
                {
                    return usageComparison;
                }

                int passengerComparison = Nullable.Compare(right.OnboardPassengerEntities, left.OnboardPassengerEntities);
                if (passengerComparison != 0)
                {
                    return passengerComparison;
                }

                int vehicleComparison = right.ActiveVehicleEntities.CompareTo(left.ActiveVehicleEntities);
                if (vehicleComparison != 0)
                {
                    return vehicleComparison;
                }

                return left.LineEntity.Index.CompareTo(right.LineEntity.Index);
            });

        return CreateTopLineSummariesFromSorted(list, maxCount);
    }

    private static TransportLineTopSummary[] CreateTopLineSummariesByOnboardPassengers(IEnumerable<MutableTransportLineUsage> lines, int maxCount)
    {
        var list = new List<MutableTransportLineUsage>();
        foreach (MutableTransportLineUsage line in lines)
        {
            if (line.OnboardPassengerEntities.HasValue)
            {
                list.Add(line);
            }
        }

        list.Sort(
            (left, right) =>
            {
                int passengerComparison = Nullable.Compare(right.OnboardPassengerEntities, left.OnboardPassengerEntities);
                if (passengerComparison != 0)
                {
                    return passengerComparison;
                }

                int usageComparison = Nullable.Compare(right.UsagePercentProxy, left.UsagePercentProxy);
                if (usageComparison != 0)
                {
                    return usageComparison;
                }

                int vehicleComparison = right.ActiveVehicleEntities.CompareTo(left.ActiveVehicleEntities);
                if (vehicleComparison != 0)
                {
                    return vehicleComparison;
                }

                return left.LineEntity.Index.CompareTo(right.LineEntity.Index);
            });

        return CreateTopLineSummariesFromSorted(list, maxCount);
    }

    private static TransportLineTopSummary[] CreateTopLineSummariesByActiveVehicles(IEnumerable<MutableTransportLineUsage> lines, int maxCount)
    {
        var list = new List<MutableTransportLineUsage>();
        foreach (MutableTransportLineUsage line in lines)
        {
            list.Add(line);
        }

        list.Sort(
            (left, right) =>
            {
                int vehicleComparison = right.ActiveVehicleEntities.CompareTo(left.ActiveVehicleEntities);
                if (vehicleComparison != 0)
                {
                    return vehicleComparison;
                }

                int passengerComparison = Nullable.Compare(right.OnboardPassengerEntities, left.OnboardPassengerEntities);
                if (passengerComparison != 0)
                {
                    return passengerComparison;
                }

                int usageComparison = Nullable.Compare(right.UsagePercentProxy, left.UsagePercentProxy);
                if (usageComparison != 0)
                {
                    return usageComparison;
                }

                return left.LineEntity.Index.CompareTo(right.LineEntity.Index);
            });

        return CreateTopLineSummariesFromSorted(list, maxCount);
    }

    private static TransportLineTopSummary[] CreateTopLineSummariesFromSorted(IReadOnlyList<MutableTransportLineUsage> sorted, int maxCount)
    {
        if (maxCount <= 0 || sorted.Count == 0)
        {
            return Array.Empty<TransportLineTopSummary>();
        }

        int take = Math.Min(maxCount, sorted.Count);
        var results = new TransportLineTopSummary[take];
        for (int i = 0; i < take; i++)
        {
            MutableTransportLineUsage line = sorted[i];
            results[i] = new TransportLineTopSummary
            {
                Mode = line.Mode,
                LineEntityIndex = line.LineEntity.Index,
                LineEntityVersion = line.LineEntity.Version,
                LineName = line.LineName,
                ActiveVehicleEntities = line.ActiveVehicleEntities,
                OnboardPassengerEntities = line.OnboardPassengerEntities,
                UsagePercentProxy = line.UsagePercentProxy
            };
        }

        return results;
    }

    private static string? TryResolveLineDisplayName(
        EntityManager entityManager,
        Entity lineEntity,
        Type lineComponentType,
        LineNameResolutionState resolutionState)
    {
        if (TryGetComponentDataBoxed(entityManager, lineEntity, lineComponentType, out object? lineComponentData, out _) &&
            TryExtractDisplayNameFromObject(lineComponentData, out string lineName))
        {
            resolutionState.ResolvedFromLineComponentCount++;
            resolutionState.RecordResolvedComponentType(lineComponentType);
            return lineName;
        }

        for (int i = 0; i < s_transportLineNameCandidates.Length; i++)
        {
            Type? nameType = ResolveComponentType(s_transportLineNameCandidates[i]);
            if (nameType == null || !HasComponentByType(entityManager, lineEntity, nameType))
            {
                continue;
            }

            resolutionState.RecordCandidatePresence(nameType, onPrefab: false);
            if (TryGetComponentDataBoxed(entityManager, lineEntity, nameType, out object? nameData, out _) &&
                TryExtractDisplayNameFromObject(nameData, out string extracted))
            {
                resolutionState.ResolvedFromLineNameComponentCount++;
                resolutionState.RecordResolvedComponentType(nameType);
                return extracted;
            }
        }

        if (TryResolveDisplayNameFromEntityComponents(
                entityManager,
                lineEntity,
                out string? deepScannedLineName,
                out Type? deepScannedLineType,
                preferNameLikeComponents: true))
        {
            resolutionState.ResolvedFromLineComponentScanCount++;
            resolutionState.RecordResolvedComponentType(deepScannedLineType);
            return deepScannedLineName;
        }
        if (TryResolveDisplayNameFromEntityComponents(
                entityManager,
                lineEntity,
                out deepScannedLineName,
                out deepScannedLineType,
                preferNameLikeComponents: false))
        {
            resolutionState.ResolvedFromLineComponentScanCount++;
            resolutionState.RecordResolvedComponentType(deepScannedLineType);
            return deepScannedLineName;
        }

        if (!entityManager.HasComponent<PrefabRef>(lineEntity))
        {
            return null;
        }

        PrefabRef prefabRef = entityManager.GetComponentData<PrefabRef>(lineEntity);
        Entity prefabEntity = prefabRef.m_Prefab;
        if (prefabEntity == Entity.Null)
        {
            return null;
        }

        for (int i = 0; i < s_transportLineNameCandidates.Length; i++)
        {
            Type? nameType = ResolveComponentType(s_transportLineNameCandidates[i]);
            if (nameType == null || !HasComponentByType(entityManager, prefabEntity, nameType))
            {
                continue;
            }

            resolutionState.RecordCandidatePresence(nameType, onPrefab: true);
            if (TryGetComponentDataBoxed(entityManager, prefabEntity, nameType, out object? nameData, out _) &&
                TryExtractDisplayNameFromObject(nameData, out string extracted))
            {
                resolutionState.ResolvedFromPrefabNameComponentCount++;
                resolutionState.RecordResolvedComponentType(nameType);
                return extracted;
            }
        }

        if (TryResolveDisplayNameFromEntityComponents(
                entityManager,
                prefabEntity,
                out string? deepScannedPrefabName,
                out Type? deepScannedPrefabType,
                preferNameLikeComponents: true))
        {
            resolutionState.ResolvedFromPrefabComponentScanCount++;
            resolutionState.RecordResolvedComponentType(deepScannedPrefabType);
            return deepScannedPrefabName;
        }
        if (TryResolveDisplayNameFromEntityComponents(
                entityManager,
                prefabEntity,
                out deepScannedPrefabName,
                out deepScannedPrefabType,
                preferNameLikeComponents: false))
        {
            resolutionState.ResolvedFromPrefabComponentScanCount++;
            resolutionState.RecordResolvedComponentType(deepScannedPrefabType);
            return deepScannedPrefabName;
        }

        return null;
    }

    private static bool TryExtractDisplayNameFromObject(object? componentData, out string displayName)
    {
        displayName = string.Empty;
        if (componentData == null)
        {
            return false;
        }

        Type type = componentData.GetType();
        string? bestCandidate = null;
        int bestScore = -1;

        FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        for (int i = 0; i < fields.Length; i++)
        {
            FieldInfo field = fields[i];
            if (!ContainsIgnoreCase(field.Name, "name"))
            {
                continue;
            }

            object? raw = field.GetValue(componentData);
            if (!TryNormalizeDisplayName(raw, out string candidate))
            {
                continue;
            }

            int score = ScoreDisplayNameMember(field.Name);
            if (score < 0)
            {
                continue;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestCandidate = candidate;
            }
        }

        PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        for (int i = 0; i < properties.Length; i++)
        {
            PropertyInfo property = properties[i];
            if (!property.CanRead || property.GetIndexParameters().Length != 0 || !ContainsIgnoreCase(property.Name, "name"))
            {
                continue;
            }

            try
            {
                object? raw = property.GetValue(componentData);
                if (!TryNormalizeDisplayName(raw, out string candidate))
                {
                    continue;
                }

                int score = ScoreDisplayNameMember(property.Name);
                if (score < 0)
                {
                    continue;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCandidate = candidate;
                }
            }
            catch
            {
                // Best-effort reflection for unstable component contracts.
            }
        }

        if (bestCandidate == null)
        {
            return false;
        }

        displayName = bestCandidate;
        return true;
    }

    private static bool TryNormalizeDisplayName(object? raw, out string displayName)
    {
        displayName = string.Empty;
        if (raw == null)
        {
            return false;
        }

        string candidate = string.Empty;
        if (raw is string text)
        {
            candidate = text.Trim();
        }
        else if (raw is IEnumerable enumerable)
        {
            string? bestCandidate = null;
            int bestScore = int.MinValue;
            int inspected = 0;
            foreach (object? value in enumerable)
            {
                if (inspected++ >= 16)
                {
                    break;
                }

                string elementText = value?.ToString()?.Trim() ?? string.Empty;
                if (!TryNormalizeDisplayNameText(elementText, out string normalized))
                {
                    continue;
                }

                int score = normalized.Length;
                if (ContainsIgnoreCase(normalized, "line") || ContainsIgnoreCase(normalized, "route"))
                {
                    score += 20;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCandidate = normalized;
                }
            }

            if (bestCandidate == null)
            {
                return false;
            }

            displayName = bestCandidate;
            return true;
        }
        else
        {
            candidate = raw.ToString()?.Trim() ?? string.Empty;
        }

        return TryNormalizeDisplayNameText(candidate, out displayName);
    }

    private static bool TryNormalizeDisplayNameText(string candidate, out string displayName)
    {
        displayName = string.Empty;
        candidate = candidate.Replace('\r', ' ').Replace('\n', ' ').Trim();
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        if (string.Equals(candidate, "System.String[]", StringComparison.Ordinal))
        {
            return false;
        }

        if (candidate.Length < 2 || candidate.Length > 120)
        {
            return false;
        }

        if (candidate.StartsWith("Game.", StringComparison.OrdinalIgnoreCase) ||
            candidate.StartsWith("Unity.", StringComparison.OrdinalIgnoreCase) ||
            candidate.StartsWith("Colossal.", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (candidate.IndexOf("::", StringComparison.Ordinal) >= 0)
        {
            return false;
        }

        string lower = candidate.ToLowerInvariant();
        if (lower is "name" or "linename" or "displayname" or "customname" or
            "number" or
            "bus" or "tram" or "subway" or "metro" or "train" or "ship" or "air" or "taxi" or "unknown")
        {
            return false;
        }

        bool hasLetter = false;
        for (int i = 0; i < candidate.Length; i++)
        {
            if (char.IsLetter(candidate[i]))
            {
                hasLetter = true;
                break;
            }
        }

        if (!hasLetter)
        {
            return false;
        }

        displayName = candidate;
        return true;
    }

    private static int ScoreDisplayNameMember(string memberName)
    {
        if (ContainsIgnoreCase(memberName, "Type") ||
            ContainsIgnoreCase(memberName, "Key") ||
            ContainsIgnoreCase(memberName, "Id") ||
            ContainsIgnoreCase(memberName, "Hash") ||
            ContainsIgnoreCase(memberName, "Index"))
        {
            return -1;
        }

        if (string.Equals(memberName, "m_Name", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(memberName, "Name", StringComparison.OrdinalIgnoreCase))
        {
            return 100;
        }

        int score = 10;
        if (ContainsIgnoreCase(memberName, "Display"))
        {
            score += 40;
        }

        if (ContainsIgnoreCase(memberName, "Custom"))
        {
            score += 30;
        }

        if (ContainsIgnoreCase(memberName, "Line") || ContainsIgnoreCase(memberName, "Route"))
        {
            score += 20;
        }

        if (memberName.StartsWith("m_", StringComparison.Ordinal))
        {
            score += 5;
        }

        return score;
    }

    private static bool TryResolveDisplayNameFromEntityComponents(
        EntityManager entityManager,
        Entity entity,
        out string? displayName,
        out Type? resolvedComponentType,
        bool preferNameLikeComponents)
    {
        displayName = null;
        resolvedComponentType = null;

        NativeArray<ComponentType> componentTypes;
        try
        {
            componentTypes = entityManager.GetComponentTypes(entity, Allocator.TempJob);
        }
        catch
        {
            return false;
        }

        try
        {
            string? bestCandidate = null;
            Type? bestType = null;
            int bestScore = int.MinValue;

            for (int i = 0; i < componentTypes.Length; i++)
            {
                Type? managedType = GetManagedType(componentTypes[i]);
                if (managedType == null || !typeof(IComponentData).IsAssignableFrom(managedType))
                {
                    continue;
                }

                if (preferNameLikeComponents && !IsLikelyLineNameComponentType(managedType))
                {
                    continue;
                }

                if (!TryGetComponentDataBoxed(entityManager, entity, managedType, out object? componentData, out _) ||
                    !TryExtractDisplayNameFromObject(componentData, out string candidate))
                {
                    continue;
                }

                int score = ScoreLineNameComponentType(managedType);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCandidate = candidate;
                    bestType = managedType;
                }
            }

            if (bestCandidate == null || bestType == null)
            {
                return false;
            }

            displayName = bestCandidate;
            resolvedComponentType = bestType;
            return true;
        }
        finally
        {
            componentTypes.Dispose();
        }
    }

    private static bool IsLikelyLineNameComponentType(Type componentType)
    {
        string token = (componentType.FullName ?? componentType.Name).ToLowerInvariant();
        return token.Contains("name") ||
               token.Contains("line") ||
               token.Contains("route") ||
               token.Contains("label");
    }

    private static int ScoreLineNameComponentType(Type componentType)
    {
        string token = (componentType.FullName ?? componentType.Name).ToLowerInvariant();

        int score = 0;
        if (token.Contains("transportline") && token.Contains("name"))
        {
            score += 220;
        }
        else if (token.Contains("route") && token.Contains("name"))
        {
            score += 200;
        }
        else if (token.Contains("name"))
        {
            score += 120;
        }

        if (token.Contains("display"))
        {
            score += 40;
        }

        if (token.Contains("custom"))
        {
            score += 30;
        }

        if (token.Contains("line"))
        {
            score += 20;
        }

        if (token.Contains("route"))
        {
            score += 20;
        }

        if (token.Contains("label"))
        {
            score += 10;
        }

        if (token.Contains("owner") ||
            token.Contains("target") ||
            token.Contains("prefabref"))
        {
            score -= 120;
        }

        return score;
    }

    private static string ResolveLineTransportMode(
        EntityManager entityManager,
        Entity lineEntity,
        Type lineComponentType,
        Type? lineDataType,
        Type? transportTypeEnumType)
    {
        if (TryGetComponentDataBoxed(entityManager, lineEntity, lineComponentType, out object? lineComponentData, out _) &&
            TryExtractTransportModeToken(lineComponentData, transportTypeEnumType, out string modeFromLine))
        {
            return modeFromLine;
        }

        if (!entityManager.HasComponent<PrefabRef>(lineEntity))
        {
            return kUnknownTransportMode;
        }

        Entity linePrefab = entityManager.GetComponentData<PrefabRef>(lineEntity).m_Prefab;

        if (lineDataType != null &&
            TryGetComponentDataBoxed(entityManager, linePrefab, lineDataType, out object? lineData, out _) &&
            TryExtractTransportModeToken(lineData, transportTypeEnumType, out string modeFromLineData))
        {
            return modeFromLineData;
        }

        for (int i = 0; i < s_transportLineDataCandidates.Length; i++)
        {
            Type? candidateType = ResolveComponentType(s_transportLineDataCandidates[i]);
            if (candidateType == null || (lineDataType != null && candidateType == lineDataType))
            {
                continue;
            }

            if (TryGetComponentDataBoxed(entityManager, linePrefab, candidateType, out object? candidateData, out _) &&
                TryExtractTransportModeToken(candidateData, transportTypeEnumType, out string modeFromCandidate))
            {
                return modeFromCandidate;
            }
        }

        return kUnknownTransportMode;
    }

    private static bool TryExtractLineUsageFromVehicle(
        EntityManager entityManager,
        Entity vehicleEntity,
        HashSet<Entity> lineEntities,
        Type? vehicleLineOwnerReferenceType,
        Type? passengerBufferType,
        out Entity lineEntity,
        out int? onboardPassengerCount,
        out string vehicleMode,
        out string? error)
    {
        error = null;
        lineEntity = Entity.Null;
        onboardPassengerCount = null;
        vehicleMode = kUnknownTransportMode;

        NativeArray<ComponentType> componentTypes = entityManager.GetComponentTypes(vehicleEntity, Allocator.Temp);
        try
        {
            bool hasPassengerBuffer = false;
            for (int i = 0; i < componentTypes.Length; i++)
            {
                ComponentType componentType = componentTypes[i];
                Type? managedType = GetManagedType(componentType);
                if (managedType == null)
                {
                    continue;
                }

                if (string.Equals(vehicleMode, kUnknownTransportMode, StringComparison.Ordinal))
                {
                    string mode = NormalizeTransportMode(managedType.FullName ?? managedType.Name);
                    if (!string.Equals(mode, kUnknownTransportMode, StringComparison.Ordinal))
                    {
                        vehicleMode = mode;
                    }
                }

                if (passengerBufferType != null &&
                    (managedType == passengerBufferType ||
                     string.Equals(managedType.FullName, passengerBufferType.FullName, StringComparison.Ordinal)))
                {
                    hasPassengerBuffer = true;
                }

                if (!typeof(IComponentData).IsAssignableFrom(managedType))
                {
                    continue;
                }

                if (!TryGetComponentDataBoxed(entityManager, vehicleEntity, managedType, out object? componentData, out _))
                {
                    continue;
                }

                if (componentData == null)
                {
                    continue;
                }

                if (TryFindLineReference(entityManager, componentData, lineEntities, vehicleLineOwnerReferenceType, out lineEntity))
                {
                    break;
                }
            }

            if (lineEntity == Entity.Null)
            {
                return false;
            }

            if (passengerBufferType == null)
            {
                onboardPassengerCount = null;
                return true;
            }

            if (!hasPassengerBuffer)
            {
                onboardPassengerCount = 0;
                return true;
            }

            if (TryGetBufferLength(entityManager, vehicleEntity, passengerBufferType, out int bufferLength, out string? bufferError))
            {
                onboardPassengerCount = bufferLength;
                return true;
            }

            onboardPassengerCount = null;
            error = bufferError;
            return true;
        }
        finally
        {
            componentTypes.Dispose();
        }
    }

    private static bool TryFindLineReference(
        EntityManager entityManager,
        object componentData,
        HashSet<Entity> lineEntities,
        Type? vehicleLineOwnerReferenceType,
        out Entity lineEntity)
    {
        lineEntity = Entity.Null;
        FieldInfo[] fields = componentData.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        for (int i = 0; i < fields.Length; i++)
        {
            FieldInfo field = fields[i];
            if (field.FieldType != typeof(Entity))
            {
                continue;
            }

            object? raw = field.GetValue(componentData);
            if (raw is not Entity entityValue || entityValue == Entity.Null)
            {
                continue;
            }

            if (!lineEntities.Contains(entityValue))
            {
                if (TryResolveLineFromReferenceEntity(
                    entityManager,
                    entityValue,
                    lineEntities,
                    vehicleLineOwnerReferenceType,
                    remainingDepth: 2,
                    out lineEntity))
                {
                    return true;
                }

                continue;
            }

            lineEntity = entityValue;
            return true;
        }

        return false;
    }

    private static bool TryResolveLineFromReferenceEntity(
        EntityManager entityManager,
        Entity referenceEntity,
        HashSet<Entity> lineEntities,
        Type? vehicleLineOwnerReferenceType,
        int remainingDepth,
        out Entity lineEntity)
    {
        lineEntity = Entity.Null;
        if (referenceEntity == Entity.Null)
        {
            return false;
        }

        if (lineEntities.Contains(referenceEntity))
        {
            lineEntity = referenceEntity;
            return true;
        }

        if (remainingDepth <= 0 || vehicleLineOwnerReferenceType == null)
        {
            return false;
        }

        if (!TryGetComponentDataBoxed(entityManager, referenceEntity, vehicleLineOwnerReferenceType, out object? ownerRefData, out _))
        {
            return false;
        }

        if (ownerRefData == null)
        {
            return false;
        }

        FieldInfo[] fields = ownerRefData.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        for (int i = 0; i < fields.Length; i++)
        {
            FieldInfo field = fields[i];
            if (field.FieldType != typeof(Entity))
            {
                continue;
            }

            object? raw = field.GetValue(ownerRefData);
            if (raw is not Entity ownerEntity || ownerEntity == Entity.Null)
            {
                continue;
            }

            if (lineEntities.Contains(ownerEntity))
            {
                lineEntity = ownerEntity;
                return true;
            }

            if (TryResolveLineFromReferenceEntity(
                entityManager,
                ownerEntity,
                lineEntities,
                vehicleLineOwnerReferenceType,
                remainingDepth - 1,
                out lineEntity))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryExtractTransportModeToken(object? componentData, Type? transportTypeEnumType, out string mode)
    {
        mode = kUnknownTransportMode;
        if (componentData == null)
        {
            return false;
        }

        FieldInfo[] fields = componentData.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        for (int i = 0; i < fields.Length; i++)
        {
            FieldInfo field = fields[i];
            if (!ContainsIgnoreCase(field.Name, "TransportType") &&
                !ContainsIgnoreCase(field.Name, "RouteType"))
            {
                continue;
            }

            object? value = field.GetValue(componentData);
            if (!TryNormalizeTransportTypeValue(value, transportTypeEnumType, out mode))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private static bool TryNormalizeTransportTypeValue(object? value, Type? transportTypeEnumType, out string mode)
    {
        mode = kUnknownTransportMode;
        if (value == null)
        {
            return false;
        }

        string? token = null;
        Type valueType = value.GetType();
        if (valueType.IsEnum)
        {
            token = value.ToString();
        }
        else if (value is byte || value is sbyte || value is short || value is ushort || value is int || value is uint)
        {
            if (transportTypeEnumType != null && transportTypeEnumType.IsEnum)
            {
                try
                {
                    int enumValue = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                    token = Enum.GetName(transportTypeEnumType, enumValue);
                }
                catch
                {
                    token = null;
                }
            }

            token ??= Convert.ToString(value, CultureInfo.InvariantCulture);
        }
        else if (value is string valueString)
        {
            token = valueString;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        mode = NormalizeTransportMode(token);
        return true;
    }

    private static Type? GetManagedType(ComponentType componentType)
    {
        if (s_componentTypeGetManagedTypeMethod != null)
        {
            try
            {
                object boxed = componentType;
                if (s_componentTypeGetManagedTypeMethod.Invoke(boxed, null) is Type managedType)
                {
                    return managedType;
                }
            }
            catch
            {
                // Fall through to property-based fallback.
            }
        }

        if (s_componentTypeManagedTypeProperty != null && s_componentTypeManagedTypeProperty.PropertyType == typeof(Type))
        {
            try
            {
                object boxed = componentType;
                return s_componentTypeManagedTypeProperty.GetValue(boxed) as Type;
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private static bool HasComponentByType(EntityManager entityManager, Entity entity, Type componentType)
    {
        if (!typeof(IComponentData).IsAssignableFrom(componentType))
        {
            return false;
        }

        if (s_entityManagerHasComponentMethod == null)
        {
            // Older toolchain variants may not expose generic HasComponent<T>(Entity).
            // Fall back to GetComponentData<T> probe behavior.
            return true;
        }

        try
        {
            MethodInfo closed = s_entityManagerHasComponentMethod.MakeGenericMethod(componentType);
            object boxedEntityManager = entityManager;
            object? hasComponent = closed.Invoke(boxedEntityManager, new object[] { entity });
            return hasComponent is bool flag && flag;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryGetComponentDataBoxed(
        EntityManager entityManager,
        Entity entity,
        Type componentType,
        out object? componentData,
        out string? error)
    {
        componentData = null;
        error = null;

        if (s_entityManagerGetComponentDataMethod == null)
        {
            error = "EntityManager.GetComponentData<T>(Entity) is unavailable in this runtime.";
            return false;
        }

        if (!typeof(IComponentData).IsAssignableFrom(componentType))
        {
            error = "Component type does not implement IComponentData: " + componentType.FullName;
            return false;
        }

        try
        {
            MethodInfo closed = s_entityManagerGetComponentDataMethod.MakeGenericMethod(componentType);
            object boxedEntityManager = entityManager;
            componentData = closed.Invoke(boxedEntityManager, new object[] { entity });
            return true;
        }
        catch (Exception ex)
        {
            error = DescribeInvocationError(ex);
            return false;
        }
    }

    private static bool TryGetBufferLength(
        EntityManager entityManager,
        Entity entity,
        Type bufferType,
        out int bufferLength,
        out string? error)
    {
        bufferLength = 0;
        error = null;

        if (s_entityManagerGetBufferMethod == null)
        {
            error = "EntityManager.GetBuffer<T>(Entity) is unavailable in this runtime.";
            return false;
        }

        if (!typeof(IBufferElementData).IsAssignableFrom(bufferType))
        {
            error = "Buffer type does not implement IBufferElementData: " + bufferType.FullName;
            return false;
        }

        try
        {
            MethodInfo closed = s_entityManagerGetBufferMethod.MakeGenericMethod(bufferType);
            object boxedEntityManager = entityManager;
            ParameterInfo[] parameters = closed.GetParameters();
            object? buffer;
            if (parameters.Length == 1)
            {
                buffer = closed.Invoke(boxedEntityManager, new object[] { entity });
            }
            else if (parameters.Length == 2 && parameters[1].ParameterType == typeof(bool))
            {
                buffer = closed.Invoke(boxedEntityManager, new object[] { entity, false });
            }
            else
            {
                buffer = closed.Invoke(boxedEntityManager, new object[] { entity });
            }

            if (buffer == null)
            {
                error = "GetBuffer returned null.";
                return false;
            }

            PropertyInfo? lengthProperty = buffer.GetType().GetProperty("Length", BindingFlags.Instance | BindingFlags.Public);
            if (lengthProperty == null)
            {
                error = "DynamicBuffer length property could not be resolved.";
                return false;
            }

            object? lengthValue = lengthProperty.GetValue(buffer);
            if (lengthValue is not int length)
            {
                error = "DynamicBuffer length property returned non-int value.";
                return false;
            }

            bufferLength = Math.Max(0, length);
            return true;
        }
        catch (Exception ex)
        {
            error = DescribeInvocationError(ex);
            return false;
        }
    }

    private bool TryBuildCurrentVehicleOccupancyMap(
        EntityManager entityManager,
        out Dictionary<Entity, int> occupancyByVehicle,
        out string? error)
    {
        occupancyByVehicle = new Dictionary<Entity, int>();
        error = null;

        Type? currentVehicleType = ResolveFirstComponentType(s_creatureCurrentVehicleCandidates);
        if (currentVehicleType == null)
        {
            return false;
        }

        try
        {
            var query = entityManager.CreateEntityQuery(
                new EntityQueryDesc
                {
                    All = new[]
                    {
                        ComponentType.ReadOnly(currentVehicleType)
                    },
                    None = new[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>()
                    }
                });

            NativeArray<Entity> entities = query.ToEntityArray(Allocator.TempJob);
            try
            {
                int sampleStride = ComputeSamplingStride(entities.Length, _sampling.MaxPopulationEntities);
                for (int i = 0; i < entities.Length; i += sampleStride)
                {
                    Entity occupantEntity = entities[i];
                    if (!TryGetComponentDataBoxed(entityManager, occupantEntity, currentVehicleType, out object? currentVehicleData, out _))
                    {
                        continue;
                    }

                    if (!TryExtractVehicleEntityReference(currentVehicleData, out Entity vehicleEntity))
                    {
                        continue;
                    }

                    if (occupancyByVehicle.TryGetValue(vehicleEntity, out int count))
                    {
                        occupancyByVehicle[vehicleEntity] = count + 1;
                    }
                    else
                    {
                        occupancyByVehicle[vehicleEntity] = 1;
                    }
                }

                if (sampleStride > 1)
                {
                    ScaleEntityCountDictionary(occupancyByVehicle, sampleStride, entities.Length);
                }
            }
            finally
            {
                entities.Dispose();
            }

            return true;
        }
        catch (Exception ex)
        {
            error = ex.GetType().Name + ": " + ex.Message;
            occupancyByVehicle = new Dictionary<Entity, int>();
            return false;
        }
    }

    private static bool TryExtractVehicleEntityReference(object? currentVehicleData, out Entity vehicleEntity)
    {
        vehicleEntity = Entity.Null;
        if (currentVehicleData == null)
        {
            return false;
        }

        FieldInfo[] fields = currentVehicleData.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        for (int i = 0; i < fields.Length; i++)
        {
            FieldInfo field = fields[i];
            if (field.FieldType != typeof(Entity) || !ContainsIgnoreCase(field.Name, "Vehicle"))
            {
                continue;
            }

            object? raw = field.GetValue(currentVehicleData);
            if (raw is not Entity candidate || candidate == Entity.Null)
            {
                continue;
            }

            vehicleEntity = candidate;
            return true;
        }

        return false;
    }

    private static string DescribeInvocationError(Exception ex)
    {
        if (ex is TargetInvocationException tie && tie.InnerException != null)
        {
            return tie.InnerException.GetType().Name + ": " + tie.InnerException.Message;
        }

        return ex.GetType().Name + ": " + ex.Message;
    }

    private static MethodInfo? FindEntityManagerGenericMethod(string methodName, int minParameterCount, int maxParameterCount)
    {
        MethodInfo[] methods = typeof(EntityManager).GetMethods(BindingFlags.Instance | BindingFlags.Public);
        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo method = methods[i];
            if (!method.IsGenericMethodDefinition)
            {
                continue;
            }

            if (!string.Equals(method.Name, methodName, StringComparison.Ordinal))
            {
                continue;
            }

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length < minParameterCount || parameters.Length > maxParameterCount)
            {
                continue;
            }

            if (parameters.Length == 0 || parameters[0].ParameterType != typeof(Entity))
            {
                continue;
            }

            return method;
        }

        return null;
    }

    private static Type? ResolveFirstComponentType(IReadOnlyList<string> candidateTypeNames)
    {
        for (int i = 0; i < candidateTypeNames.Count; i++)
        {
            Type? candidate = ResolveComponentType(candidateTypeNames[i]);
            if (candidate != null)
            {
                return candidate;
            }
        }

        return null;
    }

    private static string NormalizeTransportMode(string? rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return kUnknownTransportMode;
        }

        if (ContainsIgnoreCase(rawToken, "bus"))
        {
            return "bus";
        }

        if (ContainsIgnoreCase(rawToken, "tram"))
        {
            return "tram";
        }

        if (ContainsIgnoreCase(rawToken, "subway") ||
            ContainsIgnoreCase(rawToken, "metro"))
        {
            return "subway";
        }

        if (ContainsIgnoreCase(rawToken, "train") ||
            ContainsIgnoreCase(rawToken, "rail"))
        {
            return "train";
        }

        if (ContainsIgnoreCase(rawToken, "ship") ||
            ContainsIgnoreCase(rawToken, "ferry"))
        {
            return "ship";
        }

        if (ContainsIgnoreCase(rawToken, "air") ||
            ContainsIgnoreCase(rawToken, "heli"))
        {
            return "air";
        }

        if (ContainsIgnoreCase(rawToken, "taxi"))
        {
            return "taxi";
        }

        return kUnknownTransportMode;
    }

    private static bool ContainsIgnoreCase(string? value, string token)
    {
        return value != null &&
               value.Length != 0 &&
               value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static long CreateLineEntityKey(int entityIndex, int entityVersion)
    {
        return ((long)entityVersion << 32) | (uint)entityIndex;
    }

    private static Dictionary<long, TransportLineUsageEntry> BuildLineUsageEntryLookup(LineUsageByTransportType lineUsageByTransportType)
    {
        var lookup = new Dictionary<long, TransportLineUsageEntry>();
        AddLineUsageEntries(lookup, lineUsageByTransportType.Bus?.Lines);
        AddLineUsageEntries(lookup, lineUsageByTransportType.Tram?.Lines);
        AddLineUsageEntries(lookup, lineUsageByTransportType.Subway?.Lines);
        AddLineUsageEntries(lookup, lineUsageByTransportType.Train?.Lines);
        AddLineUsageEntries(lookup, lineUsageByTransportType.Ship?.Lines);
        AddLineUsageEntries(lookup, lineUsageByTransportType.Air?.Lines);
        AddLineUsageEntries(lookup, lineUsageByTransportType.Taxi?.Lines);
        AddLineUsageEntries(lookup, lineUsageByTransportType.Unknown?.Lines);
        return lookup;
    }

    private static void AddLineUsageEntries(
        Dictionary<long, TransportLineUsageEntry> lookup,
        IReadOnlyList<TransportLineUsageEntry>? entries)
    {
        if (entries == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            TransportLineUsageEntry entry = entries[i];
            lookup[CreateLineEntityKey(entry.LineEntityIndex, entry.LineEntityVersion)] = entry;
        }
    }

    private static bool TryGetLineUsageEntry(
        IReadOnlyDictionary<long, TransportLineUsageEntry>? lookup,
        Entity lineEntity,
        out TransportLineUsageEntry? entry)
    {
        entry = null;
        if (lookup == null)
        {
            return false;
        }

        return lookup.TryGetValue(CreateLineEntityKey(lineEntity.Index, lineEntity.Version), out entry);
    }

    private static bool TryResolveVehiclePassengerCapacity(
        EntityManager entityManager,
        Entity vehicleEntity,
        IDictionary<Entity, int?> capacityByPrefab,
        out int passengerCapacity)
    {
        passengerCapacity = 0;

        if (entityManager.HasComponent<PrefabRef>(vehicleEntity))
        {
            Entity prefabEntity = entityManager.GetComponentData<PrefabRef>(vehicleEntity).m_Prefab;
            if (prefabEntity != Entity.Null)
            {
                if (capacityByPrefab.TryGetValue(prefabEntity, out int? cachedCapacity))
                {
                    if (cachedCapacity.HasValue && cachedCapacity.Value > 0)
                    {
                        passengerCapacity = cachedCapacity.Value;
                        return true;
                    }

                    return false;
                }

                if (TryExtractPassengerCapacityFromEntity(entityManager, prefabEntity, out int prefabCapacity))
                {
                    capacityByPrefab[prefabEntity] = prefabCapacity;
                    passengerCapacity = prefabCapacity;
                    return true;
                }

                capacityByPrefab[prefabEntity] = null;
            }
        }

        return TryExtractPassengerCapacityFromEntity(entityManager, vehicleEntity, out passengerCapacity);
    }

    private static bool TryExtractPassengerCapacityFromEntity(
        EntityManager entityManager,
        Entity entity,
        out int passengerCapacity)
    {
        passengerCapacity = 0;
        NativeArray<ComponentType> componentTypes = entityManager.GetComponentTypes(entity, Allocator.Temp);
        try
        {
            int bestScore = int.MinValue;
            for (int i = 0; i < componentTypes.Length; i++)
            {
                Type? managedType = GetManagedType(componentTypes[i]);
                if (managedType == null || !typeof(IComponentData).IsAssignableFrom(managedType))
                {
                    continue;
                }

                if (!TryGetComponentDataBoxed(entityManager, entity, managedType, out object? componentData, out _) ||
                    componentData == null)
                {
                    continue;
                }

                if (!TryExtractPassengerCapacityFromObject(componentData, managedType, out int candidateCapacity, out int candidateScore))
                {
                    continue;
                }

                if (candidateScore > bestScore)
                {
                    bestScore = candidateScore;
                    passengerCapacity = candidateCapacity;
                }
            }

            return passengerCapacity > 0;
        }
        finally
        {
            componentTypes.Dispose();
        }
    }

    private static bool TryExtractPassengerCapacityFromObject(
        object componentData,
        Type ownerType,
        out int passengerCapacity,
        out int score)
    {
        passengerCapacity = 0;
        score = int.MinValue;

        FieldInfo[] fields = ownerType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        for (int i = 0; i < fields.Length; i++)
        {
            if (!TryConvertPositiveInt(fields[i].GetValue(componentData), out int candidateCapacity))
            {
                continue;
            }

            int candidateScore = ScorePassengerCapacityMember(ownerType, fields[i].Name, candidateCapacity);
            if (candidateScore > score)
            {
                score = candidateScore;
                passengerCapacity = candidateCapacity;
            }
        }

        PropertyInfo[] properties = ownerType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        for (int i = 0; i < properties.Length; i++)
        {
            PropertyInfo property = properties[i];
            if (!property.CanRead || property.GetIndexParameters().Length != 0)
            {
                continue;
            }

            if (!TryConvertPositiveInt(property.GetValue(componentData), out int candidateCapacity))
            {
                continue;
            }

            int candidateScore = ScorePassengerCapacityMember(ownerType, property.Name, candidateCapacity);
            if (candidateScore > score)
            {
                score = candidateScore;
                passengerCapacity = candidateCapacity;
            }
        }

        return passengerCapacity > 0 && score > 0;
    }

    private static int ScorePassengerCapacityMember(Type ownerType, string memberName, int candidateCapacity)
    {
        if (candidateCapacity <= 0 || candidateCapacity > 2000)
        {
            return int.MinValue;
        }

        string ownerName = ownerType.FullName ?? ownerType.Name;
        if (ContainsIgnoreCase(ownerName, "cargo") ||
            ContainsIgnoreCase(ownerName, "freight") ||
            ContainsIgnoreCase(ownerName, "storage") ||
            ContainsIgnoreCase(ownerName, "resource"))
        {
            return int.MinValue;
        }

        if (ContainsIgnoreCase(memberName, "fuel") ||
            ContainsIgnoreCase(memberName, "battery") ||
            ContainsIgnoreCase(memberName, "mail") ||
            ContainsIgnoreCase(memberName, "garbage") ||
            ContainsIgnoreCase(memberName, "cargo") ||
            ContainsIgnoreCase(memberName, "resource") ||
            ContainsIgnoreCase(memberName, "weight"))
        {
            return int.MinValue;
        }

        int score = 0;
        if (ContainsIgnoreCase(memberName, "passenger"))
        {
            score += 80;
        }

        if (ContainsIgnoreCase(memberName, "capacity"))
        {
            score += 40;
        }

        if (ContainsIgnoreCase(memberName, "seat"))
        {
            score += 35;
        }

        if (ContainsIgnoreCase(ownerName, "passenger") ||
            ContainsIgnoreCase(ownerName, "publictransport") ||
            ContainsIgnoreCase(ownerName, "transport") ||
            ContainsIgnoreCase(ownerName, "bus") ||
            ContainsIgnoreCase(ownerName, "tram") ||
            ContainsIgnoreCase(ownerName, "subway") ||
            ContainsIgnoreCase(ownerName, "train") ||
            ContainsIgnoreCase(ownerName, "ship") ||
            ContainsIgnoreCase(ownerName, "ferry") ||
            ContainsIgnoreCase(ownerName, "air") ||
            ContainsIgnoreCase(ownerName, "taxi"))
        {
            score += 20;
        }

        return score;
    }

    private static bool TryConvertPositiveInt(object? value, out int result)
    {
        result = 0;
        if (value == null)
        {
            return false;
        }

        try
        {
            switch (value)
            {
                case byte byteValue:
                    result = byteValue;
                    break;
                case sbyte sbyteValue:
                    result = sbyteValue;
                    break;
                case short shortValue:
                    result = shortValue;
                    break;
                case ushort ushortValue:
                    result = ushortValue;
                    break;
                case int intValue:
                    result = intValue;
                    break;
                case uint uintValue when uintValue <= int.MaxValue:
                    result = (int)uintValue;
                    break;
                case long longValue when longValue <= int.MaxValue:
                    result = (int)longValue;
                    break;
                case ulong ulongValue when ulongValue <= int.MaxValue:
                    result = (int)ulongValue;
                    break;
                default:
                    return false;
            }
        }
        catch
        {
            return false;
        }

        return result > 0;
    }

    private static void ScaleLineUsageCounters(Dictionary<Entity, MutableTransportLineUsage> linesByEntity, int sampleStride, int maxCap)
    {
        if (sampleStride <= 1)
        {
            return;
        }

        foreach (KeyValuePair<Entity, MutableTransportLineUsage> pair in linesByEntity)
        {
            MutableTransportLineUsage usage = pair.Value;
            usage.ActiveVehicleEntities = ScaleSampledCount(usage.ActiveVehicleEntities, sampleStride, maxCap);
            if (usage.OnboardPassengerEntities.HasValue)
            {
                usage.OnboardPassengerEntities = ScaleSampledCount(usage.OnboardPassengerEntities.Value, sampleStride, maxCap);
            }

            if (usage.TotalPassengerCapacity.HasValue)
            {
                usage.TotalPassengerCapacity = ScaleSampledCount(usage.TotalPassengerCapacity.Value, sampleStride, maxCap);
            }

            usage.MissingCapacityVehicleCount = ScaleSampledCount(usage.MissingCapacityVehicleCount, sampleStride, maxCap);
        }
    }

    private bool TryGetEntityManager(out EntityManager entityManager, out string reason)
    {
        EntityManager? current = _getEntityManager();
        if (!current.HasValue)
        {
            entityManager = default;
            reason = "runtime ECS world is not attached yet.";
            return false;
        }

        entityManager = current.Value;
        reason = string.Empty;
        return true;
    }

    private bool TryScanPopulationAndWorkforce(
        EntityManager entityManager,
        out PopulationWorkforceScanResult result,
        out string? error)
    {
        error = null;

        var localByEducation = new int[5];
        var potentialByEducation = new int[5];
        var workersByEducation = new int[5];
        var unemployedByEducation = new int[5];
        var homelessByEducation = new int[5];
        var outsideByEducation = new int[5];
        var underByEducation = new int[5];

        int localPopulation = 0;
        int touristPopulation = 0;
        int commuterPopulation = 0;
        int movingAwayPopulation = 0;
        int homelessPopulation = 0;
        int localHouseholds = 0;
        int movingAwayHouseholds = 0;
        int homelessHouseholds = 0;
        int propertyLinkedHouseholds = 0;
        int childrenPopulation = 0;
        int elderlyPopulation = 0;
        int workingAgePopulation = 0;
        bool wasSampled = false;

        try
        {
            var query = entityManager.CreateEntityQuery(
                new EntityQueryDesc
                {
                    All = new[]
                    {
                        ComponentType.ReadOnly<Citizen>(),
                        ComponentType.ReadOnly<HouseholdMember>()
                    },
                    None = new[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>()
                    }
                });

            NativeArray<Entity> entities = query.ToEntityArray(Allocator.TempJob);
            try
            {
                int sampleStride = ComputeSamplingStride(entities.Length, _sampling.MaxPopulationEntities);
                wasSampled = sampleStride > 1;
                for (int i = 0; i < entities.Length; i += sampleStride)
                {
                    Entity citizenEntity = entities[i];
                    Citizen citizen = entityManager.GetComponentData<Citizen>(citizenEntity);
                    HouseholdMember householdMember = entityManager.GetComponentData<HouseholdMember>(citizenEntity);
                    Entity householdEntity = householdMember.m_Household;

                    if (!entityManager.HasComponent<Household>(householdEntity))
                    {
                        continue;
                    }

                    Household household = entityManager.GetComponentData<Household>(householdEntity);
                    if (household.m_Flags == HouseholdFlags.None)
                    {
                        continue;
                    }

                    bool isDead = entityManager.HasComponent<HealthProblem>(citizenEntity) &&
                                  CitizenUtils.IsDead(entityManager.GetComponentData<HealthProblem>(citizenEntity));
                    if (isDead)
                    {
                        continue;
                    }

                    bool isTourist = (citizen.m_State & CitizenFlags.Tourist) != 0;
                    bool isCommuter = (citizen.m_State & CitizenFlags.Commuter) != 0;

                    if (isTourist)
                    {
                        touristPopulation++;
                        continue;
                    }

                    if (isCommuter)
                    {
                        commuterPopulation++;
                        continue;
                    }

                    bool isMovedIn = (household.m_Flags & HouseholdFlags.MovedIn) != 0;
                    if (!isMovedIn)
                    {
                        continue;
                    }

                    bool isHomeless = entityManager.HasComponent<HomelessHousehold>(householdEntity) ||
                                      !entityManager.HasComponent<PropertyRenter>(householdEntity);
                    bool isMovingAwayHousehold = entityManager.HasComponent<MovingAway>(householdEntity);

                    if (isMovingAwayHousehold)
                    {
                        movingAwayPopulation++;
                        continue;
                    }

                    localPopulation++;

                    if (isHomeless)
                    {
                        homelessPopulation++;
                    }

                    CitizenAge age = citizen.GetAge();
                    if (age == CitizenAge.Child)
                    {
                        childrenPopulation++;
                    }
                    else if (age == CitizenAge.Elderly)
                    {
                        elderlyPopulation++;
                    }
                    else
                    {
                        workingAgePopulation++;
                    }

                    int educationLevel = ClampEducationLevel(citizen.GetEducationLevel());
                    localByEducation[educationLevel]++;

                    bool isStudent = entityManager.HasComponent<Game.Citizens.Student>(citizenEntity);
                    if (!IsWorkingAge(age) || isStudent)
                    {
                        continue;
                    }

                    potentialByEducation[educationLevel]++;
                    if (entityManager.HasComponent<Worker>(citizenEntity))
                    {
                        workersByEducation[educationLevel]++;
                        Worker worker = entityManager.GetComponentData<Worker>(citizenEntity);

                        if (entityManager.HasComponent<Game.Objects.OutsideConnection>(worker.m_Workplace))
                        {
                            outsideByEducation[educationLevel]++;
                        }

                        if (worker.m_Level < educationLevel)
                        {
                            underByEducation[educationLevel]++;
                        }
                    }
                    else
                    {
                        unemployedByEducation[educationLevel]++;
                        if (isHomeless)
                        {
                            homelessByEducation[educationLevel]++;
                        }
                    }
                }

                if (sampleStride > 1)
                {
                    ScaleSampledArray(localByEducation, sampleStride, entities.Length);
                    ScaleSampledArray(potentialByEducation, sampleStride, entities.Length);
                    ScaleSampledArray(workersByEducation, sampleStride, entities.Length);
                    ScaleSampledArray(unemployedByEducation, sampleStride, entities.Length);
                    ScaleSampledArray(homelessByEducation, sampleStride, entities.Length);
                    ScaleSampledArray(outsideByEducation, sampleStride, entities.Length);
                    ScaleSampledArray(underByEducation, sampleStride, entities.Length);

                    localPopulation = ScaleSampledCount(localPopulation, sampleStride, entities.Length);
                    touristPopulation = ScaleSampledCount(touristPopulation, sampleStride, entities.Length);
                    commuterPopulation = ScaleSampledCount(commuterPopulation, sampleStride, entities.Length);
                    movingAwayPopulation = ScaleSampledCount(movingAwayPopulation, sampleStride, entities.Length);
                    homelessPopulation = ScaleSampledCount(homelessPopulation, sampleStride, entities.Length);
                    childrenPopulation = ScaleSampledCount(childrenPopulation, sampleStride, entities.Length);
                    elderlyPopulation = ScaleSampledCount(elderlyPopulation, sampleStride, entities.Length);
                    workingAgePopulation = ScaleSampledCount(workingAgePopulation, sampleStride, entities.Length);
                }
            }
            finally
            {
                entities.Dispose();
            }
        }
        catch (Exception ex)
        {
            result = default;
            error = ex.GetType().Name + ": " + ex.Message;
            return false;
        }

        WorkforceLevelSummary[] levels = CreateWorkforceLevels(
            potentialByEducation,
            workersByEducation,
            unemployedByEducation,
            homelessByEducation,
            outsideByEducation,
            underByEducation);

        int totalPotential = SumArray(potentialByEducation);
        int totalWorkers = SumArray(workersByEducation);
        int totalUnemployed = SumArray(unemployedByEducation);
        int totalHomelessUnemployed = SumArray(homelessByEducation);
        int totalOutside = SumArray(outsideByEducation);
        int totalUnder = SumArray(underByEducation);

        if (!TryScanHouseholdPressureContext(
                entityManager,
                out localHouseholds,
                out movingAwayHouseholds,
                out homelessHouseholds,
                out propertyLinkedHouseholds,
                out bool householdScanWasSampled,
                out string? householdError))
        {
            result = default;
            error = householdError;
            return false;
        }

        wasSampled = wasSampled || householdScanWasSampled;

        result = new PopulationWorkforceScanResult(
            LocalPopulation: localPopulation,
            TouristPopulation: touristPopulation,
            CommuterPopulation: commuterPopulation,
            MovingAwayPopulation: movingAwayPopulation,
            HomelessPopulation: homelessPopulation,
            LocalHouseholds: localHouseholds,
            MovingAwayHouseholds: movingAwayHouseholds,
            HomelessHouseholds: homelessHouseholds,
            PropertyLinkedHouseholds: propertyLinkedHouseholds,
            WorkingAgePopulation: workingAgePopulation,
            ChildrenPopulation: childrenPopulation,
            ElderlyPopulation: elderlyPopulation,
            LocalByEducationLevel: localByEducation,
            WorkforceLevels: levels,
            TotalPotentialWorkers: totalPotential,
            TotalWorkers: totalWorkers,
            TotalUnemployed: totalUnemployed,
            TotalHomelessUnemployed: totalHomelessUnemployed,
            TotalOutsideWorkers: totalOutside,
            TotalUnderemployedWorkers: totalUnder,
            TotalEmployable: totalOutside + totalUnder,
            WasSampled: wasSampled);

        return true;
    }

    private bool TryScanWorkplaces(
        EntityManager entityManager,
        out WorkplacesScanResult result,
        out string? error)
    {
        error = null;
        var levels = new WorkplaceCounter[5];

        int providersTotal = 0;
        int providersService = 0;
        int providersCommercial = 0;
        int providersLeisure = 0;
        int providersExtractor = 0;
        int providersIndustrial = 0;
        int providersOffice = 0;
        bool wasSampled = false;

        try
        {
            var query = entityManager.CreateEntityQuery(
                new EntityQueryDesc
                {
                    All = new[]
                    {
                        ComponentType.ReadOnly<Employee>(),
                        ComponentType.ReadOnly<WorkProvider>(),
                        ComponentType.ReadOnly<PrefabRef>()
                    },
                    Any = new[]
                    {
                        ComponentType.ReadOnly<PropertyRenter>(),
                        ComponentType.ReadOnly<Game.Buildings.Building>()
                    },
                    None = new[]
                    {
                        ComponentType.ReadOnly<Temp>()
                    }
                });

            NativeArray<Entity> entities = query.ToEntityArray(Allocator.TempJob);
            try
            {
                int sampleStride = ComputeSamplingStride(entities.Length, _sampling.MaxWorkplaceEntities);
                wasSampled = sampleStride > 1;
                for (int i = 0; i < entities.Length; i += sampleStride)
                {
                    Entity providerEntity = entities[i];
                    PrefabRef prefabRef = entityManager.GetComponentData<PrefabRef>(providerEntity);
                    Entity providerPrefab = prefabRef.m_Prefab;

                    if (!entityManager.HasComponent<WorkplaceData>(providerPrefab))
                    {
                        continue;
                    }

                    WorkProvider workProvider = entityManager.GetComponentData<WorkProvider>(providerEntity);
                    WorkplaceData workplaceData = entityManager.GetComponentData<WorkplaceData>(providerPrefab);
                    DynamicBuffer<Employee> employees = entityManager.GetBuffer<Employee>(providerEntity);

                    int buildingLevel = 1;
                    if (entityManager.HasComponent<PropertyRenter>(providerEntity))
                    {
                        Entity propertyEntity = entityManager.GetComponentData<PropertyRenter>(providerEntity).m_Property;
                        if (entityManager.HasComponent<PrefabRef>(propertyEntity))
                        {
                            Entity propertyPrefab = entityManager.GetComponentData<PrefabRef>(propertyEntity).m_Prefab;
                            if (entityManager.HasComponent<SpawnableBuildingData>(propertyPrefab))
                            {
                                buildingLevel = (int)entityManager.GetComponentData<SpawnableBuildingData>(propertyPrefab).m_Level;
                            }
                        }
                    }

                    EmploymentData workplacesData = EmploymentData.GetWorkplacesData(
                        workProvider.m_MaxWorkers,
                        buildingLevel,
                        workplaceData.m_Complexity);

                    int freePositions = Math.Max(0, workplacesData.total - employees.Length);
                    EmploymentData employeesData = EmploymentData.GetEmployeesData(employees, freePositions);

                    bool isExtractor = entityManager.HasComponent<Game.Companies.ExtractorCompany>(providerEntity);
                    bool isIndustrial = entityManager.HasComponent<Game.Companies.IndustrialCompany>(providerEntity);
                    bool isCommercial = entityManager.HasComponent<Game.Companies.CommercialCompany>(providerEntity);
                    bool isService = !isIndustrial && !isCommercial;

                    bool isOffice = false;
                    bool isLeisure = false;
                    if (entityManager.HasComponent<IndustrialProcessData>(providerPrefab))
                    {
                        IndustrialProcessData process = entityManager.GetComponentData<IndustrialProcessData>(providerPrefab);
                        Resource output = process.m_Output.m_Resource;
                        isLeisure = (output & kLeisureResources) != Resource.NoResource;
                        isOffice = (output & kOfficeResources) != Resource.NoResource;
                    }

                    var commuterByLevel = new int[5];
                    for (int e = 0; e < employees.Length; e++)
                    {
                        Employee employee = employees[e];
                        Entity workerEntity = employee.m_Worker;
                        if (!entityManager.HasComponent<Citizen>(workerEntity))
                        {
                            continue;
                        }

                        Citizen workerCitizen = entityManager.GetComponentData<Citizen>(workerEntity);
                        if ((workerCitizen.m_State & CitizenFlags.Commuter) != 0)
                        {
                            int level = ClampEducationLevel(employee.m_Level);
                            commuterByLevel[level]++;
                        }
                    }

                    AccumulateWorkplaceLevel(
                        levels: levels,
                        level: 0,
                        workplaces: workplacesData.uneducated,
                        employees: employeesData.uneducated,
                        commuters: commuterByLevel[0],
                        isService: isService,
                        isCommercial: isCommercial,
                        isLeisure: isLeisure,
                        isExtractor: isExtractor,
                        isOffice: isOffice);

                    AccumulateWorkplaceLevel(
                        levels: levels,
                        level: 1,
                        workplaces: workplacesData.poorlyEducated,
                        employees: employeesData.poorlyEducated,
                        commuters: commuterByLevel[1],
                        isService: isService,
                        isCommercial: isCommercial,
                        isLeisure: isLeisure,
                        isExtractor: isExtractor,
                        isOffice: isOffice);

                    AccumulateWorkplaceLevel(
                        levels: levels,
                        level: 2,
                        workplaces: workplacesData.educated,
                        employees: employeesData.educated,
                        commuters: commuterByLevel[2],
                        isService: isService,
                        isCommercial: isCommercial,
                        isLeisure: isLeisure,
                        isExtractor: isExtractor,
                        isOffice: isOffice);

                    AccumulateWorkplaceLevel(
                        levels: levels,
                        level: 3,
                        workplaces: workplacesData.wellEducated,
                        employees: employeesData.wellEducated,
                        commuters: commuterByLevel[3],
                        isService: isService,
                        isCommercial: isCommercial,
                        isLeisure: isLeisure,
                        isExtractor: isExtractor,
                        isOffice: isOffice);

                    AccumulateWorkplaceLevel(
                        levels: levels,
                        level: 4,
                        workplaces: workplacesData.highlyEducated,
                        employees: employeesData.highlyEducated,
                        commuters: commuterByLevel[4],
                        isService: isService,
                        isCommercial: isCommercial,
                        isLeisure: isLeisure,
                        isExtractor: isExtractor,
                        isOffice: isOffice);

                    providersTotal++;
                    if (isService)
                    {
                        providersService++;
                    }
                    else if (isCommercial)
                    {
                        if (isLeisure)
                        {
                            providersLeisure++;
                        }
                        else
                        {
                            providersCommercial++;
                        }
                    }
                    else
                    {
                        if (isExtractor)
                        {
                            providersExtractor++;
                        }
                        else if (isOffice)
                        {
                            providersOffice++;
                        }
                        else
                        {
                            providersIndustrial++;
                        }
                    }
                }

                if (sampleStride > 1)
                {
                    ScaleWorkplaceCounters(levels, sampleStride, entities.Length);
                    providersTotal = ScaleSampledCount(providersTotal, sampleStride, entities.Length);
                    providersService = ScaleSampledCount(providersService, sampleStride, entities.Length);
                    providersCommercial = ScaleSampledCount(providersCommercial, sampleStride, entities.Length);
                    providersLeisure = ScaleSampledCount(providersLeisure, sampleStride, entities.Length);
                    providersExtractor = ScaleSampledCount(providersExtractor, sampleStride, entities.Length);
                    providersIndustrial = ScaleSampledCount(providersIndustrial, sampleStride, entities.Length);
                    providersOffice = ScaleSampledCount(providersOffice, sampleStride, entities.Length);
                }
            }
            finally
            {
                entities.Dispose();
            }
        }
        catch (Exception ex)
        {
            result = default;
            error = ex.GetType().Name + ": " + ex.Message;
            return false;
        }

        WorkplaceLevelSummary[] levelSummary = new WorkplaceLevelSummary[5];
        int totalWorkplaces = 0;
        int filledWorkplaces = 0;
        int openWorkplaces = 0;
        int commuterEmployees = 0;
        int serviceWorkplaces = 0;
        int commercialWorkplaces = 0;
        int leisureWorkplaces = 0;
        int extractorWorkplaces = 0;
        int industrialWorkplaces = 0;
        int officeWorkplaces = 0;
        int serviceEmployees = 0;
        int commercialEmployees = 0;
        int leisureEmployees = 0;
        int extractorEmployees = 0;
        int industrialEmployees = 0;
        int officeEmployees = 0;

        for (int i = 0; i < 5; i++)
        {
            WorkplaceCounter counter = levels[i];
            levelSummary[i] = new WorkplaceLevelSummary
            {
                Level = i,
                Total = counter.Total,
                Service = counter.Service,
                Commercial = counter.Commercial,
                Leisure = counter.Leisure,
                Extractor = counter.Extractor,
                Industrial = counter.Industrial,
                Office = counter.Office,
                Employees = counter.Employees,
                Open = counter.Open,
                Commuter = counter.Commuter
            };

            totalWorkplaces += counter.Total;
            filledWorkplaces += counter.Employees;
            openWorkplaces += counter.Open;
            commuterEmployees += counter.Commuter;
            serviceWorkplaces += counter.Service;
            commercialWorkplaces += counter.Commercial;
            leisureWorkplaces += counter.Leisure;
            extractorWorkplaces += counter.Extractor;
            industrialWorkplaces += counter.Industrial;
            officeWorkplaces += counter.Office;
            serviceEmployees += counter.ServiceEmployees;
            commercialEmployees += counter.CommercialEmployees;
            leisureEmployees += counter.LeisureEmployees;
            extractorEmployees += counter.ExtractorEmployees;
            industrialEmployees += counter.IndustrialEmployees;
            officeEmployees += counter.OfficeEmployees;
        }

        result = new WorkplacesScanResult(
            TotalWorkplaces: totalWorkplaces,
            FilledWorkplaces: filledWorkplaces,
            OpenWorkplaces: openWorkplaces,
            CommuterEmployees: commuterEmployees,
            WorkProvidersTotal: providersTotal,
            WorkProvidersService: providersService,
            WorkProvidersCommercial: providersCommercial,
            WorkProvidersLeisure: providersLeisure,
            WorkProvidersExtractor: providersExtractor,
            WorkProvidersIndustrial: providersIndustrial,
            WorkProvidersOffice: providersOffice,
            ServiceWorkplacesTotal: serviceWorkplaces,
            CommercialWorkplacesTotal: commercialWorkplaces,
            LeisureWorkplacesTotal: leisureWorkplaces,
            ExtractorWorkplacesTotal: extractorWorkplaces,
            IndustrialWorkplacesTotal: industrialWorkplaces,
            OfficeWorkplacesTotal: officeWorkplaces,
            ServiceEmployeesTotal: serviceEmployees,
            CommercialEmployeesTotal: commercialEmployees,
            LeisureEmployeesTotal: leisureEmployees,
            ExtractorEmployeesTotal: extractorEmployees,
            IndustrialEmployeesTotal: industrialEmployees,
            OfficeEmployeesTotal: officeEmployees,
            Levels: levelSummary,
            WasSampled: wasSampled);

        return true;
    }

    private bool TryScanHouseholdPressureContext(
        EntityManager entityManager,
        out int localHouseholds,
        out int movingAwayHouseholds,
        out int homelessHouseholds,
        out int propertyLinkedHouseholds,
        out bool wasSampled,
        out string? error)
    {
        error = null;
        localHouseholds = 0;
        movingAwayHouseholds = 0;
        homelessHouseholds = 0;
        propertyLinkedHouseholds = 0;
        wasSampled = false;

        try
        {
            var query = entityManager.CreateEntityQuery(
                new EntityQueryDesc
                {
                    All = new[]
                    {
                        ComponentType.ReadOnly<Household>()
                    },
                    None = new[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>()
                    }
                });

            NativeArray<Entity> entities = query.ToEntityArray(Allocator.TempJob);
            try
            {
                int sampleStride = ComputeSamplingStride(entities.Length, _sampling.MaxHouseholdEntities);
                wasSampled = sampleStride > 1;
                for (int i = 0; i < entities.Length; i += sampleStride)
                {
                    Entity householdEntity = entities[i];
                    Household household = entityManager.GetComponentData<Household>(householdEntity);
                    if ((household.m_Flags & HouseholdFlags.MovedIn) == 0)
                    {
                        continue;
                    }

                    localHouseholds++;

                    bool hasPropertyLink = entityManager.HasComponent<PropertyRenter>(householdEntity);
                    bool isHomeless = entityManager.HasComponent<HomelessHousehold>(householdEntity) || !hasPropertyLink;
                    if (isHomeless)
                    {
                        homelessHouseholds++;
                    }

                    if (hasPropertyLink)
                    {
                        propertyLinkedHouseholds++;
                    }

                    if (entityManager.HasComponent<MovingAway>(householdEntity))
                    {
                        movingAwayHouseholds++;
                    }
                }

                if (sampleStride > 1)
                {
                    localHouseholds = ScaleSampledCount(localHouseholds, sampleStride, entities.Length);
                    movingAwayHouseholds = ScaleSampledCount(movingAwayHouseholds, sampleStride, entities.Length);
                    homelessHouseholds = ScaleSampledCount(homelessHouseholds, sampleStride, entities.Length);
                    propertyLinkedHouseholds = ScaleSampledCount(propertyLinkedHouseholds, sampleStride, entities.Length);
                }
            }
            finally
            {
                entities.Dispose();
            }
        }
        catch (Exception ex)
        {
            error = ex.GetType().Name + ": " + ex.Message;
            return false;
        }

        return true;
    }

    private bool TryScanHouseholdEconomy(
        EntityManager entityManager,
        out HouseholdEconomyScanResult result,
        out string? error)
    {
        error = null;

        var resources = new List<int>(capacity: 4096);
        bool wasSampled = false;

        try
        {
            var query = entityManager.CreateEntityQuery(
                new EntityQueryDesc
                {
                    All = new[]
                    {
                        ComponentType.ReadOnly<Household>()
                    },
                    None = new[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>()
                    }
                });

            NativeArray<Entity> entities = query.ToEntityArray(Allocator.TempJob);
            try
            {
                int sampleStride = ComputeSamplingStride(entities.Length, _sampling.MaxHouseholdEntities);
                wasSampled = sampleStride > 1;
                for (int i = 0; i < entities.Length; i += sampleStride)
                {
                    Entity householdEntity = entities[i];
                    Household household = entityManager.GetComponentData<Household>(householdEntity);

                    if ((household.m_Flags & HouseholdFlags.MovedIn) == 0)
                    {
                        continue;
                    }

                    if ((household.m_Flags & HouseholdFlags.Tourist) != 0 ||
                        (household.m_Flags & HouseholdFlags.Commuter) != 0)
                    {
                        continue;
                    }

                    resources.Add(household.m_Resources);
                }
            }
            finally
            {
                entities.Dispose();
            }
        }
        catch (Exception ex)
        {
            result = default;
            error = ex.GetType().Name + ": " + ex.Message;
            return false;
        }

        if (resources.Count == 0)
        {
            result = default;
            error = "No local moved-in household resources were available.";
            return false;
        }

        resources.Sort();
        long sum = 0;
        for (int i = 0; i < resources.Count; i++)
        {
            sum += resources[i];
        }

        double average = sum / (double)resources.Count;
        double p25 = Percentile(resources, 0.25);
        double p50 = Percentile(resources, 0.50);
        double p75 = Percentile(resources, 0.75);

        result = new HouseholdEconomyScanResult(
            Average: Math.Round(average, 2, MidpointRounding.AwayFromZero),
            P25: Math.Round(p25, 2, MidpointRounding.AwayFromZero),
            P50: Math.Round(p50, 2, MidpointRounding.AwayFromZero),
            P75: Math.Round(p75, 2, MidpointRounding.AwayFromZero),
            WasSampled: wasSampled);
        return true;
    }

    private static void AccumulateWorkplaceLevel(
        WorkplaceCounter[] levels,
        int level,
        int workplaces,
        int employees,
        int commuters,
        bool isService,
        bool isCommercial,
        bool isLeisure,
        bool isExtractor,
        bool isOffice)
    {
        ref WorkplaceCounter counter = ref levels[level];
        counter.Total += workplaces;

        if (isService)
        {
            counter.Service += workplaces;
            counter.ServiceEmployees += employees;
        }
        else if (isCommercial)
        {
            if (isLeisure)
            {
                counter.Leisure += workplaces;
                counter.LeisureEmployees += employees;
            }
            else
            {
                counter.Commercial += workplaces;
                counter.CommercialEmployees += employees;
            }
        }
        else
        {
            if (isExtractor)
            {
                counter.Extractor += workplaces;
                counter.ExtractorEmployees += employees;
            }
            else if (isOffice)
            {
                counter.Office += workplaces;
                counter.OfficeEmployees += employees;
            }
            else
            {
                counter.Industrial += workplaces;
                counter.IndustrialEmployees += employees;
            }
        }

        counter.Employees += employees;
        counter.Open += workplaces - employees;
        counter.Commuter += commuters;
    }

    private static WorkforceLevelSummary[] CreateWorkforceLevels(
        int[] total,
        int[] workers,
        int[] unemployed,
        int[] homeless,
        int[] outside,
        int[] under)
    {
        int totalPotential = SumArray(total);
        var levels = new WorkforceLevelSummary[5];
        for (int i = 0; i < levels.Length; i++)
        {
            levels[i] = new WorkforceLevelSummary
            {
                Level = i,
                Total = total[i],
                TotalPercent = CalculatePercent(total[i], totalPotential),
                Workers = workers[i],
                Unemployed = unemployed[i],
                UnemployedPercent = CalculatePercent(unemployed[i], total[i]),
                Homeless = homeless[i],
                Outside = outside[i],
                Under = under[i],
                Employable = outside[i] + under[i]
            };
        }

        return levels;
    }

    private static SortedDictionary<string, MetricDefinition> CreateMobilityMetricMetadata()
    {
        return new SortedDictionary<string, MetricDefinition>(StringComparer.Ordinal)
        {
            ["traffic_volume_index"] = CreateMetricDefinition(
                MetricMeasurementKind.Derived,
                MetricTimeBasis.Instant,
                "index",
                "Game.Vehicles.Vehicle|Game.Vehicles.PublicTransport"),
            ["lines_total"] = CreateMetricDefinition(
                MetricMeasurementKind.Observed,
                MetricTimeBasis.Instant,
                "count",
                "Game.Routes.TransportLine|Game.Routes.CargoTransportLine"),
            ["passenger_lines_total"] = CreateMetricDefinition(
                MetricMeasurementKind.Observed,
                MetricTimeBasis.Instant,
                "count",
                "Game.Routes.TransportLine"),
            ["cargo_lines_total"] = CreateMetricDefinition(
                MetricMeasurementKind.Observed,
                MetricTimeBasis.Instant,
                "count",
                "Game.Routes.CargoTransportLine"),
            ["lines_by_transport_type"] = CreateMetricDefinition(
                MetricMeasurementKind.Observed,
                MetricTimeBasis.Instant,
                "count",
                "Game.UI.InGame.TransportUIUtils|Game.Routes.TransportLine|Game.Prefabs.PrefabRef|Game.Prefabs.TransportType"),
            ["active_vehicles_by_transport_type"] = CreateMetricDefinition(
                MetricMeasurementKind.Observed,
                MetricTimeBasis.Instant,
                "count",
                "Game.UI.InGame.TransportUIUtils|Game.Vehicles.PublicTransport|Game.Routes.TransportLine"),
            ["lines_with_service_count"] = CreateMetricDefinition(
                MetricMeasurementKind.Observed,
                MetricTimeBasis.Instant,
                "count",
                "Game.UI.InGame.TransportUIUtils|Game.Routes.TransportLine|Game.Vehicles.PublicTransport"),
            ["lines_without_service_count"] = CreateMetricDefinition(
                MetricMeasurementKind.Observed,
                MetricTimeBasis.Instant,
                "count",
                "Game.UI.InGame.TransportUIUtils|Game.Routes.TransportLine|Game.Vehicles.PublicTransport"),
            ["lines_with_service_percent"] = CreateMetricDefinition(
                MetricMeasurementKind.Derived,
                MetricTimeBasis.Instant,
                "percent",
                "Game.UI.InGame.TransportUIUtils|Game.Routes.TransportLine|Game.Vehicles.PublicTransport"),
            ["line_vehicle_entities_p50"] = CreateMetricDefinition(
                MetricMeasurementKind.Observed,
                MetricTimeBasis.Instant,
                "count",
                "Game.UI.InGame.TransportUIUtils|Game.Routes.TransportLine|Game.Vehicles.PublicTransport"),
            ["line_vehicle_entities_p95"] = CreateMetricDefinition(
                MetricMeasurementKind.Observed,
                MetricTimeBasis.Instant,
                "count",
                "Game.UI.InGame.TransportUIUtils|Game.Routes.TransportLine|Game.Vehicles.PublicTransport"),
            ["top_lines_by_active_vehicles"] = CreateMetricDefinition(
                MetricMeasurementKind.Observed,
                MetricTimeBasis.Instant,
                "count",
                "Game.UI.InGame.TransportUIUtils|Game.Routes.TransportLine|Game.Vehicles.PublicTransport"),
            ["lines"] = CreateMetricDefinition(
                MetricMeasurementKind.Observed,
                MetricTimeBasis.Instant,
                "line_records",
                "Game.UI.InGame.TransportUIUtils|Game.UI.NameSystem|Game.Routes.Color|Game.Routes.RouteNumber|BelzontTLM.XTMRouteExtraData")
        };
    }

    private static SortedDictionary<string, MetricDefinition> CreateEconomyMetricMetadata()
    {
        return new SortedDictionary<string, MetricDefinition>(StringComparer.Ordinal)
        {
            ["land_value_avg"] = CreateMetricDefinition(
                MetricMeasurementKind.Observed,
                MetricTimeBasis.Instant,
                "land_value_index",
                "Game.Buildings.LandValue"),
            ["land_value_p25"] = CreateMetricDefinition(
                MetricMeasurementKind.Observed,
                MetricTimeBasis.Instant,
                "land_value_index",
                "Game.Buildings.LandValue"),
            ["land_value_p50"] = CreateMetricDefinition(
                MetricMeasurementKind.Observed,
                MetricTimeBasis.Instant,
                "land_value_index",
                "Game.Buildings.LandValue"),
            ["land_value_p75"] = CreateMetricDefinition(
                MetricMeasurementKind.Observed,
                MetricTimeBasis.Instant,
                "land_value_index",
                "Game.Buildings.LandValue"),
            ["citizen_wealth_avg"] = CreateMetricDefinition(
                MetricMeasurementKind.Observed,
                MetricTimeBasis.Instant,
                "currency",
                "Game.Citizens.Household.m_Resources"),
            ["citizen_wealth_p25"] = CreateMetricDefinition(
                MetricMeasurementKind.Observed,
                MetricTimeBasis.Instant,
                "currency",
                "Game.Citizens.Household.m_Resources"),
            ["citizen_wealth_p50"] = CreateMetricDefinition(
                MetricMeasurementKind.Observed,
                MetricTimeBasis.Instant,
                "currency",
                "Game.Citizens.Household.m_Resources"),
            ["citizen_wealth_p75"] = CreateMetricDefinition(
                MetricMeasurementKind.Observed,
                MetricTimeBasis.Instant,
                "currency",
                "Game.Citizens.Household.m_Resources")
        };
    }

    private static SortedDictionary<string, MetricDefinition> CreateExternalConnectionsMetricMetadata()
    {
        return new SortedDictionary<string, MetricDefinition>(StringComparer.Ordinal)
        {
            ["imports_total_value"] = CreateMetricDefinition(
                MetricMeasurementKind.Observed,
                MetricTimeBasis.Monthly,
                "currency_per_month",
                "Game.Economy.TradeCost"),
            ["exports_total_value"] = CreateMetricDefinition(
                MetricMeasurementKind.Observed,
                MetricTimeBasis.Monthly,
                "currency_per_month",
                "Game.Economy.TradeCost"),
            ["imports_by_resource"] = CreateMetricDefinition(
                MetricMeasurementKind.Observed,
                MetricTimeBasis.Monthly,
                "currency_per_month",
                "Game.Economy.TradeCost"),
            ["exports_by_resource"] = CreateMetricDefinition(
                MetricMeasurementKind.Observed,
                MetricTimeBasis.Monthly,
                "currency_per_month",
                "Game.Economy.TradeCost"),
            ["service_trade"] = CreateMetricDefinition(
                MetricMeasurementKind.Observed,
                MetricTimeBasis.Monthly,
                "service_units_per_month",
                "Game.Net.OutsideConnection|Game.Objects.OutsideConnection")
        };
    }

    private static SortedDictionary<string, MetricDefinition> CreateLaborMarketMetricMetadata()
    {
        return new SortedDictionary<string, MetricDefinition>(StringComparer.Ordinal)
        {
            ["jobs_available_by_education_level"] = CreateMetricDefinition(
                MetricMeasurementKind.Observed,
                MetricTimeBasis.Instant,
                "jobs",
                "Game.Companies.WorkProvider|Game.Companies.Employee|Game.Prefabs.WorkplaceData"),
            ["jobs_filled_by_education_level"] = CreateMetricDefinition(
                MetricMeasurementKind.Observed,
                MetricTimeBasis.Instant,
                "jobs",
                "Game.Companies.WorkProvider|Game.Companies.Employee"),
            ["jobs_open_by_education_level"] = CreateMetricDefinition(
                MetricMeasurementKind.Derived,
                MetricTimeBasis.Instant,
                "jobs",
                "Game.Companies.WorkProvider|Game.Companies.Employee"),
            ["workforce_by_education_level"] = CreateMetricDefinition(
                MetricMeasurementKind.Observed,
                MetricTimeBasis.Instant,
                "workers",
                "Game.Citizens.Citizen|Game.Citizens.Worker|Game.Citizens.Student")
        };
    }

    private static MetricDefinition CreateMetricDefinition(string measurementKind, string timeBasis, string units, string sourceComponent)
    {
        return new MetricDefinition
        {
            MeasurementKind = measurementKind,
            TimeBasis = timeBasis,
            Units = units,
            SourceComponent = sourceComponent
        };
    }

    private static int ComputeSamplingStride(int totalCount, int maxSamples)
    {
        if (maxSamples <= 0 || totalCount <= maxSamples)
        {
            return 1;
        }

        return (int)Math.Ceiling(totalCount / (double)maxSamples);
    }

    private static int ScaleSampledCount(int sampledCount, int sampleStride, int maxCap)
    {
        if (sampleStride <= 1)
        {
            return sampledCount;
        }

        long scaled = (long)sampledCount * sampleStride;
        if (maxCap > 0 && scaled > maxCap)
        {
            scaled = maxCap;
        }

        return (int)Math.Max(0, scaled);
    }

    private static void ScaleSampledArray(int[] values, int sampleStride, int maxCap)
    {
        if (sampleStride <= 1)
        {
            return;
        }

        for (int i = 0; i < values.Length; i++)
        {
            values[i] = ScaleSampledCount(values[i], sampleStride, maxCap);
        }
    }

    private static void ScaleWorkplaceCounters(WorkplaceCounter[] counters, int sampleStride, int maxCap)
    {
        if (sampleStride <= 1)
        {
            return;
        }

        for (int i = 0; i < counters.Length; i++)
        {
            WorkplaceCounter counter = counters[i];
            counter.Total = ScaleSampledCount(counter.Total, sampleStride, maxCap);
            counter.Service = ScaleSampledCount(counter.Service, sampleStride, maxCap);
            counter.Commercial = ScaleSampledCount(counter.Commercial, sampleStride, maxCap);
            counter.Leisure = ScaleSampledCount(counter.Leisure, sampleStride, maxCap);
            counter.Extractor = ScaleSampledCount(counter.Extractor, sampleStride, maxCap);
            counter.Industrial = ScaleSampledCount(counter.Industrial, sampleStride, maxCap);
            counter.Office = ScaleSampledCount(counter.Office, sampleStride, maxCap);
            counter.ServiceEmployees = ScaleSampledCount(counter.ServiceEmployees, sampleStride, maxCap);
            counter.CommercialEmployees = ScaleSampledCount(counter.CommercialEmployees, sampleStride, maxCap);
            counter.LeisureEmployees = ScaleSampledCount(counter.LeisureEmployees, sampleStride, maxCap);
            counter.ExtractorEmployees = ScaleSampledCount(counter.ExtractorEmployees, sampleStride, maxCap);
            counter.IndustrialEmployees = ScaleSampledCount(counter.IndustrialEmployees, sampleStride, maxCap);
            counter.OfficeEmployees = ScaleSampledCount(counter.OfficeEmployees, sampleStride, maxCap);
            counter.Employees = ScaleSampledCount(counter.Employees, sampleStride, maxCap);
            counter.Open = ScaleSampledCount(counter.Open, sampleStride, maxCap);
            counter.Commuter = ScaleSampledCount(counter.Commuter, sampleStride, maxCap);
            counters[i] = counter;
        }
    }

    private static int? ToMonthlyProxy(int? activeEntityCount)
    {
        if (!activeEntityCount.HasValue)
        {
            return null;
        }

        long proxy = (long)activeEntityCount.Value * 30L;
        if (proxy > int.MaxValue)
        {
            proxy = int.MaxValue;
        }

        if (proxy < 0)
        {
            proxy = 0;
        }

        return (int)proxy;
    }

    private static SectorIntSummary CreateSectorIntSummary(
        int total,
        int service,
        int commercial,
        int leisure,
        int extractor,
        int industrial,
        int office)
    {
        return new SectorIntSummary
        {
            Total = total,
            Service = service,
            Commercial = commercial,
            Leisure = leisure,
            Extractor = extractor,
            Industrial = industrial,
            Office = office
        };
    }

    private static SectorDoubleSummary CreateSectorDoubleSummary(
        double? total,
        double? service,
        double? commercial,
        double? leisure,
        double? extractor,
        double? industrial,
        double? office)
    {
        return new SectorDoubleSummary
        {
            Total = total,
            Service = service,
            Commercial = commercial,
            Leisure = leisure,
            Extractor = extractor,
            Industrial = industrial,
            Office = office
        };
    }

    private static double? CalculatePercent(int numerator, int denominator)
    {
        if (denominator <= 0)
        {
            return null;
        }

        return Math.Round((numerator * 100.0) / denominator, 2, MidpointRounding.AwayFromZero);
    }

    private static double? CalculateRatio(int? numerator, int? denominator)
    {
        if (!numerator.HasValue || !denominator.HasValue || denominator.Value <= 0)
        {
            return null;
        }

        return Math.Round(numerator.Value / (double)denominator.Value, 2, MidpointRounding.AwayFromZero);
    }

    private static LevelCountSummary CreateLevelCountSummary(int level0, int level1, int level2, int level3, int level4)
    {
        return new LevelCountSummary
        {
            Level0 = level0,
            Level1 = level1,
            Level2 = level2,
            Level3 = level3,
            Level4 = level4,
            Total = level0 + level1 + level2 + level3 + level4
        };
    }

    private static double Percentile(List<int> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0)
        {
            return 0;
        }

        if (sortedValues.Count == 1)
        {
            return sortedValues[0];
        }

        double rank = percentile * (sortedValues.Count - 1);
        int lowerIndex = (int)Math.Floor(rank);
        int upperIndex = (int)Math.Ceiling(rank);
        double weight = rank - lowerIndex;

        if (upperIndex <= lowerIndex)
        {
            return sortedValues[lowerIndex];
        }

        return sortedValues[lowerIndex] +
               ((sortedValues[upperIndex] - sortedValues[lowerIndex]) * weight);
    }

    private static double Percentile(List<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0)
        {
            return 0;
        }

        if (sortedValues.Count == 1)
        {
            return sortedValues[0];
        }

        sortedValues.Sort();

        double rank = percentile * (sortedValues.Count - 1);
        int lowerIndex = (int)Math.Floor(rank);
        int upperIndex = (int)Math.Ceiling(rank);
        double weight = rank - lowerIndex;

        if (upperIndex <= lowerIndex)
        {
            return sortedValues[lowerIndex];
        }

        return sortedValues[lowerIndex] +
               ((sortedValues[upperIndex] - sortedValues[lowerIndex]) * weight);
    }

    private static double Average(List<double> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        double total = 0;
        for (int i = 0; i < values.Count; i++)
        {
            total += values[i];
        }

        return total / values.Count;
    }

    private static bool IsWorkingAge(CitizenAge age)
    {
        return age != CitizenAge.Child && age != CitizenAge.Elderly;
    }

    private static int ClampEducationLevel(int level)
    {
        if (level < 0)
        {
            return 0;
        }

        if (level > 4)
        {
            return 4;
        }

        return level;
    }

    private static int SumArray(int[] values)
    {
        int total = 0;
        for (int i = 0; i < values.Length; i++)
        {
            total += values[i];
        }

        return total;
    }

    private static OfficialCitySingletonValues ReadOfficialCitySingletonValues(
        EntityManager entityManager,
        List<string> notes)
    {
        EntityQuery query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<City>(),
            ComponentType.ReadOnly<Population>(),
            ComponentType.ReadOnly<Tourism>(),
            ComponentType.ReadOnly<DevTreePoints>());

        try
        {
            if (query.IsEmptyIgnoreFilter)
            {
                notes.Add("city singleton query returned no entity for Population, Tourism, and DevTreePoints.");
                return new OfficialCitySingletonValues(null, null, null, null, null);
            }

            Entity cityEntity = query.GetSingletonEntity();
            var population = entityManager.GetComponentData<Population>(cityEntity);
            var tourism = entityManager.GetComponentData<Tourism>(cityEntity);
            var devTreePoints = entityManager.GetComponentData<DevTreePoints>(cityEntity);

            return new OfficialCitySingletonValues(
                PopulationWithMoveIn: population.m_PopulationWithMoveIn,
                CurrentTourists: tourism.m_CurrentTourists,
                AverageTourists: tourism.m_AverageTourists,
                Attractiveness: tourism.m_Attractiveness,
                DevTreePoints: devTreePoints.m_Points);
        }
        catch (Exception ex)
        {
            notes.Add("city singleton values unavailable: " + ex.Message);
            return new OfficialCitySingletonValues(null, null, null, null, null);
        }
        finally
        {
            query.Dispose();
        }
    }

    private static int? GetOfficialStatistic(CityStatisticsSystem statistics, StatisticType type)
    {
        try
        {
            return statistics.GetStatisticValue(type);
        }
        catch
        {
            return null;
        }
    }

    private static int CountPresent(int? value)
    {
        return value.HasValue ? 1 : 0;
    }

    private static int CountPresent(double? value)
    {
        return value.HasValue ? 1 : 0;
    }

    private static int CountPresent(object? value)
    {
        return value != null ? 1 : 0;
    }

    private static string ComputeStatus(int availableMetrics, int expectedMetrics)
    {
        if (availableMetrics <= 0)
        {
            return MetricStatus.Unavailable;
        }

        return availableMetrics >= expectedMetrics
            ? MetricStatus.Ok
            : MetricStatus.Partial;
    }

    private static string BuildSourceComponent(string prefix, params CountResult[] results)
    {
        var componentNames = new List<string>();
        foreach (CountResult result in results)
        {
            foreach (string componentName in result.ResolvedComponentTypeNames)
            {
                if (!componentNames.Contains(componentName))
                {
                    componentNames.Add(componentName);
                }
            }
        }

        if (componentNames.Count == 0)
        {
            return prefix;
        }

        return prefix + ":" + string.Join("|", componentNames);
    }

    private static void AddResultNotes(List<string> notes, string fieldName, CountResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.Error))
        {
            notes.Add(fieldName + " query error: " + result.Error);
            return;
        }

        if (!result.Count.HasValue)
        {
            notes.Add(fieldName + " unavailable: no matching ECS component type found.");
            return;
        }

        string componentList = result.ResolvedComponentTypeNames.Length == 0
            ? "unknown component"
            : string.Join(", ", result.ResolvedComponentTypeNames);
        notes.Add(
            fieldName +
            "=" +
            result.Count.Value.ToString(CultureInfo.InvariantCulture) +
            " from " +
            componentList +
            ".");
    }

    private static CountResult TryCountByAll(EntityManager entityManager, IReadOnlyList<string> candidateTypeNames)
    {
        string? lastError = null;
        string[]? lastResolvedType = null;

        foreach (string candidateTypeName in candidateTypeNames)
        {
            Type? componentType = ResolveComponentType(candidateTypeName);
            if (componentType == null)
            {
                continue;
            }

            if (TryQueryCount(entityManager, allComponentTypes: new[] { componentType }, anyComponentTypes: null, out int count, out string? error))
            {
                return new CountResult(count, new[] { componentType.FullName ?? componentType.Name }, null);
            }

            lastError = error;
            lastResolvedType = new[] { componentType.FullName ?? componentType.Name };
        }

        if (!string.IsNullOrWhiteSpace(lastError) && lastResolvedType != null)
        {
            return new CountResult(null, lastResolvedType, lastError);
        }

        return CountResult.Empty;
    }

    private static CountResult TryCountByAny(EntityManager entityManager, IReadOnlyList<string> candidateTypeNames)
    {
        var resolvedTypes = new List<Type>();
        var resolvedNames = new List<string>();

        foreach (string candidateTypeName in candidateTypeNames)
        {
            Type? componentType = ResolveComponentType(candidateTypeName);
            if (componentType == null)
            {
                continue;
            }

            resolvedTypes.Add(componentType);
            resolvedNames.Add(componentType.FullName ?? componentType.Name);
        }

        if (resolvedTypes.Count == 0)
        {
            return CountResult.Empty;
        }

        if (TryQueryCount(entityManager, allComponentTypes: null, anyComponentTypes: resolvedTypes.ToArray(), out int count, out string? error))
        {
            return new CountResult(count, resolvedNames.ToArray(), null);
        }

        return new CountResult(null, resolvedNames.ToArray(), error);
    }

    private static bool TryQueryCount(
        EntityManager entityManager,
        Type[]? allComponentTypes,
        Type[]? anyComponentTypes,
        out int count,
        out string? error)
    {
        count = 0;
        error = null;

        try
        {
            ComponentType[]? all = ToComponentTypes(allComponentTypes);
            ComponentType[]? any = ToComponentTypes(anyComponentTypes);
            ComponentType[]? required = all ?? any;
            if (required == null || required.Length == 0)
            {
                error = "No component types were provided for query.";
                return false;
            }

            EntityQuery query = entityManager.CreateEntityQuery(required);
            count = query.CalculateEntityCount();
            return true;
        }
        catch (Exception ex)
        {
            error = ex.GetType().Name + ": " + ex.Message;
            return false;
        }
    }

    private static ComponentType[]? ToComponentTypes(Type[]? componentTypes)
    {
        if (componentTypes == null || componentTypes.Length == 0)
        {
            return null;
        }

        var results = new ComponentType[componentTypes.Length];
        for (int i = 0; i < componentTypes.Length; i++)
        {
            results[i] = ComponentType.ReadOnly(componentTypes[i]);
        }

        return results;
    }

    private static Type? ResolveComponentType(string typeName)
    {
        lock (s_typeCacheLock)
        {
            if (s_componentTypeCache.TryGetValue(typeName, out Type? cached))
            {
                return cached;
            }
        }

        Type? resolved = ResolveByExactTypeName(typeName);
        if (resolved == null)
        {
            resolved = ResolveByFallbackSuffix(typeName);
        }

        if (resolved != null && !IsEcsComponentType(resolved))
        {
            resolved = null;
        }

        lock (s_typeCacheLock)
        {
            s_componentTypeCache[typeName] = resolved;
        }

        return resolved;
    }

    private static Type? ResolveByExactTypeName(string typeName)
    {
        Type? direct = Type.GetType(typeName, throwOnError: false);
        if (direct != null)
        {
            return direct;
        }

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type? found = assembly.GetType(typeName, throwOnError: false, ignoreCase: false);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Type? ResolveByFallbackSuffix(string typeName)
    {
        int lastDot = typeName.LastIndexOf('.');
        if (lastDot < 0 || lastDot >= typeName.Length - 1)
        {
            return null;
        }

        string expectedNamespace = typeName.Substring(0, lastDot);
        string expectedName = typeName.Substring(lastDot + 1);

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type?[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types ?? Array.Empty<Type?>();
            }
            catch
            {
                continue;
            }

            foreach (Type? type in types)
            {
                if (type == null)
                {
                    continue;
                }

                if (!string.Equals(type.Name, expectedName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (type.Namespace == null || !type.Namespace.StartsWith(expectedNamespace, StringComparison.Ordinal))
                {
                    continue;
                }

                if (IsEcsComponentType(type))
                {
                    return type;
                }
            }
        }

        return null;
    }

    private static bool IsEcsComponentType(Type type)
    {
        return typeof(IComponentData).IsAssignableFrom(type) ||
               typeof(IBufferElementData).IsAssignableFrom(type);
    }

    private sealed class MutableTransportLineUsage
    {
        public Entity LineEntity { get; init; }

        public string Mode { get; set; } = kUnknownTransportMode;

        public string? LineName { get; set; }

        public int ActiveVehicleEntities { get; set; }

        public int? OnboardPassengerEntities { get; set; }

        public int? TotalPassengerCapacity { get; set; }

        public int MissingCapacityVehicleCount { get; set; }

        public double? UsagePercent { get; set; }

        public double? UsagePercentProxy { get; set; }
    }

    private sealed class LineNameResolutionState
    {
        private readonly Dictionary<string, int> _lineCandidatePresenceCounts = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _prefabCandidatePresenceCounts = new(StringComparer.Ordinal);
        private readonly HashSet<string> _resolvedComponentTypes = new(StringComparer.Ordinal);

        public int TotalLineCount { get; set; }

        public int ResolvedLineNameCount { get; set; }

        public int ResolvedFromLineComponentCount { get; set; }

        public int ResolvedFromLineNameComponentCount { get; set; }

        public int ResolvedFromPrefabNameComponentCount { get; set; }

        public int ResolvedFromLineComponentScanCount { get; set; }

        public int ResolvedFromPrefabComponentScanCount { get; set; }

        public void RecordResolvedComponentType(Type? type)
        {
            if (type == null)
            {
                return;
            }

            string name = type.FullName ?? type.Name;
            if (!string.IsNullOrWhiteSpace(name))
            {
                _resolvedComponentTypes.Add(name);
            }
        }

        public void RecordCandidatePresence(Type type, bool onPrefab)
        {
            string name = type.FullName ?? type.Name;
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            Dictionary<string, int> counts = onPrefab ? _prefabCandidatePresenceCounts : _lineCandidatePresenceCounts;
            if (counts.TryGetValue(name, out int current))
            {
                counts[name] = current + 1;
            }
            else
            {
                counts[name] = 1;
            }
        }

        public string[] BuildLineCandidatePresenceSummary()
        {
            return BuildPresenceSummary(_lineCandidatePresenceCounts);
        }

        public string[] BuildPrefabCandidatePresenceSummary()
        {
            return BuildPresenceSummary(_prefabCandidatePresenceCounts);
        }

        public string[] BuildResolvedComponentTypes()
        {
            if (_resolvedComponentTypes.Count == 0)
            {
                return Array.Empty<string>();
            }

            var values = new List<string>(_resolvedComponentTypes);
            values.Sort(StringComparer.Ordinal);
            return values.ToArray();
        }

        private static string[] BuildPresenceSummary(Dictionary<string, int> counts)
        {
            if (counts.Count == 0)
            {
                return Array.Empty<string>();
            }

            var entries = new List<KeyValuePair<string, int>>(counts);
            entries.Sort(
                (left, right) =>
                {
                    int countCompare = right.Value.CompareTo(left.Value);
                    if (countCompare != 0)
                    {
                        return countCompare;
                    }

                    return string.CompareOrdinal(left.Key, right.Key);
                });

            var summary = new string[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                summary[i] = entries[i].Key + "=" + entries[i].Value.ToString(CultureInfo.InvariantCulture);
            }

            return summary;
        }
    }

    private readonly struct ObservedMobilityLineData
    {
        public ObservedMobilityLineData(
            MobilityLineRecord[] Lines,
            MobilityLineRecord[] TopLinesByActiveVehicles,
            int LinesTotal,
            int PassengerLinesTotal,
            int CargoLinesTotal,
            ModeEntityCounts LinesByTransportType,
            ModeEntityCounts ActiveVehiclesByTransportType,
            int LinesWithServiceCount,
            int LinesWithoutServiceCount,
            double? LinesWithServicePercent,
            double? LineVehicleEntitiesP50,
            double? LineVehicleEntitiesP95,
            bool UsedXtmAcronym)
        {
            this.Lines = Lines;
            this.TopLinesByActiveVehicles = TopLinesByActiveVehicles;
            this.LinesTotal = LinesTotal;
            this.PassengerLinesTotal = PassengerLinesTotal;
            this.CargoLinesTotal = CargoLinesTotal;
            this.LinesByTransportType = LinesByTransportType;
            this.ActiveVehiclesByTransportType = ActiveVehiclesByTransportType;
            this.LinesWithServiceCount = LinesWithServiceCount;
            this.LinesWithoutServiceCount = LinesWithoutServiceCount;
            this.LinesWithServicePercent = LinesWithServicePercent;
            this.LineVehicleEntitiesP50 = LineVehicleEntitiesP50;
            this.LineVehicleEntitiesP95 = LineVehicleEntitiesP95;
            this.UsedXtmAcronym = UsedXtmAcronym;
        }

        public MobilityLineRecord[] Lines { get; }

        public MobilityLineRecord[] TopLinesByActiveVehicles { get; }

        public int LinesTotal { get; }

        public int PassengerLinesTotal { get; }

        public int CargoLinesTotal { get; }

        public ModeEntityCounts LinesByTransportType { get; }

        public ModeEntityCounts ActiveVehiclesByTransportType { get; }

        public int LinesWithServiceCount { get; }

        public int LinesWithoutServiceCount { get; }

        public double? LinesWithServicePercent { get; }

        public double? LineVehicleEntitiesP50 { get; }

        public double? LineVehicleEntitiesP95 { get; }

        public bool UsedXtmAcronym { get; }
    }

    private readonly struct TransportLineUsageScanResult
    {
        public TransportLineUsageScanResult(
            LineUsageByTransportType LineUsageByTransportType,
            ModeEntityCounts LinesByTransportType,
            ModeEntityCounts ActiveVehiclesByTransportType,
            ModeEntityCounts OnboardPassengersByTransportType,
            double? LineUsageAvgPercent,
            double? LineUsageP95Percent,
            int LinesWithServiceCount,
            int LinesWithoutServiceCount,
            double? LinesWithServicePercent,
            int MappedPublicTransportVehicles,
            int UnmappedPublicTransportVehicles,
            int? MappedPublicTransportVehiclesWithPassengers,
            double? MappedPublicTransportVehiclesWithPassengersPercent,
            double? LineVehicleEntitiesP50,
            double? LineVehicleEntitiesP95,
            double? LineOnboardPassengersP50,
            double? LineOnboardPassengersP95,
            int? VehiclesWithPassengersCount,
            double? VehiclesWithPassengersPercent,
            ModeEntityCounts VehiclesWithPassengersByTransportType,
            ModeDoubleValues VehiclesWithPassengersPercentByTransportType,
            double? AvgOnboardPassengersPerActiveVehicle,
            ModeDoubleValues AvgOnboardPassengersPerActiveVehicleByTransportType,
            TransportLineTopSummary[] TopLinesByUsageProxy,
            TransportLineTopSummary[] TopLinesByOnboardPassengers,
            TransportLineTopSummary[] TopLinesByActiveVehicles,
            int LineNameTotalCount,
            int LineNameResolvedCount,
            int LineNameResolvedFromLineComponentCount,
            int LineNameResolvedFromLineNameComponentCount,
            int LineNameResolvedFromPrefabNameComponentCount,
            int LineNameResolvedFromLineComponentScanCount,
            int LineNameResolvedFromPrefabComponentScanCount,
            string[] LineNameResolvedComponentTypes,
            string[] LineNameLineCandidatePresenceSummary,
            string[] LineNamePrefabCandidatePresenceSummary,
            bool UsedFallbackTransportTypeClassification,
            bool PassengerBufferUnavailable,
            bool VehicleLineMappingUnavailable,
            bool UsedCurrentVehicleOccupancyFallback,
            bool CurrentVehicleOccupancyUnavailable)
        {
            this.LineUsageByTransportType = LineUsageByTransportType;
            this.LinesByTransportType = LinesByTransportType;
            this.ActiveVehiclesByTransportType = ActiveVehiclesByTransportType;
            this.OnboardPassengersByTransportType = OnboardPassengersByTransportType;
            this.LineUsageAvgPercent = LineUsageAvgPercent;
            this.LineUsageP95Percent = LineUsageP95Percent;
            this.LinesWithServiceCount = LinesWithServiceCount;
            this.LinesWithoutServiceCount = LinesWithoutServiceCount;
            this.LinesWithServicePercent = LinesWithServicePercent;
            this.MappedPublicTransportVehicles = MappedPublicTransportVehicles;
            this.UnmappedPublicTransportVehicles = UnmappedPublicTransportVehicles;
            this.MappedPublicTransportVehiclesWithPassengers = MappedPublicTransportVehiclesWithPassengers;
            this.MappedPublicTransportVehiclesWithPassengersPercent = MappedPublicTransportVehiclesWithPassengersPercent;
            this.LineVehicleEntitiesP50 = LineVehicleEntitiesP50;
            this.LineVehicleEntitiesP95 = LineVehicleEntitiesP95;
            this.LineOnboardPassengersP50 = LineOnboardPassengersP50;
            this.LineOnboardPassengersP95 = LineOnboardPassengersP95;
            this.VehiclesWithPassengersCount = VehiclesWithPassengersCount;
            this.VehiclesWithPassengersPercent = VehiclesWithPassengersPercent;
            this.VehiclesWithPassengersByTransportType = VehiclesWithPassengersByTransportType;
            this.VehiclesWithPassengersPercentByTransportType = VehiclesWithPassengersPercentByTransportType;
            this.AvgOnboardPassengersPerActiveVehicle = AvgOnboardPassengersPerActiveVehicle;
            this.AvgOnboardPassengersPerActiveVehicleByTransportType = AvgOnboardPassengersPerActiveVehicleByTransportType;
            this.TopLinesByUsageProxy = TopLinesByUsageProxy;
            this.TopLinesByOnboardPassengers = TopLinesByOnboardPassengers;
            this.TopLinesByActiveVehicles = TopLinesByActiveVehicles;
            this.LineNameTotalCount = LineNameTotalCount;
            this.LineNameResolvedCount = LineNameResolvedCount;
            this.LineNameResolvedFromLineComponentCount = LineNameResolvedFromLineComponentCount;
            this.LineNameResolvedFromLineNameComponentCount = LineNameResolvedFromLineNameComponentCount;
            this.LineNameResolvedFromPrefabNameComponentCount = LineNameResolvedFromPrefabNameComponentCount;
            this.LineNameResolvedFromLineComponentScanCount = LineNameResolvedFromLineComponentScanCount;
            this.LineNameResolvedFromPrefabComponentScanCount = LineNameResolvedFromPrefabComponentScanCount;
            this.LineNameResolvedComponentTypes = LineNameResolvedComponentTypes;
            this.LineNameLineCandidatePresenceSummary = LineNameLineCandidatePresenceSummary;
            this.LineNamePrefabCandidatePresenceSummary = LineNamePrefabCandidatePresenceSummary;
            this.UsedFallbackTransportTypeClassification = UsedFallbackTransportTypeClassification;
            this.PassengerBufferUnavailable = PassengerBufferUnavailable;
            this.VehicleLineMappingUnavailable = VehicleLineMappingUnavailable;
            this.UsedCurrentVehicleOccupancyFallback = UsedCurrentVehicleOccupancyFallback;
            this.CurrentVehicleOccupancyUnavailable = CurrentVehicleOccupancyUnavailable;
        }

        public LineUsageByTransportType LineUsageByTransportType { get; }

        public ModeEntityCounts LinesByTransportType { get; }

        public ModeEntityCounts ActiveVehiclesByTransportType { get; }

        public ModeEntityCounts OnboardPassengersByTransportType { get; }

        public double? LineUsageAvgPercent { get; }

        public double? LineUsageP95Percent { get; }

        public int LinesWithServiceCount { get; }

        public int LinesWithoutServiceCount { get; }

        public double? LinesWithServicePercent { get; }

        public int MappedPublicTransportVehicles { get; }

        public int UnmappedPublicTransportVehicles { get; }

        public int? MappedPublicTransportVehiclesWithPassengers { get; }

        public double? MappedPublicTransportVehiclesWithPassengersPercent { get; }

        public double? LineVehicleEntitiesP50 { get; }

        public double? LineVehicleEntitiesP95 { get; }

        public double? LineOnboardPassengersP50 { get; }

        public double? LineOnboardPassengersP95 { get; }

        public int? VehiclesWithPassengersCount { get; }

        public double? VehiclesWithPassengersPercent { get; }

        public ModeEntityCounts VehiclesWithPassengersByTransportType { get; }

        public ModeDoubleValues VehiclesWithPassengersPercentByTransportType { get; }

        public double? AvgOnboardPassengersPerActiveVehicle { get; }

        public ModeDoubleValues AvgOnboardPassengersPerActiveVehicleByTransportType { get; }

        public TransportLineTopSummary[] TopLinesByUsageProxy { get; }

        public TransportLineTopSummary[] TopLinesByOnboardPassengers { get; }

        public TransportLineTopSummary[] TopLinesByActiveVehicles { get; }

        public int LineNameTotalCount { get; }

        public int LineNameResolvedCount { get; }

        public int LineNameResolvedFromLineComponentCount { get; }

        public int LineNameResolvedFromLineNameComponentCount { get; }

        public int LineNameResolvedFromPrefabNameComponentCount { get; }

        public int LineNameResolvedFromLineComponentScanCount { get; }

        public int LineNameResolvedFromPrefabComponentScanCount { get; }

        public string[] LineNameResolvedComponentTypes { get; }

        public string[] LineNameLineCandidatePresenceSummary { get; }

        public string[] LineNamePrefabCandidatePresenceSummary { get; }

        public bool UsedFallbackTransportTypeClassification { get; }

        public bool PassengerBufferUnavailable { get; }

        public bool VehicleLineMappingUnavailable { get; }

        public bool UsedCurrentVehicleOccupancyFallback { get; }

        public bool CurrentVehicleOccupancyUnavailable { get; }
    }

    private readonly struct CountResult
    {
        public static CountResult Empty => new(null, Array.Empty<string>(), null);

        public CountResult(int? count, string[] resolvedComponentTypeNames, string? error)
        {
            Count = count;
            ResolvedComponentTypeNames = resolvedComponentTypeNames;
            Error = error;
        }

        public int? Count { get; }

        public string[] ResolvedComponentTypeNames { get; }

        public string? Error { get; }
    }

    private readonly struct PopulationWorkforceScanResult
    {
        public PopulationWorkforceScanResult(
            int LocalPopulation,
            int TouristPopulation,
            int CommuterPopulation,
            int MovingAwayPopulation,
            int HomelessPopulation,
            int LocalHouseholds,
            int MovingAwayHouseholds,
            int HomelessHouseholds,
            int PropertyLinkedHouseholds,
            int WorkingAgePopulation,
            int ChildrenPopulation,
            int ElderlyPopulation,
            int[] LocalByEducationLevel,
            WorkforceLevelSummary[] WorkforceLevels,
            int TotalPotentialWorkers,
            int TotalWorkers,
            int TotalUnemployed,
            int TotalHomelessUnemployed,
            int TotalOutsideWorkers,
            int TotalUnderemployedWorkers,
            int TotalEmployable,
            bool WasSampled)
        {
            this.LocalPopulation = LocalPopulation;
            this.TouristPopulation = TouristPopulation;
            this.CommuterPopulation = CommuterPopulation;
            this.MovingAwayPopulation = MovingAwayPopulation;
            this.HomelessPopulation = HomelessPopulation;
            this.LocalHouseholds = LocalHouseholds;
            this.MovingAwayHouseholds = MovingAwayHouseholds;
            this.HomelessHouseholds = HomelessHouseholds;
            this.PropertyLinkedHouseholds = PropertyLinkedHouseholds;
            this.WorkingAgePopulation = WorkingAgePopulation;
            this.ChildrenPopulation = ChildrenPopulation;
            this.ElderlyPopulation = ElderlyPopulation;
            this.LocalByEducationLevel = LocalByEducationLevel;
            this.WorkforceLevels = WorkforceLevels;
            this.TotalPotentialWorkers = TotalPotentialWorkers;
            this.TotalWorkers = TotalWorkers;
            this.TotalUnemployed = TotalUnemployed;
            this.TotalHomelessUnemployed = TotalHomelessUnemployed;
            this.TotalOutsideWorkers = TotalOutsideWorkers;
            this.TotalUnderemployedWorkers = TotalUnderemployedWorkers;
            this.TotalEmployable = TotalEmployable;
            this.WasSampled = WasSampled;
        }

        public int LocalPopulation { get; }
        public int TouristPopulation { get; }
        public int CommuterPopulation { get; }
        public int MovingAwayPopulation { get; }
        public int HomelessPopulation { get; }
        public int LocalHouseholds { get; }
        public int MovingAwayHouseholds { get; }
        public int HomelessHouseholds { get; }
        public int PropertyLinkedHouseholds { get; }
        public int WorkingAgePopulation { get; }
        public int ChildrenPopulation { get; }
        public int ElderlyPopulation { get; }
        public int[] LocalByEducationLevel { get; }
        public WorkforceLevelSummary[] WorkforceLevels { get; }
        public int TotalPotentialWorkers { get; }
        public int TotalWorkers { get; }
        public int TotalUnemployed { get; }
        public int TotalHomelessUnemployed { get; }
        public int TotalOutsideWorkers { get; }
        public int TotalUnderemployedWorkers { get; }
        public int TotalEmployable { get; }
        public bool WasSampled { get; }
    }

    private readonly struct WorkplacesScanResult
    {
        public WorkplacesScanResult(
            int TotalWorkplaces,
            int FilledWorkplaces,
            int OpenWorkplaces,
            int CommuterEmployees,
            int WorkProvidersTotal,
            int WorkProvidersService,
            int WorkProvidersCommercial,
            int WorkProvidersLeisure,
            int WorkProvidersExtractor,
            int WorkProvidersIndustrial,
            int WorkProvidersOffice,
            int ServiceWorkplacesTotal,
            int CommercialWorkplacesTotal,
            int LeisureWorkplacesTotal,
            int ExtractorWorkplacesTotal,
            int IndustrialWorkplacesTotal,
            int OfficeWorkplacesTotal,
            int ServiceEmployeesTotal,
            int CommercialEmployeesTotal,
            int LeisureEmployeesTotal,
            int ExtractorEmployeesTotal,
            int IndustrialEmployeesTotal,
            int OfficeEmployeesTotal,
            WorkplaceLevelSummary[] Levels,
            bool WasSampled)
        {
            this.TotalWorkplaces = TotalWorkplaces;
            this.FilledWorkplaces = FilledWorkplaces;
            this.OpenWorkplaces = OpenWorkplaces;
            this.CommuterEmployees = CommuterEmployees;
            this.WorkProvidersTotal = WorkProvidersTotal;
            this.WorkProvidersService = WorkProvidersService;
            this.WorkProvidersCommercial = WorkProvidersCommercial;
            this.WorkProvidersLeisure = WorkProvidersLeisure;
            this.WorkProvidersExtractor = WorkProvidersExtractor;
            this.WorkProvidersIndustrial = WorkProvidersIndustrial;
            this.WorkProvidersOffice = WorkProvidersOffice;
            this.ServiceWorkplacesTotal = ServiceWorkplacesTotal;
            this.CommercialWorkplacesTotal = CommercialWorkplacesTotal;
            this.LeisureWorkplacesTotal = LeisureWorkplacesTotal;
            this.ExtractorWorkplacesTotal = ExtractorWorkplacesTotal;
            this.IndustrialWorkplacesTotal = IndustrialWorkplacesTotal;
            this.OfficeWorkplacesTotal = OfficeWorkplacesTotal;
            this.ServiceEmployeesTotal = ServiceEmployeesTotal;
            this.CommercialEmployeesTotal = CommercialEmployeesTotal;
            this.LeisureEmployeesTotal = LeisureEmployeesTotal;
            this.ExtractorEmployeesTotal = ExtractorEmployeesTotal;
            this.IndustrialEmployeesTotal = IndustrialEmployeesTotal;
            this.OfficeEmployeesTotal = OfficeEmployeesTotal;
            this.Levels = Levels;
            this.WasSampled = WasSampled;
        }

        public int TotalWorkplaces { get; }
        public int FilledWorkplaces { get; }
        public int OpenWorkplaces { get; }
        public int CommuterEmployees { get; }
        public int WorkProvidersTotal { get; }
        public int WorkProvidersService { get; }
        public int WorkProvidersCommercial { get; }
        public int WorkProvidersLeisure { get; }
        public int WorkProvidersExtractor { get; }
        public int WorkProvidersIndustrial { get; }
        public int WorkProvidersOffice { get; }
        public int ServiceWorkplacesTotal { get; }
        public int CommercialWorkplacesTotal { get; }
        public int LeisureWorkplacesTotal { get; }
        public int ExtractorWorkplacesTotal { get; }
        public int IndustrialWorkplacesTotal { get; }
        public int OfficeWorkplacesTotal { get; }
        public int ServiceEmployeesTotal { get; }
        public int CommercialEmployeesTotal { get; }
        public int LeisureEmployeesTotal { get; }
        public int ExtractorEmployeesTotal { get; }
        public int IndustrialEmployeesTotal { get; }
        public int OfficeEmployeesTotal { get; }
        public WorkplaceLevelSummary[] Levels { get; }
        public bool WasSampled { get; }
    }

    private readonly struct HouseholdEconomyScanResult
    {
        public HouseholdEconomyScanResult(double Average, double P25, double P50, double P75, bool WasSampled)
        {
            this.Average = Average;
            this.P25 = P25;
            this.P50 = P50;
            this.P75 = P75;
            this.WasSampled = WasSampled;
        }

        public double Average { get; }
        public double P25 { get; }
        public double P50 { get; }
        public double P75 { get; }
        public bool WasSampled { get; }
    }

    private struct WorkplaceCounter
    {
        public int Total;
        public int Service;
        public int Commercial;
        public int Leisure;
        public int Extractor;
        public int Industrial;
        public int Office;
        public int ServiceEmployees;
        public int CommercialEmployees;
        public int LeisureEmployees;
        public int ExtractorEmployees;
        public int IndustrialEmployees;
        public int OfficeEmployees;
        public int Employees;
        public int Open;
        public int Commuter;
    }
}
