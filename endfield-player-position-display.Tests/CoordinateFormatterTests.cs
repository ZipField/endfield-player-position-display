using endfield_player_position_display.Services;

namespace endfield_player_position_display.Tests
{
    internal static class CoordinateFormatterTests
    {
        public static void FormatPadsIntegerPartAndKeepsFiveFractionDigits()
        {
            TestAssert.AreEqual("  13.02   ", CoordinateFormatter.Format(13.02));
            TestAssert.AreEqual(" 159.36252", CoordinateFormatter.Format(159.36252));
            TestAssert.AreEqual("-502.3604 ", CoordinateFormatter.Format(-502.3604));
        }
    }
}
