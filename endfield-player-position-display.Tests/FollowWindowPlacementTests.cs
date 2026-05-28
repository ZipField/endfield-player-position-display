using System.Windows;
using endfield_player_position_display.Services;

namespace endfield_player_position_display.Tests
{
    internal static class FollowWindowPlacementTests
    {
        public static void CalculatePlacesAllEightDirections()
        {
            Rect game = new Rect(100, 200, 800, 600);
            Size window = new Size(160, 80);

            TestAssert.AreEqual(new Point(420, 208), FollowWindowPlacement.Calculate(game, window, "正上", 0, 0));
            TestAssert.AreEqual(new Point(420, 712), FollowWindowPlacement.Calculate(game, window, "正下", 0, 0));
            TestAssert.AreEqual(new Point(108, 460), FollowWindowPlacement.Calculate(game, window, "正左", 0, 0));
            TestAssert.AreEqual(new Point(732, 460), FollowWindowPlacement.Calculate(game, window, "正右", 0, 0));
            TestAssert.AreEqual(new Point(108, 208), FollowWindowPlacement.Calculate(game, window, "左上", 0, 0));
            TestAssert.AreEqual(new Point(732, 208), FollowWindowPlacement.Calculate(game, window, "右上", 0, 0));
            TestAssert.AreEqual(new Point(108, 682), FollowWindowPlacement.Calculate(game, window, "左下", 0, 0));
            TestAssert.AreEqual(new Point(732, 712), FollowWindowPlacement.Calculate(game, window, "右下", 0, 0));
        }

        public static void CalculateAppliesDirectionalOffsetsTowardInside()
        {
            Rect game = new Rect(100, 200, 800, 600);
            Size window = new Size(160, 80);

            TestAssert.AreEqual(new Point(138, 652), FollowWindowPlacement.Calculate(game, window, "左下", 30, 30));
            TestAssert.AreEqual(new Point(702, 682), FollowWindowPlacement.Calculate(game, window, "右下", 30, 30));
            TestAssert.AreEqual(new Point(450, 238), FollowWindowPlacement.Calculate(game, window, "正上", 30, 30));
            TestAssert.AreEqual(new Point(450, 682), FollowWindowPlacement.Calculate(game, window, "正下", 30, 30));
        }

        public static void CalculateAllowsNegativeCenterAxisOffsets()
        {
            Rect game = new Rect(100, 200, 800, 600);
            Size window = new Size(160, 80);

            TestAssert.AreEqual(new Point(220, 712), FollowWindowPlacement.Calculate(game, window, "正下", -200, 0));
            TestAssert.AreEqual(new Point(620, 712), FollowWindowPlacement.Calculate(game, window, "正下", 200, 0));
            TestAssert.AreEqual(new Point(108, 260), FollowWindowPlacement.Calculate(game, window, "正左", 0, -200));
            TestAssert.AreEqual(new Point(108, 660), FollowWindowPlacement.Calculate(game, window, "正左", 0, 200));
        }
    }
}
