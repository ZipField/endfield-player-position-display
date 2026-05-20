namespace endfield_player_position_display.Models
{
    public sealed class RoleBinding
    {
        public RoleBinding(string serverId, string roleId)
        {
            ServerId = serverId;
            RoleId = roleId;
        }

        public string ServerId { get; }
        public string RoleId { get; }
    }
}
