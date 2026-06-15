using System;
using System.Collections.Generic;
using System.Linq;

namespace CS2DataExport;

public sealed class TransitLineDetailCalculationInput
{
    public IReadOnlyList<TransitLineStopLoad> Stops { get; init; } = Array.Empty<TransitLineStopLoad>();
    public IReadOnlyList<TransitLineVehicleLoad> Vehicles { get; init; } = Array.Empty<TransitLineVehicleLoad>();
    public IReadOnlyList<double> SegmentDurations { get; init; } = Array.Empty<double>();
    public double? TicksPerDay { get; init; }
}

public readonly record struct TransitLineStopLoad(int WaitingPassengers);

public readonly record struct TransitLineVehicleLoad(
    int EntityIndex,
    int EntityVersion,
    int PassengerCount,
    int Capacity,
    double? OdometerMeters,
    double? MaintenanceRangeMeters);

public sealed class TransitLineDetailCalculationResult
{
    public int WaitingPassengersAllStops { get; init; }
    public int MaxWaitingPassengersAtStop { get; init; }
    public int OnboardPassengersInVehicles { get; init; }
    public int TotalPassengerCapacity { get; init; }
    public int StopCapacity { get; init; }
    public double? AverageVehicleOccupancyPercent { get; init; }
    public double? AverageStopOccupancyPercent { get; init; }
    public double? ExpectedRoundTripTimeTicks { get; init; }
    public double? ExpectedRoundTripTimeMinutes { get; init; }
    public int? NextMaintenanceVehicleEntityIndex { get; init; }
    public int? NextMaintenanceVehicleEntityVersion { get; init; }
    public double? NextMaintenanceDistanceMeters { get; init; }
}

public static class TransitLineDetailCalculator
{
    public static TransitLineDetailCalculationResult Calculate(TransitLineDetailCalculationInput input)
    {
        int waiting = input.Stops.Sum(static stop => Math.Max(0, stop.WaitingPassengers));
        int maxWaiting = input.Stops.Count == 0
            ? 0
            : input.Stops.Max(static stop => Math.Max(0, stop.WaitingPassengers));

        int onboard = input.Vehicles.Sum(static vehicle => Math.Max(0, vehicle.PassengerCount));
        int totalCapacity = input.Vehicles.Sum(static vehicle => Math.Max(0, vehicle.Capacity));
        int stopCapacity = input.Vehicles.Count == 0
            ? 0
            : input.Vehicles.Max(static vehicle => Math.Max(0, vehicle.Capacity));

        double? averageVehicleOccupancy = null;
        TransitLineVehicleLoad[] vehiclesWithCapacity = input.Vehicles
            .Where(static vehicle => vehicle.Capacity > 0)
            .ToArray();
        if (vehiclesWithCapacity.Length > 0)
        {
            averageVehicleOccupancy = RoundPercent(
                vehiclesWithCapacity.Average(static vehicle => vehicle.PassengerCount / (double)vehicle.Capacity) * 100.0);
        }

        double? averageStopOccupancy = null;
        if (input.Stops.Count > 0 && stopCapacity > 0)
        {
            averageStopOccupancy = RoundPercent(
                input.Stops.Average(stop => Math.Max(0, stop.WaitingPassengers) / (double)stopCapacity) * 100.0);
        }

        double? expectedRoundTripTicks = null;
        double? expectedRoundTripMinutes = null;
        if (input.SegmentDurations.Count > 0 || input.Stops.Count > 0)
        {
            double segmentDuration = input.SegmentDurations.Sum(static duration => Math.Max(0.0, duration));
            expectedRoundTripTicks = Math.Round(
                segmentDuration * Math.PI + input.Stops.Count * 4.0,
                2,
                MidpointRounding.AwayFromZero);

            if (input.TicksPerDay.HasValue && input.TicksPerDay.Value > 0)
            {
                expectedRoundTripMinutes = Math.Round(
                    expectedRoundTripTicks.Value * 86400.0 / input.TicksPerDay.Value,
                    2,
                    MidpointRounding.AwayFromZero);
            }
        }

        TransitLineVehicleLoad? nextMaintenanceVehicle = input.Vehicles
            .Where(static vehicle => vehicle.MaintenanceRangeMeters.HasValue && vehicle.MaintenanceRangeMeters.Value > 0)
            .OrderBy(static vehicle => vehicle.MaintenanceRangeMeters!.Value - (vehicle.OdometerMeters ?? 0.0))
            .Cast<TransitLineVehicleLoad?>()
            .FirstOrDefault();

        return new TransitLineDetailCalculationResult
        {
            WaitingPassengersAllStops = waiting,
            MaxWaitingPassengersAtStop = maxWaiting,
            OnboardPassengersInVehicles = onboard,
            TotalPassengerCapacity = totalCapacity,
            StopCapacity = stopCapacity,
            AverageVehicleOccupancyPercent = averageVehicleOccupancy,
            AverageStopOccupancyPercent = averageStopOccupancy,
            ExpectedRoundTripTimeTicks = expectedRoundTripTicks,
            ExpectedRoundTripTimeMinutes = expectedRoundTripMinutes,
            NextMaintenanceVehicleEntityIndex = nextMaintenanceVehicle?.EntityIndex,
            NextMaintenanceVehicleEntityVersion = nextMaintenanceVehicle?.EntityVersion,
            NextMaintenanceDistanceMeters = nextMaintenanceVehicle.HasValue
                ? Math.Round(
                    nextMaintenanceVehicle.Value.MaintenanceRangeMeters!.Value - (nextMaintenanceVehicle.Value.OdometerMeters ?? 0.0),
                    2,
                    MidpointRounding.AwayFromZero)
                : null
        };
    }

    private static double RoundPercent(double value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
