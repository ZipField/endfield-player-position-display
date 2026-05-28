namespace endfield_player_position_display.Models
{
    public sealed class RoleSession
    {
        public RoleSession(int tokenIndex, CredentialResult credential, RoleBinding roleBinding)
        {
            TokenIndex = tokenIndex;
            Credential = credential;
            RoleBinding = roleBinding;
            Key = tokenIndex + ":" + roleBinding.ServerId + ":" + roleBinding.RoleId;
        }

        public int TokenIndex { get; }
        public CredentialResult Credential { get; }
        public RoleBinding RoleBinding { get; }
        public string Key { get; }

        public string DisplayName
        {
            get { return RoleBinding == null ? "未知角色" : RoleBinding.DisplayName; }
        }
    }
}
