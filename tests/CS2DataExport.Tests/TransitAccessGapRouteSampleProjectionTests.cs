using Xunit;

namespace CS2DataExport.Tests;

public sealed class TransitAccessGapRouteSampleProjectionTests
{
    [Fact]
    public void BuildSampleRoute_OmitsSegments_WhenTargetsAreNotValidated()
    {
        TransitAccessGapSampleRoute route = TransitAccessGapRouteSampleProjection.BuildSampleRoute(
            sampleIndex: 0,
            directionIsProven: false,
            validatedTargets: false,
            new TransitAccessGapRouteSegmentRecord(101, 1, null));

        Assert.Empty(route.Segments);
    }
}
