using System;
using System.Collections.Generic;

namespace CS2DataExport;

public sealed class TransitAccessGapAnalyzer
{
    public TransitAccessGapSemanticsSummary BuildSummary(CompletedTransitAccessGapCapture capture)
    {
        if (capture.RecordedTrips.Count == 0)
        {
            return new TransitAccessGapSemanticsSummary
            {
                Status = MetricStatus.Unavailable,
                Notes = new[] { "no trips were recorded during the completed capture window" },
                CaptureContext = BuildCaptureContext(capture),
                Summary = new TransitAccessGapSummary(),
                Hotspots = Array.Empty<TransitAccessGapHotspot>()
            };
        }

        var clusters = new List<TransitAccessGapCluster>();
        for (int tripIndex = 0; tripIndex < capture.RecordedTrips.Count; tripIndex++)
        {
            CapturedTransitTrip trip = capture.RecordedTrips[tripIndex];
            var touchedClusterIndexes = new HashSet<int>();
            for (int anchorIndex = 0; anchorIndex < trip.Anchors.Count; anchorIndex++)
            {
                TransitAccessGapAnchor anchor = trip.Anchors[anchorIndex];
                int clusterIndex = FindOrCreateCluster(clusters, anchor, capture.ClusterRadiusM);
                TransitAccessGapCluster cluster = clusters[clusterIndex];
                cluster.AnchorCount++;
                cluster.SumX += anchor.X;
                cluster.SumY += anchor.Y;
                cluster.SumZ += anchor.Z;

                EvaluateCoverage(anchor, capture.Stops, out double nearestDistance, out double uncoveredDistance);
                cluster.NearestDistanceSum += nearestDistance;
                cluster.UncoveredDistanceSum += uncoveredDistance;
                if (uncoveredDistance > 0)
                {
                    cluster.UncoveredAnchorCount++;
                }
                cluster.IncludesOutsideTrips |= trip.IncludesOutsideConnection;

                touchedClusterIndexes.Add(clusterIndex);
            }

            if (trip.RouteSegments.Count == 0)
            {
                continue;
            }

            foreach (int clusterIndex in touchedClusterIndexes)
            {
                TransitAccessGapCluster cluster = clusters[clusterIndex];
                if (cluster.SampleRoutes.Count >= capture.MaxSampleRoutesPerHotspot)
                {
                    continue;
                }

                cluster.SampleRoutes.Add(BuildSampleRoute(cluster.SampleRoutes.Count, trip.RouteSegments));
            }
        }

        var hotspots = new List<TransitAccessGapHotspot>(clusters.Count);
        for (int index = 0; index < clusters.Count; index++)
        {
            TransitAccessGapCluster cluster = clusters[index];
            if (cluster.AnchorCount == 0)
            {
                continue;
            }

            double uncoveredShare = (double)cluster.UncoveredAnchorCount / cluster.AnchorCount;
            double averageNearestDistance = cluster.NearestDistanceSum / cluster.AnchorCount;
            double averageUncoveredDistance = cluster.UncoveredDistanceSum / cluster.AnchorCount;
            double priorityScore = ComputePriorityScore(cluster.AnchorCount, uncoveredShare, averageUncoveredDistance);

            hotspots.Add(new TransitAccessGapHotspot
            {
                HotspotId = $"hotspot_{index + 1}",
                Label = $"demand_hotspot_x{cluster.AnchorCount}",
                CenterPosition = new TransitAccessGapPosition
                {
                    X = cluster.SumX / cluster.AnchorCount,
                    Y = cluster.SumY / cluster.AnchorCount,
                    Z = cluster.SumZ / cluster.AnchorCount
                },
                ObservedTripCount = cluster.AnchorCount,
                SampleRouteCount = cluster.SampleRoutes.Count,
                BucketIndex = 0,
                PriorityScore = Math.Round(priorityScore, 2, MidpointRounding.AwayFromZero),
                UncoveredSharePercent = Math.Round(uncoveredShare * 100.0, 2, MidpointRounding.AwayFromZero),
                AverageNearestStopDistanceM = Math.Round(averageNearestDistance, 2, MidpointRounding.AwayFromZero),
                AverageUncoveredDistanceM = Math.Round(averageUncoveredDistance, 2, MidpointRounding.AwayFromZero),
                IncludesOutsideTrips = cluster.IncludesOutsideTrips,
                SampleRoutes = cluster.SampleRoutes.ToArray()
            });
        }

        hotspots.Sort(static (left, right) =>
        {
            int priorityComparison = Nullable.Compare(right.PriorityScore, left.PriorityScore);
            if (priorityComparison != 0)
            {
                return priorityComparison;
            }

            return string.CompareOrdinal(left.HotspotId, right.HotspotId);
        });

        if (hotspots.Count > capture.MaxHotspots)
        {
            hotspots.RemoveRange(capture.MaxHotspots, hotspots.Count - capture.MaxHotspots);
        }

        for (int index = 0; index < hotspots.Count; index++)
        {
            TransitAccessGapHotspot hotspot = hotspots[index];
            hotspots[index] = new TransitAccessGapHotspot
            {
                HotspotId = hotspot.HotspotId,
                Label = hotspot.Label,
                CenterPosition = hotspot.CenterPosition,
                ObservedTripCount = hotspot.ObservedTripCount,
                SampleRouteCount = hotspot.SampleRouteCount,
                BucketIndex = ComputeBucketIndex(index, hotspots.Count),
                PriorityScore = hotspot.PriorityScore,
                UncoveredSharePercent = hotspot.UncoveredSharePercent,
                AverageNearestStopDistanceM = hotspot.AverageNearestStopDistanceM,
                AverageUncoveredDistanceM = hotspot.AverageUncoveredDistanceM,
                IncludesOutsideTrips = hotspot.IncludesOutsideTrips,
                SampleRoutes = hotspot.SampleRoutes
            };
        }

        return new TransitAccessGapSemanticsSummary
        {
            Status = MetricStatus.Ok,
            Notes = new[]
            {
                "transit access gap semantics are derived from observed capture-window trip anchors and current passenger-stop coverage."
            },
            CaptureContext = BuildCaptureContext(capture),
            Summary = new TransitAccessGapSummary
            {
                HotspotsTotal = hotspots.Count,
                HotspotsWithUncoveredDemand = CountHotspotsWithUncoveredDemand(hotspots),
                HighPriorityHotspots = CountHotspotsAtOrAboveBucket(hotspots, 2),
                CriticalPriorityHotspots = CountHotspotsAtOrAboveBucket(hotspots, 3)
            },
            Hotspots = hotspots.ToArray()
        };
    }

