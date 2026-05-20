using System.Globalization;

namespace endfield_player_position_display.Services
{
    public static class CoordinateFormatter
    {
        public static string Format(double value)
        {
            string text = value.ToString("0.00000", CultureInfo.InvariantCulture);
            int dotIndex = text.IndexOf('.');
            if (dotIndex < 0)
            {
                return text.PadLeft(10);
            }

            string integerPart = text.Substring(0, dotIndex).PadLeft(4);
            string fractionPart = text.Substring(dotIndex + 1).TrimEnd('0').PadRight(5);
            return integerPart + "." + fractionPart;
        }
    }
}
