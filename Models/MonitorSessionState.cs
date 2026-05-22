namespace endfield_player_position_display.Models
{
    public sealed class MonitorSessionState
    {
        public MonitorSessionState(CredentialResult credential, RoleBinding roleBinding, string mapId)
        {
            Credential = credential;
            RoleBinding = roleBinding;
            MapId = mapId;
        }

        public CredentialResult Credential { get; }
        public RoleBinding RoleBinding { get; }
        public string MapId { get; }
    }
}
