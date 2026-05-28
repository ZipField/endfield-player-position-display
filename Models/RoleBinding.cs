namespace endfield_player_position_display.Models
{
    public sealed class RoleBinding
    {
        public RoleBinding(string serverId, string roleId)
            : this(serverId, roleId, null, null)
        {
        }

        public RoleBinding(string serverId, string roleId, string nickname, string channelName)
        {
            ServerId = serverId;
            RoleId = roleId;
            Nickname = nickname;
            ChannelName = channelName;
        }

        public string ServerId { get; }
        public string RoleId { get; }
        public string Nickname { get; }
        public string ChannelName { get; }

        public string DisplayName
        {
            get
            {
                string name = string.IsNullOrWhiteSpace(Nickname) ? RoleId : Nickname;
                return string.IsNullOrWhiteSpace(ChannelName) ? name : ChannelName + " - " + name;
            }
        }
    }
}
