using System;

namespace endfield_player_position_display.Models
{
    public sealed class ZiplineMotionSample
    {
        public ZiplineMotionSample(
            int index,
            DateTimeOffset timestamp,
            string label,
            string eventName,
            double x,
            double y,
            double z,
            double planarSpeed)
        {
            Index = index;
            Timestamp = timestamp;
            Label = label ?? string.Empty;
            EventName = eventName ?? string.Empty;
            X = x;
            Y = y;
            Z = z;
            PlanarSpeed = planarSpeed;
        }

        public int Index { get; }
        public DateTimeOffset Timestamp { get; }
        public string Label { get; }
        public string EventName { get; }
        public double X { get; }
        public double Y { get; }
        public double Z { get; }
        public double PlanarSpeed { get; }
    }
}