    internal static double ComputePriorityScore(int observedTripCount, double uncoveredShare0To1, double averageUncoveredDistanceM)
    {
        double shareWeight = Lerp(0.35, 1.0, Clamp01(uncoveredShare0To1));
        double distanceWeight = Lerp(0.35, 1.0, Clamp01(averageUncoveredDistanceM / 96.0));
        return observedTripCount * shareWeight * distanceWeight;
    }

    private static TransitAccessGapCaptureContext BuildCaptureContext(CompletedTransitAccessGapCapture capture)
    {
        return new TransitAccessGapCaptureContext
        {
            CaptureMode = capture.CaptureMode,
            CaptureDurationSeconds = capture.CaptureDurationSeconds,
            RecordedTripCount = capture.RecordedTrips.Count,
            IncludedSnapshotCount = capture.RecordedTrips.Count,
            OutsideTripMode = capture.IncludeOutsideTrips ? "include" : "exclude",
            OutsideTripCount = CountOutsideTrips(capture.RecordedTrips),
            ClusterRadiusM = capture.ClusterRadiusM,
            StopCoverageRadiusM = capture.StopCoverageRadiusM
        };
    }

    private static int CountOutsideTrips(IReadOnlyList<CapturedTransitTrip> trips)
    {
        int count = 0;
        for (int index = 0; index < trips.Count; index++)
        {
            if (trips[index].IncludesOutsideConnection)
            {
                count++;
            }
        }

        return count;
    }

    private static int FindOrCreateCluster(List<TransitAccessGapCluster> clusters, TransitAccessGapAnchor anchor, int clusterRadiusM)
    {
        int bestIndex = -1;
        double bestDistanceSquared = (double)clusterRadiusM * clusterRadiusM;

        for (int index = 0; index < clusters.Count; index++)
        {
            TransitAccessGapCluster cluster = clusters[index];
            if (cluster.AnchorCount == 0)
            {
                continue;
            }

            double centerX = cluster.SumX / cluster.AnchorCount;
            double centerZ = cluster.SumZ / cluster.AnchorCount;
            double dx = centerX - anchor.X;
            double dz = centerZ - anchor.Z;
            double distanceSquared = dx * dx + dz * dz;
            if (distanceSquared <= bestDistanceSquared)
            {
                bestDistanceSquared = distanceSquared;
                bestIndex = index;
            }
        }

        if (bestIndex >= 0)
        {
            return bestIndex;
        }

        clusters.Add(new TransitAccessGapCluster());
        return clusters.Count - 1;
    }

