namespace endfield_player_position_display.Models
{
    public sealed class ZiplineMotionSegment
    {
        public ZiplineMotionSegment(
            int startIndex,
            int endIndex,
            double headingDegrees,
            double planarDistance,
            double medianSpeed)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
            HeadingDegrees = headingDegrees;
            PlanarDistance = planarDistance;
            MedianSpeed = medianSpeed;
        }

        public int StartIndex { get; }
        public int EndIndex { get; }
        public double HeadingDegrees { get; }
        public double PlanarDistance { get; }
        public double MedianSpeed { get; }
    }
}
