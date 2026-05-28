using System.Collections.Generic;
using endfield_player_position_display.Models;
using endfield_player_position_display.Services;

namespace endfield_player_position_display.Tests
{
    internal static class ZiplineCollectionExporterTests
    {
        public static void ExportMarksJsonUsesNewFormatWithBidirectionalConnections()
        {
            var stops = new List<DetectedZiplineStop>
            {
                Stop(1, 117, 169, 766, "东"),
                Stop(2, 120, 170, 770, "南"),
                Stop(3, 117, 169, 766, "东")
            };

            string json = ZiplineCollectionExporter.ExportMarksJson(stops);

            TestAssert.AreEqual("[{\"id\":\"(117,766)\",\"name\":\"未命名滑索\",\"connect\":[\"(120,770)\"],\"h\":169,\"direction\":\"东\"},{\"id\":\"(120,770)\",\"name\":\"未命名滑索\",\"connect\":[\"(117,766)\"],\"h\":170,\"direction\":\"南\"}]", json);
        }

        public static void ExportRoutesJsonGroupsConnectedMarks()
        {
            var stops = new List<DetectedZiplineStop>
            {
                Stop(1, 1, 10, 1, "东"),
                Stop(2, 2, 10, 2, "西"),
                Stop(3, 3, 10, 3, "北")
            };

            string json = ZiplineCollectionExporter.ExportRoutesJson(stops);

            TestAssert.AreEqual("[{\"name\":\"未命名路线\",\"marks\":[\"(1,1)\",\"(2,2)\",\"(3,3)\"]}]", json);
        }

        public static void ExportRoutesJsonSplitsWhenStopDoesNotConnectToPrevious()
        {
            var stops = new List<DetectedZiplineStop>
            {
                Stop(1, -1362, 326, -414, "北"),
                Stop(2, -1271, 316, -443, "南"),
                Stop(3, -1070, 269, -565, "南"),
                Stop(4, -1069, 272, -584, "南", false),
                Stop(5, -966, 268, -606, "东")
            };

            string routeJson = ZiplineCollectionExporter.ExportRoutesJson(stops);
            string marksJson = ZiplineCollectionExporter.ExportMarksJson(stops);

            TestAssert.AreEqual("[{\"name\":\"未命名路线\",\"marks\":[\"(-1362,-414)\",\"(-1271,-443)\",\"(-1070,-565)\"]},{\"name\":\"未命名路线\",\"marks\":[\"(-1069,-584)\",\"(-966,-606)\"]}]", routeJson);
            TestAssert.AreEqual(true, marksJson.Contains("\"id\":\"(-1070,-565)\",\"name\":\"未命名滑索\",\"connect\":[\"(-1271,-443)\"]"));
            TestAssert.AreEqual(true, marksJson.Contains("\"id\":\"(-1069,-584)\",\"name\":\"未命名滑索\",\"connect\":[\"(-966,-606)\"]"));
        }

        private static DetectedZiplineStop Stop(int order, int x, double y, int z, string direction, bool connectToPrevious = true)
        {
            return new DetectedZiplineStop(order, x, y, z, direction, 0, 0, connectToPrevious);
        }
    }
}