    private static void EvaluateCoverage(TransitAccessGapAnchor anchor, IReadOnlyList<TransitAccessGapStop> stops, out double nearestDistance, out double uncoveredDistance)
    {
        nearestDistance = double.PositiveInfinity;
        uncoveredDistance = double.PositiveInfinity;

        if (stops.Count == 0)
        {
            return;
        }

        for (int index = 0; index < stops.Count; index++)
        {
            TransitAccessGapStop stop = stops[index];
            double dx = stop.X - anchor.X;
            double dz = stop.Z - anchor.Z;
            double distance = Math.Sqrt(dx * dx + dz * dz);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                uncoveredDistance = Math.Max(0.0, distance - stop.AccessRadiusM);
            }
        }
    }

    private static TransitAccessGapSampleRoute BuildSampleRoute(int sampleIndex, IReadOnlyList<TransitAccessGapRouteSegmentRecord> routeSegments)
    {
        var rawSegments = new TransitAccessGapRouteSegmentRecord[routeSegments.Count];
        for (int index = 0; index < routeSegments.Count; index++)
        {
            rawSegments[index] = routeSegments[index];
        }

        return TransitAccessGapRouteSampleProjection.BuildSampleRoute(
            sampleIndex,
            directionIsProven: false,
            validatedTargets: true,
            rawSegments);
    }

    private static int ComputeBucketIndex(int index, int totalCount)
    {
        if (totalCount <= 1)
        {
            return 3;
        }

        double percentile = (double)index / Math.Max(1, totalCount - 1);
        if (percentile <= 0.2)
        {
            return 3;
        }

        if (percentile <= 0.45)
        {
            return 2;
        }

        if (percentile <= 0.7)
        {
            return 1;
        }

        return 0;
    }

    private static int CountHotspotsWithUncoveredDemand(IReadOnlyList<TransitAccessGapHotspot> hotspots)
    {
        int count = 0;
        for (int index = 0; index < hotspots.Count; index++)
        {
            if ((hotspots[index].UncoveredSharePercent ?? 0) > 0)
            {
                count++;
            }
        }

        return count;
    }

    private static int CountHotspotsAtOrAboveBucket(IReadOnlyList<TransitAccessGapHotspot> hotspots, int minimumBucket)
    {
        int count = 0;
        for (int index = 0; index < hotspots.Count; index++)
        {
            if ((hotspots[index].BucketIndex ?? 0) >= minimumBucket)
            {
                count++;
            }
        }

        return count;
    }

    private static double Clamp01(double value)
    {
        if (value < 0)
        {
            return 0;
        }

        if (value > 1)
        {
            return 1;
        }

        return value;
    }

    private static double Lerp(double left, double right, double t)
    {
        return left + ((right - left) * t);
    }

    private sealed class TransitAccessGapCluster
    {
        public int AnchorCount { get; set; }
        public int UncoveredAnchorCount { get; set; }
        public double SumX { get; set; }
        public double SumY { get; set; }
        public double SumZ { get; set; }
        public double NearestDistanceSum { get; set; }
        public double UncoveredDistanceSum { get; set; }
        public bool IncludesOutsideTrips { get; set; }
        public List<TransitAccessGapSampleRoute> SampleRoutes { get; } = new();
    }
}

public sealed class CompletedTransitAccessGapCapture
{
    public string CaptureMode { get; init; } = "off";
    public int CaptureDurationSeconds { get; init; }
    public bool IncludeOutsideTrips { get; init; }
    public int ClusterRadiusM { get; init; } = 192;
    public double StopCoverageRadiusM { get; init; } = 250;
    public int MaxSampleRoutesPerHotspot { get; init; } = 5;
    public int MaxHotspots { get; init; } = 50;
    public List<CapturedTransitTrip> RecordedTrips { get; } = new();
    public List<TransitAccessGapStop> Stops { get; } = new();
}

public sealed class CapturedTransitTrip
{
    public bool IncludesOutsideConnection { get; init; }
    public List<TransitAccessGapAnchor> Anchors { get; } = new();
    public List<TransitAccessGapRouteSegmentRecord> RouteSegments { get; } = new();
}

public sealed record TransitAccessGapAnchor(double X, double Y, double Z);

public sealed record TransitAccessGapStop(double X, double Y, double Z, double AccessRadiusM);

public sealed record TransitAccessGapRouteSegmentRecord(int PathTargetEntityIndex, int PathTargetEntityVersion, bool? IsForward);
