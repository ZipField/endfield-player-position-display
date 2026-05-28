using System.Collections.Generic;

namespace endfield_player_position_display.Models
{
    public sealed class ZiplineMotionAnalysisResult
    {
        public ZiplineMotionAnalysisResult(
            IReadOnlyList<ZiplineMotionPoint> points,
            IReadOnlyList<ZiplineMotionSegment> movingSegments)
        {
            Points = points;
            MovingSegments = movingSegments;
        }

        public IReadOnlyList<ZiplineMotionPoint> Points { get; }
        public IReadOnlyList<ZiplineMotionSegment> MovingSegments { get; }
    }
}
