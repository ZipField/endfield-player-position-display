namespace endfield_player_position_display.Models
{
    public sealed class ZiplineRealtimeDetection
    {
        private ZiplineRealtimeDetection(bool detected, DetectedZiplineStop stop)
        {
            Detected = detected;
            Stop = stop;
        }

        public bool Detected { get; }
        public DetectedZiplineStop Stop { get; }

        public static ZiplineRealtimeDetection None()
        {
            return new ZiplineRealtimeDetection(false, null);
        }

        public static ZiplineRealtimeDetection Found(DetectedZiplineStop stop)
        {
            return new ZiplineRealtimeDetection(true, stop);
        }
    }
}
