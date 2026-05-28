using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using endfield_player_position_display.Models;

namespace endfield_player_position_display.Services
{
    public sealed class PositionCaptureRecorder : IDisposable
    {
        private const string Header = "timestamp,label,event,x,y,z,dtSeconds,dx,dy,dz,planarDistance,distance3d,planarSpeed,speed3d";
        private readonly string captureDirectory;
        private StreamWriter writer;
        private StreamWriter detectionWriter;
        private PositionSnapshot previousPosition;
        private DateTimeOffset? previousTimestamp;
        private string pendingEvent;

        public PositionCaptureRecorder(string baseDirectory)
        {
            captureDirectory = Path.Combine(baseDirectory, "captures");
            CurrentLabel = "未标注";
        }

        public bool IsRecording { get; private set; }
        public string CurrentLabel { get; private set; }
        public string CurrentFilePath { get; private set; }
        public string CurrentSessionDirectory { get; private set; }
        public int SampleCount { get; private set; }

        public void Start(DateTimeOffset timestamp)
        {
            Stop();
            Directory.CreateDirectory(captureDirectory);
            string sessionName = "zipline-" + timestamp.ToLocalTime().ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            CurrentSessionDirectory = Path.Combine(captureDirectory, sessionName);
            Directory.CreateDirectory(CurrentSessionDirectory);
            CurrentFilePath = Path.Combine(CurrentSessionDirectory, "positions.csv");
            writer = new StreamWriter(CurrentFilePath, false, new UTF8Encoding(true));
            writer.WriteLine(Header);
            detectionWriter = new StreamWriter(Path.Combine(CurrentSessionDirectory, "detections.csv"), false, new UTF8Encoding(true));
            detectionWriter.WriteLine("timestamp,order,x,y,z,direction,distance,heightOffset,connectToPrevious");
            IsRecording = true;
            SampleCount = 0;
            previousPosition = null;
            previousTimestamp = null;
            pendingEvent = "start";
        }

        public void Stop()
        {
            if (writer != null)
            {
                writer.Flush();
                writer.Dispose();
                writer = null;
            }

            if (detectionWriter != null)
            {
                detectionWriter.Flush();
                detectionWriter.Dispose();
                detectionWriter = null;
            }

            IsRecording = false;
            previousPosition = null;
            previousTimestamp = null;
            pendingEvent = null;
        }

        public void SetLabel(string label)
        {
            CurrentLabel = string.IsNullOrWhiteSpace(label) ? "未标注" : label;
            MarkEvent("label:" + CurrentLabel);
        }

        public void MarkEvent(string eventName)
        {
            string actual = string.IsNullOrWhiteSpace(eventName) ? string.Empty : eventName;
            if (string.IsNullOrEmpty(pendingEvent))
            {
                pendingEvent = actual;
            }
            else if (!string.IsNullOrEmpty(actual))
            {
                pendingEvent += "|" + actual;
            }
        }

        public void Record(PositionSnapshot position, DateTimeOffset timestamp)
        {
            if (!IsRecording || position == null || writer == null)
            {
                return;
            }

            double dt = 0;
            double dx = 0;
            double dy = 0;
            double dz = 0;
            if (previousPosition != null && previousTimestamp.HasValue)
            {
                dt = Math.Max(0, (timestamp - previousTimestamp.Value).TotalSeconds);
                dx = position.X - previousPosition.X;
                dy = position.Y - previousPosition.Y;
                dz = position.Z - previousPosition.Z;
            }

            double planarDistance = Math.Sqrt(dx * dx + dz * dz);
            double distance3d = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            double planarSpeed = dt > 0 ? planarDistance / dt : 0;
            double speed3d = dt > 0 ? distance3d / dt : 0;

            writer.WriteLine(string.Join(",", new[]
            {
                Csv(timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)),
                Csv(CurrentLabel),
                Csv(pendingEvent ?? string.Empty),
                Number(position.X),
                Number(position.Y),
                Number(position.Z),
                Number(dt),
                Number(dx),
                Number(dy),
                Number(dz),
                Number(planarDistance),
                Number(distance3d),
                Number(planarSpeed),
                Number(speed3d)
            }));
            writer.Flush();

            SampleCount++;
            previousPosition = position;
            previousTimestamp = timestamp;
            pendingEvent = null;
        }

        public void WriteMarks(IEnumerable<ZiplineMark> marks)
        {
            if (string.IsNullOrWhiteSpace(CurrentSessionDirectory) || marks == null)
            {
                return;
            }

            using (var markWriter = new StreamWriter(Path.Combine(CurrentSessionDirectory, "marks.csv"), false, new UTF8Encoding(true)))
            {
                markWriter.WriteLine("x,y,z");
                foreach (ZiplineMark mark in marks)
                {
                    markWriter.WriteLine(string.Join(",", new[]
                    {
                        Number(mark.X),
                        Number(mark.Y),
                        Number(mark.Z)
                    }));
                }
            }
        }

        public void WriteDetection(DetectedZiplineStop stop, DateTimeOffset timestamp)
        {
            if (!IsRecording || detectionWriter == null || stop == null)
            {
                return;
            }

            detectionWriter.WriteLine(string.Join(",", new[]
            {
                Csv(timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)),
                Number(stop.Order),
                Number(stop.X),
                Number(stop.Y),
                Number(stop.Z),
                Csv(stop.Direction),
                Number(stop.Distance),
                Number(stop.HeightOffset),
                stop.ConnectToPrevious ? "true" : "false"
            }));
            detectionWriter.Flush();
        }

        public void Dispose()
        {
            Stop();
        }

        private static string Number(double value)
        {
            return value.ToString("0.########", CultureInfo.InvariantCulture);
        }

        private static string Csv(string value)
        {
            string actual = value ?? string.Empty;
            return "\"" + actual.Replace("\"", "\"\"") + "\"";
        }
    }
}
