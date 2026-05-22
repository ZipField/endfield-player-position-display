using endfield_player_position_display.Models;
using endfield_player_position_display.Services;

namespace endfield_player_position_display.Tests
{
    internal static class ZiplineMatcherTests
    {
        public static void FindNearestMatchesBottomLeftAsNorth()
        {
            var marks = new[] { new ZiplineMark(10.2, 7.5, 20.8) };

            ZiplineLookupResult result = ZiplineMatcher.FindNearest(new PositionSnapshot(11.3, 99, 21.9), marks);

            TestAssert.AreEqual(true, result.Found);
            TestAssert.AreEqual(11, result.X);
            TestAssert.AreEqual(7.5, result.Y);
            TestAssert.AreEqual(21, result.Z);
            TestAssert.AreEqual("北", result.Direction);
        }

        public static void FindNearestMatchesBottomRightAsWest()
        {
            var marks = new[] { new ZiplineMark(10, 8, 20) };

            ZiplineLookupResult result = ZiplineMatcher.FindNearest(new PositionSnapshot(9, 1, 21), marks);

            TestAssert.AreEqual(true, result.Found);
            TestAssert.AreEqual("西", result.Direction);
        }

        public static void FindNearestMatchesTopRightAsSouth()
        {
            var marks = new[] { new ZiplineMark(10, 8, 20) };

            ZiplineLookupResult result = ZiplineMatcher.FindNearest(new PositionSnapshot(9, 1, 19), marks);

            TestAssert.AreEqual(true, result.Found);
            TestAssert.AreEqual("南", result.Direction);
        }

        public static void FindNearestMatchesTopLeftAsEast()
        {
            var marks = new[] { new ZiplineMark(10, 8, 20) };

            ZiplineLookupResult result = ZiplineMatcher.FindNearest(new PositionSnapshot(11, 1, 19), marks);

            TestAssert.AreEqual(true, result.Found);
            TestAssert.AreEqual("东", result.Direction);
        }

        public static void FindNearestReturnsNoMatchBeyondThreeMeters()
        {
            var marks = new[] { new ZiplineMark(10, 8, 20) };

            ZiplineLookupResult result = ZiplineMatcher.FindNearest(new PositionSnapshot(100, 1, 100), marks);

            TestAssert.AreEqual(false, result.Found);
            TestAssert.AreEqual("未找到，刚放置的滑索可能需要过一小会才能查找到", result.Message);
        }

        public static void FindNearestChoosesClosestCandidate()
        {
            var marks = new[]
            {
                new ZiplineMark(50, 1, 50),
                new ZiplineMark(10, 2, 20)
            };

            ZiplineLookupResult result = ZiplineMatcher.FindNearest(new PositionSnapshot(9.1, 1, 20.9), marks);

            TestAssert.AreEqual(true, result.Found);
            TestAssert.AreEqual(2, result.Y);
            TestAssert.AreEqual("西", result.Direction);
        }

        public static void FormatsCopyValues()
        {
            var result = ZiplineLookupResult.FoundResult(1, 2.5, 3, "北");

            TestAssert.AreEqual("(1,2.5,3,北)", result.ToTupleText());
            TestAssert.AreEqual("{\"x\":1,\"y\":2.5,\"z\":3,\"d\":\"北\"}", result.ToJsonText());
        }
    }
}
