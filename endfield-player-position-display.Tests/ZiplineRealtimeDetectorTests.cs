using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using endfield_player_position_display.Models;
using endfield_player_position_display.Services;

namespace endfield_player_position_display.Tests
{
    internal static class ZiplineRealtimeDetectorTests
    {
        public static void DetectorConfirmsStablePositionNearMarkWithHeightOffset()
        {
            var detector = new ZiplineRealtimeDetector(new[]
            {
                new ZiplineMark(100, 200, 300)
            });

            TestAssert.AreEqual(false, detector.Update(new PositionSnapshot(101.2, 203.45, 301.1)).Detected);
            ZiplineRealtimeDetection detection = detector.Update(new PositionSnapshot(101.1, 203.5, 301.0));

            TestAssert.AreEqual(true, detection.Detected);
            TestAssert.AreEqual(101, detection.Stop.X);
            TestAssert.AreEqual(200.0, detection.Stop.Y);
            TestAssert.AreEqual(301, detection.Stop.Z);
            TestAssert.AreEqual("北", detection.Stop.Direction);
        }

        public static void DetectorRejectsGroundPositionNearMarkWithWrongHeight()
        {
            var detector = new ZiplineRealtimeDetector(new[]
            {
                new ZiplineMark(100, 200, 300)
            });

            detector.Update(new PositionSnapshot(101.2, 200.05, 301.1));
            detector.Update(new PositionSnapshot(101.1, 200.08, 301.0));
            ZiplineRealtimeDetection detection = detector.Update(new PositionSnapshot(101.0, 200.06, 301.1));

            TestAssert.AreEqual(false, detection.Detected);
        }

        public static void DetectorDoesNotRepeatUntilLeavingPreviousMark()
        {
            var detector = new ZiplineRealtimeDetector(new List<ZiplineMark>
            {
                new ZiplineMark(100, 200, 300),
                new ZiplineMark(150, 205, 300)
            });

            detector.Update(new PositionSnapshot(101.2, 203.45, 301.1));
            TestAssert.AreEqual(true, detector.Update(new PositionSnapshot(101.1, 203.5, 301.0)).Detected);

            detector.Update(new PositionSnapshot(101.1, 203.5, 301.0));
            detector.Update(new PositionSnapshot(101.0, 203.48, 301.1));
            TestAssert.AreEqual(false, detector.Update(new PositionSnapshot(101.1, 203.5, 301.0)).Detected);

            detector.Update(new PositionSnapshot(130, 203.5, 301));
            detector.Update(new PositionSnapshot(151.2, 208.45, 301.0));
            ZiplineRealtimeDetection detection = detector.Update(new PositionSnapshot(151.1, 208.5, 301.1));

            TestAssert.AreEqual(true, detection.Detected);
            TestAssert.AreEqual(2, detection.Stop.Order);
        }

        public static void DetectorReplaysLatestCaptureAndFindsFourStops()
        {
            string directory = Path.Combine(
                System.AppDomain.CurrentDomain.BaseDirectory,
                "..",
                "..",
                "..",
                "bin",
                "Debug",
                "captures",
                "zipline-20260527-003650");

            string marksPath = Path.Combine(directory, "marks.csv");
            string positionsPath = Path.Combine(directory, "positions.csv");
            if (!File.Exists(marksPath) || !File.Exists(positionsPath))
            {
                return;
            }

            var detector = new ZiplineRealtimeDetector(ReadMarks(marksPath));
            foreach (PositionSnapshot position in ReadPositions(positionsPath))
            {
                detector.Update(position);
            }

            TestAssert.AreEqual(4, detector.DetectedStops.Count);
        }

