using System;
using System.IO;
using System.Text;
using endfield_player_position_display.Models;
using endfield_player_position_display.Services;

namespace endfield_player_position_display.Tests
{
    internal static class PositionCaptureRecorderTests
    {
        public static void RecorderWritesUtf8BomCsvWithMovementMetrics()
        {
            string dir = Path.Combine(Path.GetTempPath(), "endfield-capture-tests-" + Guid.NewGuid().ToString("N"));
            var recorder = new PositionCaptureRecorder(dir);
            recorder.Start(new DateTimeOffset(2026, 5, 26, 12, 0, 0, TimeSpan.Zero));
            recorder.SetLabel("普通移动");

            recorder.Record(new PositionSnapshot(1, 2, 3), new DateTimeOffset(2026, 5, 26, 12, 0, 1, TimeSpan.Zero));
            recorder.MarkEvent("manual:on_zipline");
            recorder.SetLabel("滑索移动");
            recorder.Record(new PositionSnapshot(4, 6, 7), new DateTimeOffset(2026, 5, 26, 12, 0, 3, TimeSpan.Zero));
            recorder.Stop();

            byte[] bytes = File.ReadAllBytes(recorder.CurrentFilePath);
            TestAssert.AreEqual(0xEF, bytes[0]);
            TestAssert.AreEqual(0xBB, bytes[1]);
            TestAssert.AreEqual(0xBF, bytes[2]);

            string[] lines = File.ReadAllLines(recorder.CurrentFilePath, Encoding.UTF8);
            TestAssert.AreEqual("timestamp,label,event,x,y,z,dtSeconds,dx,dy,dz,planarDistance,distance3d,planarSpeed,speed3d", lines[0]);
            TestAssert.AreEqual(true, lines[1].Contains("\"普通移动\""));
            TestAssert.AreEqual(true, lines[1].Contains("label:普通移动"));
            TestAssert.AreEqual(true, lines[2].Contains("\"滑索移动\""));
            TestAssert.AreEqual(true, lines[2].Contains("manual:on_zipline|label:滑索移动"));
            TestAssert.AreEqual(true, lines[2].Contains("2,3,4,4,5,6.40312424,2.5"));
        }
    }
}
