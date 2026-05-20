namespace endfield_player_position_display.Models
{
    public sealed class MonitorUpdate
    {
        private MonitorUpdate(PositionSnapshot position, string status, bool isError)
        {
            Position = position;
            Status = status;
            IsError = isError;
        }

        public PositionSnapshot Position { get; }
        public string Status { get; }
        public bool IsError { get; }

        public static MonitorUpdate Connecting(string status)
        {
            return new MonitorUpdate(null, status, false);
        }

        public static MonitorUpdate FromPosition(PositionSnapshot position)
        {
            return new MonitorUpdate(position, null, false);
        }

        public static MonitorUpdate Error(string message)
        {
            return new MonitorUpdate(null, message, true);
        }
    }
}
