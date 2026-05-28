namespace endfield_player_position_display.Models
{
    public sealed class MonitorUpdate
    {
        private MonitorUpdate(PositionSnapshot position, string status, bool isError, MonitorSessionState sessionState)
        {
            Position = position;
            Status = status;
            IsError = isError;
            SessionState = sessionState;
        }

        public PositionSnapshot Position { get; }
        public string Status { get; }
        public bool IsError { get; }
        public MonitorSessionState SessionState { get; }

        public static MonitorUpdate Connecting(string status)
        {
            return new MonitorUpdate(null, status, false, null);
        }

        public static MonitorUpdate SessionReady(CredentialResult credential, RoleBinding roleBinding)
        {
            return new MonitorUpdate(null, "已连接", false, new MonitorSessionState(credential, roleBinding, null));
        }

        public static MonitorUpdate SessionReady(RoleSession activeRole, System.Collections.Generic.IReadOnlyList<RoleSession> availableRoles)
        {
            return new MonitorUpdate(
                null,
                activeRole == null ? "已连接" : "已连接：" + activeRole.DisplayName,
                false,
                new MonitorSessionState(activeRole.Credential, activeRole.RoleBinding, null, availableRoles, activeRole));
        }

        public static MonitorUpdate FromPosition(PositionSnapshot position)
        {
            return new MonitorUpdate(position, null, false, null);
        }

        public static MonitorUpdate FromPosition(PositionSnapshot position, MonitorSessionState sessionState)
        {
            return new MonitorUpdate(position, null, false, sessionState);
        }

        public static MonitorUpdate Error(string message)
        {
            return new MonitorUpdate(null, message, true, null);
        }
    }
}
