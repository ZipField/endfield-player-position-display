namespace endfield_player_position_display.Models
{
    public sealed class ZiplineMark
    {
        public ZiplineMark(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double X { get; }
        public double Y { get; }
        public double Z { get; }
    }
}
