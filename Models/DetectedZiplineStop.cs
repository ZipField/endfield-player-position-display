namespace endfield_player_position_display.Models
{
    public sealed class DetectedZiplineStop
    {
        public DetectedZiplineStop(
            int order,
            int x,
            double y,
            int z,
            string direction,
            double distance,
            double heightOffset,
            bool connectToPrevious = true)
        {
            Order = order;
            X = x;
            Y = y;
            Z = z;
            Direction = direction;
            Distance = distance;
            HeightOffset = heightOffset;
            ConnectToPrevious = connectToPrevious;
        }

        public int Order { get; }
        public int X { get; }
        public double Y { get; }
        public int Z { get; }
        public string Direction { get; }
        public double Distance { get; }
        public double HeightOffset { get; }
        public bool ConnectToPrevious { get; }
    }
}
