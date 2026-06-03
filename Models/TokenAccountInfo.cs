namespace endfield_player_position_display.Models
{
    public sealed class TokenAccountInfo
    {
        public TokenAccountInfo(int tokenIndex, string maskedToken, string rolesText, string statusText)
        {
            TokenIndex = tokenIndex;
            MaskedToken = maskedToken;
            RolesText = rolesText;
            StatusText = statusText;
        }

        public int TokenIndex { get; private set; }
        public string MaskedToken { get; private set; }
        public string RolesText { get; private set; }
        public string StatusText { get; private set; }

        public string DisplayText
        {
            get
            {
                string roles = string.IsNullOrWhiteSpace(RolesText) ? StatusText : RolesText;
                return (TokenIndex + 1) + ". " + MaskedToken + "    " + roles;
            }
        }
    }
}
