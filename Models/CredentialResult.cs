namespace endfield_player_position_display.Models
{
    public sealed class CredentialResult
    {
        public CredentialResult(string cred, string userId, string token)
        {
            Cred = cred;
            UserId = userId;
            Token = token;
        }

        public string Cred { get; }
        public string UserId { get; }
        public string Token { get; }
    }
}
