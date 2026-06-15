using Xunit;

namespace CS2DataExport.Tests;

public sealed class TransitAccessGapAnalyzerTests
{
    [Fact]
    public void BuildSummary_ClustersAnchors_AndRanksUncoveredDemandFirst()
    {
        var analyzer = new TransitAccessGapAnalyzer();
        var capture = new CompletedTransitAccessGapCapture
        {
            CaptureMode = "next_export_window",
            ClusterRadiusM = 192,
            StopCoverageRadiusM = 250,
            RecordedTrips =
            {
                new CapturedTransitTrip
                {
                    IncludesOutsideConnection = false,
                    Anchors =
                    {
                        new TransitAccessGapAnchor(0, 0, 0),
                        new TransitAccessGapAnchor(12, 0, 8)
                    },
                    RouteSegments =
                    {
                        new TransitAccessGapRouteSegmentRecord(101, 1, true),
                        new TransitAccessGapRouteSegmentRecord(102, 1, true)
                    }
                },
                new CapturedTransitTrip
                {
                    IncludesOutsideConnection = false,
                    Anchors =
                    {
                        new TransitAccessGapAnchor(18, 0, 15)
                    }
                },
                new CapturedTransitTrip
                {
                    IncludesOutsideConnection = false,
                    Anchors =
                    {
                        new TransitAccessGapAnchor(900, 0, 900)
                    }
                }
            },
            Stops =
            {
                new TransitAccessGapStop(0, 0, 0, 100),
                new TransitAccessGapStop(1000, 0, 1000, 100)
            }
        };

        TransitAccessGapSemanticsSummary summary = analyzer.BuildSummary(capture);

        Assert.Equal(MetricStatus.Ok, summary.Status);
        Assert.Equal(2, summary.Hotspots.Length);
        Assert.True(summary.Hotspots[0].PriorityScore >= summary.Hotspots[1].PriorityScore);
        Assert.True(summary.Hotspots[0].UncoveredSharePercent > 0);
        Assert.Equal(summary.Hotspots[0].SampleRouteCount, summary.Hotspots[0].SampleRoutes.Length);
    }

    [Fact]
    public void BuildSummary_NullsSampleRouteDirection_WhenDirectionIsNotProven()
    {
        var analyzer = new TransitAccessGapAnalyzer();
        var capture = new CompletedTransitAccessGapCapture
        {
            CaptureMode = "next_export_window",
            ClusterRadiusM = 192,
            StopCoverageRadiusM = 250,
            RecordedTrips =
            {
                new CapturedTransitTrip
                {
                    Anchors =
                    {
                        new TransitAccessGapAnchor(600, 0, 600)
                    },
                    RouteSegments =
                    {
                        new TransitAccessGapRouteSegmentRecord(101, 1, true)
                    }
                }
            }
        };

        TransitAccessGapSemanticsSummary summary = analyzer.BuildSummary(capture);

        TransitAccessGapSampleRoute route = Assert.Single(summary.Hotspots).SampleRoutes[0];
        TransitAccessGapRouteSegment segment = Assert.Single(route.Segments);
        Assert.Equal(101, segment.PathTargetEntityIndex);
        Assert.Equal(1, segment.PathTargetEntityVersion);
        Assert.Null(segment.IsForward);
    }
}
