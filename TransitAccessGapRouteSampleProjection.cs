using System.Collections.Generic;

namespace CS2DataExport;

public static class TransitAccessGapRouteSampleProjection
{
    public static TransitAccessGapSampleRoute BuildSampleRoute(
        int sampleIndex,
        bool directionIsProven,
        bool validatedTargets,
        params TransitAccessGapRouteSegmentRecord[] rawSegments)
    {
        if (!validatedTargets || rawSegments.Length == 0)
        {
            return new TransitAccessGapSampleRoute
            {
                SampleIndex = sampleIndex,
                SegmentCount = 0,
                Segments = System.Array.Empty<TransitAccessGapRouteSegment>()
            };
        }

        var segments = new List<TransitAccessGapRouteSegment>(rawSegments.Length);
        for (int index = 0; index < rawSegments.Length; index++)
        {
            TransitAccessGapRouteSegmentRecord segment = rawSegments[index];
            segments.Add(new TransitAccessGapRouteSegment
            {
                PathTargetEntityIndex = segment.PathTargetEntityIndex,
                PathTargetEntityVersion = segment.PathTargetEntityVersion,
                IsForward = directionIsProven ? segment.IsForward : null
            });
        }

        return new TransitAccessGapSampleRoute
        {
            SampleIndex = sampleIndex,
            SegmentCount = segments.Count,
            Segments = segments.ToArray()
        };
    }
}