        public static void DetectorReplaysCaptureWithFastConfirm()
        {
            string directory = Path.Combine(
                System.AppDomain.CurrentDomain.BaseDirectory,
                "..",
                "..",
                "..",
                "bin",
                "Debug",
                "captures",
                "zipline-20260527-010142");

            string marksPath = Path.Combine(directory, "marks.csv");
            string positionsPath = Path.Combine(directory, "positions.csv");
            if (!File.Exists(marksPath) || !File.Exists(positionsPath))
            {
                return;
            }

            var detector = new ZiplineRealtimeDetector(ReadMarks(marksPath));
            var detectedIndexes = new List<int>();
            int index = 0;
            foreach (PositionSnapshot position in ReadPositions(positionsPath))
            {
                ZiplineRealtimeDetection detection = detector.Update(position);
                if (detection.Detected)
                {
                    detectedIndexes.Add(index);
                }

                index++;
            }

            TestAssert.AreEqual(5, detectedIndexes.Count);
            TestAssert.AreEqual(true, detectedIndexes.SequenceEqual(new[] { 1, 14, 25, 33, 50 }));
        }

        public static void DetectorReplaysCapture164946AndSplitsGroundTransition()
        {
            string directory = Path.Combine(
                System.AppDomain.CurrentDomain.BaseDirectory,
                "..",
                "..",
                "..",
                "bin",
                "Debug",
                "captures",
                "zipline-20260527-164946");

            string marksPath = Path.Combine(directory, "marks.csv");
            string positionsPath = Path.Combine(directory, "positions.csv");
            if (!File.Exists(marksPath) || !File.Exists(positionsPath))
            {
                return;
            }

            var detector = new ZiplineRealtimeDetector(ReadMarks(marksPath));
            foreach (PositionSnapshot position in ReadPositions(positionsPath))
            {
                detector.Update(position);
            }

            TestAssert.AreEqual(10, detector.DetectedStops.Count);
            TestAssert.AreEqual("(-1362,-414)", Id(detector.DetectedStops[0]));
            TestAssert.AreEqual("(-1070,-565)", Id(detector.DetectedStops[4]));
            TestAssert.AreEqual("(-1069,-584)", Id(detector.DetectedStops[5]));
            TestAssert.AreEqual(false, detector.DetectedStops[5].ConnectToPrevious);

            string routesJson = detector.GetRouteCollectionJson();
            TestAssert.AreEqual(true, routesJson.Contains("\"marks\":[\"(-1362,-414)\",\"(-1271,-443)\",\"(-1201,-484)\",\"(-1109,-523)\",\"(-1070,-565)\"]"));
            TestAssert.AreEqual(true, routesJson.Contains("\"marks\":[\"(-1069,-584)\",\"(-966,-606)\",\"(-862,-635)\",\"(-821,-667)\",\"(-774,-656)\"]"));
        }

        private static string Id(DetectedZiplineStop stop)
        {
            return string.Format(CultureInfo.InvariantCulture, "({0},{1})", stop.X, stop.Z);
        }

        private static IEnumerable<ZiplineMark> ReadMarks(string path)
        {
            string[] lines = File.ReadAllLines(path);
            for (int i = 1; i < lines.Length; i++)
            {
                string[] parts = lines[i].Split(',');
                yield return new ZiplineMark(Number(parts[0]), Number(parts[1]), Number(parts[2]));
            }
        }

        private static IEnumerable<PositionSnapshot> ReadPositions(string path)
        {
            string[] lines = File.ReadAllLines(path);
            for (int i = 1; i < lines.Length; i++)
            {
                List<string> parts = ParseCsvLine(lines[i]);
                yield return new PositionSnapshot(Number(parts[3]), Number(parts[4]), Number(parts[5]));
            }
        }

        private static double Number(string value)
        {
            return double.Parse(value, CultureInfo.InvariantCulture);
        }

        private static List<string> ParseCsvLine(string line)
        {
            var values = new List<string>();
            var current = new System.Text.StringBuilder();
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (quoted)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else if (c == '"')
                    {
                        quoted = false;
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else if (c == ',')
                {
                    values.Add(current.ToString());
                    current.Length = 0;
                }
                else if (c == '"')
                {
                    quoted = true;
                }
                else
                {
                    current.Append(c);
                }
            }

            values.Add(current.ToString());
            return values;
        }
    }
}
