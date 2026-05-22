using System.Globalization;

namespace endfield_player_position_display.Models
{
    public sealed class ZiplineLookupResult
    {
        private ZiplineLookupResult(bool found, int x, double y, int z, string direction, string message)
        {
            Found = found;
            X = x;
            Y = y;
            Z = z;
            Direction = direction;
            Message = message;
        }

        public bool Found { get; }
        public int X { get; }
        public double Y { get; }
        public int Z { get; }
        public string Direction { get; }
        public string Message { get; }

        public static ZiplineLookupResult FoundResult(int x, double y, int z, string direction)
        {
            return new ZiplineLookupResult(true, x, y, z, direction, null);
        }

        public static ZiplineLookupResult NotFound()
        {
            return new ZiplineLookupResult(false, 0, 0, 0, null, "未找到，刚放置的滑索可能需要过一小会才能查找到");
        }

        public string ToTupleText()
        {
            return string.Format(CultureInfo.InvariantCulture, "({0},{1},{2},{3})", X, Y, Z, Direction);
        }

        public string ToJsonText()
        {
            return string.Format(CultureInfo.InvariantCulture, "{{\"x\":{0},\"y\":{1},\"z\":{2},\"d\":\"{3}\"}}", X, Y, Z, Direction);
        }
    }
}
