namespace endfield_player_position_display.Models
{
    public sealed class ZiplineMotionPoint
    {
        public ZiplineMotionPoint(
            int order,
            double x,
            double y,
            double z,
            string source,
            double confidence,
            int startIndex,
            int endIndex)
        {
            Order = order;
            X = x;
            Y = y;
            Z = z;
            Source = source;
            Confidence = confidence;
            StartIndex = startIndex;
            EndIndex = endIndex;
        }

        public int Order { get; }
        public double X { get; }
        public double Y { get; }
        public double Z { get; }
        public string Source { get; }
        public double Confidence { get; }
        public int StartIndex { get; }
        public int EndIndex { get; }
    }
}
