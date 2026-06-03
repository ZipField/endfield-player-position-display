namespace endfield_player_position_display.Services
{
    public static class TokenTextFormatter
    {
        public static string MaskToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return "***";
            }

            string trimmed = token.Trim();
            if (trimmed.Length <= 8)
            {
                return "***";
            }

            return trimmed.Substring(0, 4) + "..." + trimmed.Substring(trimmed.Length - 4);
        }
    }
}
