namespace endfield_player_position_display.Models
{
    using System.Collections.Generic;

    public sealed class MonitorSessionState
    {
        public MonitorSessionState(CredentialResult credential, RoleBinding roleBinding, string mapId)
            : this(credential, roleBinding, mapId, null, null)
        {
        }

        public MonitorSessionState(
            CredentialResult credential,
            RoleBinding roleBinding,
            string mapId,
            IReadOnlyList<RoleSession> availableRoles,
            RoleSession activeRole)
        {
            Credential = credential;
            RoleBinding = roleBinding;
            MapId = mapId;
            AvailableRoles = availableRoles;
            ActiveRole = activeRole;
        }

        public CredentialResult Credential { get; }
        public RoleBinding RoleBinding { get; }
        public string MapId { get; }
        public IReadOnlyList<RoleSession> AvailableRoles { get; }
        public RoleSession ActiveRole { get; }
    }
}
