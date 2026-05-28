using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using endfield_player_position_display.Models;

namespace endfield_player_position_display.Services
{
    public static class ZiplineMotionAnalyzer
    {
        private const double StopSpeed = 0.2;
        private const double MovingSpeed = 2.0;
        private const double MinTurnDegrees = 20.0;
        private const double MergeDistance = 4.0;
        public const double MaxDistanceBetweenZiplinePoints = 120.0;

        public static ZiplineMotionAnalysisResult AnalyzeCsv(string path)
        {
            return Analyze(ReadCsv(path));
        }

        public static ZiplineMotionAnalysisResult Analyze(IReadOnlyList<ZiplineMotionSample> samples)
        {
            if (samples == null || samples.Count == 0)
            {
                return new ZiplineMotionAnalysisResult(new List<ZiplineMotionPoint>(), new List<ZiplineMotionSegment>());
            }

            samples = TrimToZiplineRange(samples);
            List<Run> runs = BuildRuns(samples);
            List<ZiplineMotionSegment> movingSegments = BuildMovingSegments(samples, runs);
            List<CandidatePoint> candidates = new List<CandidatePoint>();

            AddStopCandidates(samples, runs, candidates);
            AddTurnCandidates(samples, runs, movingSegments, candidates);
            AddTerminalCandidate(samples, runs, candidates);

            List<ZiplineMotionPoint> points = MergeCandidates(candidates);
            AddDistanceCandidates(samples, points, candidates);
            points = MergeCandidates(candidates);
            return new ZiplineMotionAnalysisResult(points, movingSegments);
        }

        private static IReadOnlyList<ZiplineMotionSample> TrimToZiplineRange(IReadOnlyList<ZiplineMotionSample> samples)
        {
            int start = -1;
            for (int i = 0; i < samples.Count; i++)
            {
                if (samples[i].EventName.IndexOf("manual:on_zipline", StringComparison.Ordinal) >= 0)
                {
                    start = i;
                    break;
                }
            }

            if (start < 0)
            {
                return samples;
            }

            int end = samples.Count - 1;
            for (int i = start; i < samples.Count; i++)
            {
                if (samples[i].EventName.IndexOf("manual:off_zipline", StringComparison.Ordinal) >= 0)
                {
                    end = i;
                    break;
                }
            }

            var result = new List<ZiplineMotionSample>();
            for (int i = start; i <= end; i++)
            {
                ZiplineMotionSample sample = samples[i];
                result.Add(new ZiplineMotionSample(
                    result.Count,
                    sample.Timestamp,
                    sample.Label,
                    sample.EventName,
                    sample.X,
                    sample.Y,
                    sample.Z,
                    sample.PlanarSpeed));
            }

            return result;
        }

        private static IReadOnlyList<ZiplineMotionSample> ReadCsv(string path)
        {
            var result = new List<ZiplineMotionSample>();
            using (var reader = new StreamReader(path, true))
            {
                string header = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(header))
                {
                    return result;
                }

                Dictionary<string, int> columns = ParseCsvLine(header)
                    .Select((name, index) => new { name, index })
                    .ToDictionary(x => x.name, x => x.index, StringComparer.Ordinal);

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    List<string> values = ParseCsvLine(line);
                    int index = result.Count;
                    result.Add(new ZiplineMotionSample(
                        index,
                        DateTimeOffset.Parse(Get(values, columns, "timestamp"), CultureInfo.InvariantCulture),
                        Get(values, columns, "label"),
                        Get(values, columns, "event"),
                        Number(Get(values, columns, "x")),
                        Number(Get(values, columns, "y")),
                        Number(Get(values, columns, "z")),
                        Number(Get(values, columns, "planarSpeed"))));
                }
            }

