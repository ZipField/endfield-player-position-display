using System;
using endfield_player_position_display.Services;

namespace endfield_player_position_display.Tests
{
    internal static class NetworkTimeServiceTests
    {
        public static void CalculateOffsetUsesNetworkDateMinusLocalDate()
        {
            DateTimeOffset local = DateTimeOffset.FromUnixTimeSeconds(1000);
            DateTimeOffset network = DateTimeOffset.FromUnixTimeSeconds(1012);

            TimeSpan offset = NetworkTimeService.CalculateOffset(local, network);

            TestAssert.AreEqual(TimeSpan.FromSeconds(12), offset);
        }
    }
}
