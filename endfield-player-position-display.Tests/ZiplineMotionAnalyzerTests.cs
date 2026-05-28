using System;
using System.Collections.Generic;
using endfield_player_position_display.Models;
using endfield_player_position_display.Services;

namespace endfield_player_position_display.Tests
{
    internal static class ZiplineMotionAnalyzerTests
    {
        public static void AnalyzeDetectsStopPointsAndTurnIntersections()
        {
            var samples = new List<ZiplineMotionSample>();
            AddStop(samples, 0, 0, 0, 0, 3);
            AddLine(samples, 0, 0, 10, 0, 0, 3);
            AddLine(samples, 10, 0, 10, 10, 10, 3);
            AddStop(samples, 10, 0, 10, 10, 3);

            ZiplineMotionAnalysisResult result = ZiplineMotionAnalyzer.Analyze(samples);

            TestAssert.AreEqual(3, result.Points.Count);
            TestAssert.AreEqual("stop", result.Points[0].Source);
            TestAssert.AreNear(0, result.Points[0].X, 0.01);
            TestAssert.AreNear(0, result.Points[0].Z, 0.01);
            TestAssert.AreEqual("turn", result.Points[1].Source);
            TestAssert.AreNear(10, result.Points[1].X, 0.01);
            TestAssert.AreNear(0, result.Points[1].Z, 0.01);
            TestAssert.AreEqual("stop", result.Points[2].Source);
            TestAssert.AreNear(10, result.Points[2].X, 0.01);
            TestAssert.AreNear(10, result.Points[2].Z, 0.01);
        }

        public static void AnalyzeInfersZiplineRangeWithoutManualStartOrEnd()
        {
            var samples = new List<ZiplineMotionSample>();
            AddJitter(samples, -50, -50, 0, 8);
            AddStop(samples, 0, 0, 0, 10, 3);
            AddLine(samples, 0, 0, 10, 0, 20, 3);
            AddLine(samples, 10, 0, 10, 10, 30, 3);
            AddStop(samples, 10, 0, 10, 40, 3);
            AddJitter(samples, 50, 50, 50, 8);

            ZiplineMotionAnalysisResult result = ZiplineMotionAnalyzer.Analyze(samples);

            TestAssert.AreEqual(3, result.Points.Count);
            TestAssert.AreNear(0, result.Points[0].X, 0.01);
            TestAssert.AreNear(10, result.Points[1].X, 0.01);
            TestAssert.AreNear(10, result.Points[2].Z, 0.01);
        }

        public static void AnalyzeRealCaptureFindsTenZiplinePoints()
        {
            string path = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..",
                "..",
                "..",
                "bin",
                "Debug",
                "captures",
                "zipline-20260526-230437.csv");

            if (!System.IO.File.Exists(path))
            {
                return;
            }

            ZiplineMotionAnalysisResult result = ZiplineMotionAnalyzer.AnalyzeCsv(path);

            TestAssert.AreEqual(10, result.Points.Count);
        }

        public static void AnalyzeNewRealCapturesUsesExpectedPointCounts()
        {
            AssertCapturePointCount("zipline-20260526-235833.csv", 5);
            AssertCapturePointCount("zipline-20260526-235952.csv", 4);
            AssertCapturePointCount("zipline-20260527-000057.csv", 5);
        }

        private static void AddStop(List<ZiplineMotionSample> samples, double x, double y, double z, int startSecond, int count)
        {
            for (int i = 0; i < count; i++)
            {
                samples.Add(Sample(samples.Count, startSecond + i, x, y, z, 0));
            }
        }

        private static void AssertCapturePointCount(string fileName, int expected)
        {
            string path = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..",
                "..",
                "..",
                "bin",
                "Debug",
                "captures",
                fileName);

            if (!System.IO.File.Exists(path))
            {
                return;
            }

            ZiplineMotionAnalysisResult result = ZiplineMotionAnalyzer.AnalyzeCsv(path);

            TestAssert.AreEqual(expected, result.Points.Count);
        }

        private static void AddLine(List<ZiplineMotionSample> samples, double startX, double startZ, double endX, double endZ, int startSecond, int count)
        {
            for (int i = 1; i <= count; i++)
            {
                double ratio = (double)i / count;
                double x = startX + (endX - startX) * ratio;
                double z = startZ + (endZ - startZ) * ratio;
                samples.Add(Sample(samples.Count, startSecond + i, x, 0, z, 10));
            }
        }

        private static void AddJitter(List<ZiplineMotionSample> samples, double startX, double startZ, int startSecond, int count)
        {
            for (int i = 0; i < count; i++)
            {
                double x = startX + (i % 3) * 2 - (i % 2) * 5;
                double z = startZ + (i % 4) * 3 - (i % 2) * 4;
                samples.Add(Sample(samples.Count, startSecond + i, x, 0, z, 6));
            }
        }

        private static ZiplineMotionSample Sample(int index, int second, double x, double y, double z, double speed)
        {
            return new ZiplineMotionSample(
                index,
                new DateTimeOffset(2026, 5, 26, 12, 0, second, TimeSpan.Zero),
                "滑索移动",
                string.Empty,
                x,
                y,
                z,
                speed);
        }
    }
}