            return result;
        }

        private static void AddStopCandidates(IReadOnlyList<ZiplineMotionSample> samples, List<Run> runs, List<CandidatePoint> candidates)
        {
            foreach (Run run in runs)
            {
                if (run.State != MotionState.Stop || run.Count < 2)
                {
                    continue;
                }

                int start = Math.Max(0, run.Start - 1);
                int end = Math.Min(samples.Count - 1, run.End + 1);
                List<ZiplineMotionSample> local = samples
                    .Where(x => x.Index >= start && x.Index <= end && x.PlanarSpeed < MovingSpeed)
                    .ToList();
                if (local.Count < 2)
                {
                    local = samples.Where(x => x.Index >= run.Start && x.Index <= run.End).ToList();
                }

                candidates.Add(new CandidatePoint(
                    Average(local, x => x.X),
                    Average(local, x => x.Y),
                    Average(local, x => x.Z),
                    "stop",
                    Math.Min(0.98, 0.75 + run.Count * 0.05),
                    run.Start,
                    run.End));
            }
        }

        private static void AddTurnCandidates(
            IReadOnlyList<ZiplineMotionSample> samples,
            List<Run> runs,
            List<ZiplineMotionSegment> movingSegments,
            List<CandidatePoint> candidates)
        {
            for (int i = 0; i < movingSegments.Count - 1; i++)
            {
                ZiplineMotionSegment previous = movingSegments[i];
                ZiplineMotionSegment next = movingSegments[i + 1];
                double turn = Math.Abs(AngleDifferenceDegrees(previous.HeadingDegrees, next.HeadingDegrees));
                if (turn < MinTurnDegrees)
                {
                    continue;
                }

                int betweenStart = previous.EndIndex + 1;
                int betweenEnd = next.StartIndex - 1;
                int supportIndex = FindManualNodeIndex(samples, betweenStart, betweenEnd);
                bool hasStopBetween = runs.Any(run => run.State == MotionState.Stop && run.Start <= betweenEnd && run.End >= betweenStart);
                bool hasSlowBetween = runs.Any(run => run.State == MotionState.Slow && run.Start <= betweenEnd && run.End >= betweenStart);

                Line lineA = FitLine(samples, previous.StartIndex, previous.EndIndex);
                Line lineB = FitLine(samples, next.StartIndex, next.EndIndex);
                double x;
                double z;
                bool hasIntersection = TryIntersect(lineA, lineB, out x, out z);
                if (!hasIntersection)
                {
                    int pivot = supportIndex >= 0 ? supportIndex : Math.Max(previous.EndIndex, Math.Min(samples.Count - 1, next.StartIndex));
                    x = samples[pivot].X;
                    z = samples[pivot].Z;
                }

                int yIndex = supportIndex >= 0 ? supportIndex : NearestIndex(samples, x, z, previous.EndIndex, next.StartIndex);
                double confidence = hasStopBetween ? 0.72 : hasSlowBetween ? 0.62 : 0.46;
                if (supportIndex >= 0)
                {
                    confidence += 0.12;
                }

                candidates.Add(new CandidatePoint(
                    x,
                    samples[yIndex].Y,
                    z,
                    "turn",
                    Math.Min(0.86, confidence),
                    Math.Min(previous.EndIndex, next.StartIndex),
                    Math.Max(previous.EndIndex, next.StartIndex)));
            }
        }

        private static void AddTerminalCandidate(IReadOnlyList<ZiplineMotionSample> samples, List<Run> runs, List<CandidatePoint> candidates)
        {
            if (samples.Count < 3)
            {
                return;
            }

            ZiplineMotionSample last = samples[samples.Count - 1];
            if (last.PlanarSpeed >= MovingSpeed)
            {
                return;
            }

            bool hasEarlierMove = runs.Any(run => run.State == MotionState.Move && run.End < samples.Count - 1);
            if (!hasEarlierMove)
            {
                return;
            }

            int start = Math.Max(0, samples.Count - 3);
            List<ZiplineMotionSample> local = samples
                .Where(sample => sample.Index >= start && sample.PlanarSpeed < MovingSpeed)
                .ToList();
            if (local.Count == 0)
            {
                local.Add(last);
            }

            candidates.Add(new CandidatePoint(
                Average(local, sample => sample.X),
                Average(local, sample => sample.Y),
                Average(local, sample => sample.Z),
                "terminal",
                0.85,
                last.Index,
                last.Index));
        }

        private static void AddDistanceCandidates(
            IReadOnlyList<ZiplineMotionSample> samples,
            List<ZiplineMotionPoint> points,
            List<CandidatePoint> candidates)
        {
            for (int i = 0; i < points.Count - 1; i++)
            {
                ZiplineMotionPoint previous = points[i];
                ZiplineMotionPoint next = points[i + 1];
                double distance = PlanarDistance(previous.X, previous.Z, next.X, next.Z);
                int insertCount = (int)Math.Floor(distance / MaxDistanceBetweenZiplinePoints);
                if (insertCount <= 0)
                {
                    continue;
                }

                for (int insert = 1; insert <= insertCount; insert++)
                {
                    double ratio = (double)insert / (insertCount + 1);
                    CandidatePoint point = InterpolateCandidate(samples, previous, next, ratio);
                    candidates.Add(point);
                }
            }
        }

        private static CandidatePoint InterpolateCandidate(
            IReadOnlyList<ZiplineMotionSample> samples,
            ZiplineMotionPoint previous,
            ZiplineMotionPoint next,
            double ratio)
        {
            double x = previous.X + (next.X - previous.X) * ratio;
            double z = previous.Z + (next.Z - previous.Z) * ratio;
            int yIndex = NearestIndex(samples, x, z, previous.EndIndex, next.StartIndex);
            double y = samples[yIndex].Y;
            int index = (int)Math.Round(previous.EndIndex + (next.StartIndex - previous.EndIndex) * ratio);
            return new CandidatePoint(x, y, z, "distance", 0.40, index, index);
        }

        private static List<ZiplineMotionPoint> MergeCandidates(List<CandidatePoint> candidates)
        {
            candidates.Sort((a, b) => a.StartIndex.CompareTo(b.StartIndex));
            var merged = new List<CandidatePoint>();
            foreach (CandidatePoint candidate in candidates)
            {
                CandidatePoint existing = merged.FirstOrDefault(x =>
                    PlanarDistance(x.X, x.Z, candidate.X, candidate.Z) <= MergeDistance);
                if (existing == null)
                {
                    merged.Add(candidate);
                    continue;
                }

                if (candidate.Confidence > existing.Confidence)
                {
                    int index = merged.IndexOf(existing);
                    merged[index] = candidate;
                }
            }

            var result = new List<ZiplineMotionPoint>();
            for (int i = 0; i < merged.Count; i++)
            {
                CandidatePoint point = merged[i];
                result.Add(new ZiplineMotionPoint(
                    i + 1,
                    point.X,
                    point.Y,
                    point.Z,
                    point.Source,
                    point.Confidence,
                    point.StartIndex,
                    point.EndIndex));
            }

            return result;
        }

        private static List<Run> BuildRuns(IReadOnlyList<ZiplineMotionSample> samples)
        {
            var runs = new List<Run>();
            MotionState state = GetState(samples[0]);
            int start = 0;
            for (int i = 1; i < samples.Count; i++)
            {
                MotionState next = GetState(samples[i]);
                if (next == state)
                {
                    continue;
                }

                runs.Add(new Run(state, start, i - 1));
                start = i;
                state = next;
            }

            runs.Add(new Run(state, start, samples.Count - 1));
            return runs;
        }

        private static List<ZiplineMotionSegment> BuildMovingSegments(IReadOnlyList<ZiplineMotionSample> samples, List<Run> runs)
        {
            var result = new List<ZiplineMotionSegment>();
            foreach (Run run in runs)
            {
                if (run.State != MotionState.Move || run.Count < 3)
                {
                    continue;
                }

                AddStraightSegments(samples, run.Start, run.End, result);
            }

            return result;
        }

        private static void AddStraightSegments(
            IReadOnlyList<ZiplineMotionSample> samples,
            int runStart,
            int runEnd,
            List<ZiplineMotionSegment> result)
        {
            int segmentStart = runStart;
            double? previousHeading = null;
            for (int i = runStart + 1; i <= runEnd; i++)
            {
                double dx = samples[i].X - samples[i - 1].X;
                double dz = samples[i].Z - samples[i - 1].Z;
                double distance = Math.Sqrt(dx * dx + dz * dz);
                if (distance < 1.0)
                {
                    continue;
                }

                double heading = Math.Atan2(dz, dx) * 180.0 / Math.PI;
                if (previousHeading.HasValue && Math.Abs(AngleDifferenceDegrees(heading, previousHeading.Value)) >= MinTurnDegrees)
                {
                    AddMovingSegment(samples, segmentStart, i - 1, result);
                    segmentStart = i;
                }

                previousHeading = heading;
            }

            AddMovingSegment(samples, segmentStart, runEnd, result);
        }

        private static void AddMovingSegment(
            IReadOnlyList<ZiplineMotionSample> samples,
            int start,
            int end,
            List<ZiplineMotionSegment> result)
        {
            if (end - start + 1 < 3)
            {
                return;
            }

            double dx = samples[end].X - samples[start].X;
            double dz = samples[end].Z - samples[start].Z;
            double distance = Math.Sqrt(dx * dx + dz * dz);
            if (distance < 5)
            {
                return;
            }

            double heading = Math.Atan2(dz, dx) * 180.0 / Math.PI;
            double medianSpeed = Median(samples.Where(sample => sample.Index >= start && sample.Index <= end).Select(sample => sample.PlanarSpeed).ToList());
            result.Add(new ZiplineMotionSegment(start, end, heading, distance, medianSpeed));
        }

        private static MotionState GetState(ZiplineMotionSample sample)
        {
            if (sample.PlanarSpeed < StopSpeed)
            {
                return MotionState.Stop;
            }

            return sample.PlanarSpeed < MovingSpeed ? MotionState.Slow : MotionState.Move;
        }

        private static Line FitLine(IReadOnlyList<ZiplineMotionSample> samples, int start, int end)
        {
            ZiplineMotionSample first = samples[start];
            ZiplineMotionSample last = samples[end];
            return new Line(first.X, first.Z, last.X - first.X, last.Z - first.Z);
        }

        private static bool TryIntersect(Line a, Line b, out double x, out double z)
        {
            double denominator = a.Dx * b.Dz - a.Dz * b.Dx;
            if (Math.Abs(denominator) < 0.000001)
            {
                x = 0;
                z = 0;
                return false;
            }

            double bx = b.X - a.X;
            double bz = b.Z - a.Z;
            double t = (bx * b.Dz - bz * b.Dx) / denominator;
            x = a.X + t * a.Dx;
            z = a.Z + t * a.Dz;
            return true;
        }

        private static int FindManualNodeIndex(IReadOnlyList<ZiplineMotionSample> samples, int start, int end)
        {
            if (start > end)
            {
                return -1;
            }

            int actualStart = Math.Max(0, start - 2);
            int actualEnd = Math.Min(samples.Count - 1, end + 2);
            for (int i = actualStart; i <= actualEnd; i++)
            {
                if (samples[i].EventName.IndexOf("manual:node", StringComparison.Ordinal) >= 0)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int NearestIndex(IReadOnlyList<ZiplineMotionSample> samples, double x, double z, int start, int end)
        {
            int actualStart = Math.Max(0, Math.Min(start, end) - 2);
            int actualEnd = Math.Min(samples.Count - 1, Math.Max(start, end) + 2);
            int best = actualStart;
            double bestDistance = double.MaxValue;
            for (int i = actualStart; i <= actualEnd; i++)
            {
                double distance = PlanarDistance(samples[i].X, samples[i].Z, x, z);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            return best;
        }

        private static double AngleDifferenceDegrees(double a, double b)
        {
            double diff = (a - b + 180.0) % 360.0 - 180.0;
            if (diff < -180.0)
            {
                diff += 360.0;
            }

            return diff;
        }

        private static double PlanarDistance(double ax, double az, double bx, double bz)
        {
            double dx = ax - bx;
            double dz = az - bz;
            return Math.Sqrt(dx * dx + dz * dz);
        }

        private static double Average(IEnumerable<ZiplineMotionSample> samples, Func<ZiplineMotionSample, double> selector)
        {
            List<double> values = samples.Select(selector).ToList();
            return values.Count == 0 ? 0 : values.Average();
        }

        private static double Median(List<double> values)
        {
            if (values.Count == 0)
            {
                return 0;
            }

            values.Sort();
            int middle = values.Count / 2;
            if (values.Count % 2 == 1)
            {
                return values[middle];
            }

            return (values[middle - 1] + values[middle]) / 2.0;
        }

        private static double Number(string value)
        {
            return double.Parse(value, CultureInfo.InvariantCulture);
        }

        private static string Get(List<string> values, Dictionary<string, int> columns, string name)
        {
            int index;
            if (!columns.TryGetValue(name, out index) || index < 0 || index >= values.Count)
            {
                return string.Empty;
            }

            return values[index];
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

        private enum MotionState
        {
            Stop,
            Slow,
            Move
        }

        private sealed class Run
        {
            public Run(MotionState state, int start, int end)
            {
                State = state;
                Start = start;
                End = end;
            }

            public MotionState State { get; }
            public int Start { get; }
            public int End { get; }
            public int Count { get { return End - Start + 1; } }
        }

        private sealed class CandidatePoint
        {
            public CandidatePoint(double x, double y, double z, string source, double confidence, int startIndex, int endIndex)
            {
                X = x;
                Y = y;
                Z = z;
                Source = source;
                Confidence = confidence;
                StartIndex = startIndex;
                EndIndex = endIndex;
            }

            public double X { get; }
            public double Y { get; }
            public double Z { get; }
            public string Source { get; }
            public double Confidence { get; }
            public int StartIndex { get; }
            public int EndIndex { get; }
        }

        private sealed class Line
        {
            public Line(double x, double z, double dx, double dz)
            {
                X = x;
                Z = z;
                Dx = dx;
                Dz = dz;
            }

            public double X { get; }
            public double Z { get; }
            public double Dx { get; }
            public double Dz { get; }
        }
    }
}
