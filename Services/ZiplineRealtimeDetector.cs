using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using endfield_player_position_display.Models;

namespace endfield_player_position_display.Services
{
    public sealed class ZiplineRealtimeDetector
    {
        public const double StandingHeightOffset = 3.5;
        public const double StandingHeightTolerance = 0.9;
        public const double EnterDistance = 4.0;
        public const double LeaveDistance = 10.0;
        private const int StableSampleCount = 3;
        private const double StableRadius = 1.2;
        private const double FastConfirmDistance = 1.0;
        private const double FastConfirmSpeed = 5.0;
        private const double GroundEvidenceDistance = 6.0;
        private const double GroundEvidenceHeightOffset = 1.0;

        private readonly List<Candidate> candidates;
        private readonly Queue<PositionSnapshot> recentPositions = new Queue<PositionSnapshot>();
        private Candidate currentCandidate;
        private Candidate lastDetectedCandidate;
        private int detectedCount;
        private bool breakBeforeNextDetection;

        public ZiplineRealtimeDetector(IEnumerable<ZiplineMark> marks)
        {
            candidates = (marks ?? new ZiplineMark[0])
                .SelectMany(CreateCandidates)
                .ToList();
        }

        public IReadOnlyList<DetectedZiplineStop> DetectedStops { get { return detectedStops; } }
        private readonly List<DetectedZiplineStop> detectedStops = new List<DetectedZiplineStop>();

        public ZiplineRealtimeDetection Update(PositionSnapshot position)
        {
            if (position == null || candidates.Count == 0)
            {
                return ZiplineRealtimeDetection.None();
            }

            AddRecentPosition(position);
            Candidate nearest = FindNearestCandidate(position);
            if (nearest == null)
            {
                currentCandidate = null;
                if (lastDetectedCandidate != null && DistanceToCandidate(position, lastDetectedCandidate) > LeaveDistance)
                {
                    lastDetectedCandidate = null;
                }

                return ZiplineRealtimeDetection.None();
            }

            currentCandidate = nearest;
            UpdateBreakEvidence(position, nearest);
            if (lastDetectedCandidate != null)
            {
                if (ReferenceEquals(nearest, lastDetectedCandidate) || DistanceToCandidate(position, lastDetectedCandidate) <= LeaveDistance)
                {
                    return ZiplineRealtimeDetection.None();
                }

                lastDetectedCandidate = null;
            }

            if (!IsHeightMatched(position, nearest) || !CanConfirm(position, nearest))
            {
                return ZiplineRealtimeDetection.None();
            }

            DetectedZiplineStop stop = new DetectedZiplineStop(
                ++detectedCount,
                (int)Math.Floor(nearest.CenterX),
                nearest.Mark.Y,
                (int)Math.Floor(nearest.CenterZ),
                nearest.Direction,
                DistanceToCandidate(position, nearest),
                position.Y - nearest.Mark.Y,
                detectedCount == 1 || !breakBeforeNextDetection);
            detectedStops.Add(stop);
            breakBeforeNextDetection = false;
            lastDetectedCandidate = nearest;
            return ZiplineRealtimeDetection.Found(stop);
        }

        public string GetResultText()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var lines = new List<string>();
            foreach (DetectedZiplineStop stop in detectedStops)
            {
                string id = stop.X + "," + stop.Z;
                if (!seen.Add(id))
                {
                    continue;
                }

                lines.Add(string.Format(CultureInfo.InvariantCulture, "{0}. ({1},{2},{3},{4})", lines.Count + 1, stop.X, stop.Y, stop.Z, stop.Direction));
            }

            return string.Join(Environment.NewLine, lines);
        }

        public string GetResultJson()
        {
            return ZiplineCollectionExporter.ExportMarksJson(detectedStops);
        }

        public string GetRouteCollectionJson()
        {
            return ZiplineCollectionExporter.ExportRoutesJson(detectedStops);
        }

        private void AddRecentPosition(PositionSnapshot position)
        {
            recentPositions.Enqueue(position);
            while (recentPositions.Count > StableSampleCount)
            {
                recentPositions.Dequeue();
            }
        }

        private bool IsStable()
        {
            if (recentPositions.Count < StableSampleCount)
            {
                return false;
            }

            PositionSnapshot first = recentPositions.Peek();
            foreach (PositionSnapshot position in recentPositions)
            {
                double dx = position.X - first.X;
                double dz = position.Z - first.Z;
                if (Math.Sqrt(dx * dx + dz * dz) > StableRadius)
                {
                    return false;
                }
            }

            return true;
        }

        private bool CanConfirm(PositionSnapshot position, Candidate candidate)
        {
            return IsStable() || (DistanceToCandidate(position, candidate) <= FastConfirmDistance && EstimateRecentPlanarSpeed() <= FastConfirmSpeed);
        }

        public ZiplineRealtimeDetection AddManual(PositionSnapshot position)
        {
            if (position == null || candidates.Count == 0)
            {
                return ZiplineRealtimeDetection.None();
            }

            Candidate nearest = FindNearestCandidate(position);
            if (nearest == null)
            {
                return ZiplineRealtimeDetection.None();
            }

            UpdateBreakEvidence(position, nearest);
            DetectedZiplineStop stop = new DetectedZiplineStop(
                ++detectedCount,
                (int)Math.Floor(nearest.CenterX),
                nearest.Mark.Y,
                (int)Math.Floor(nearest.CenterZ),
                nearest.Direction,
                DistanceToCandidate(position, nearest),
                position.Y - nearest.Mark.Y,
                detectedCount == 1 || !breakBeforeNextDetection);
            detectedStops.Add(stop);
            breakBeforeNextDetection = false;
            lastDetectedCandidate = nearest;
            return ZiplineRealtimeDetection.Found(stop);
        }

        private double EstimateRecentPlanarSpeed()
        {
            if (recentPositions.Count < 2)
            {
                return double.MaxValue;
            }

            PositionSnapshot previous = null;
            PositionSnapshot current = null;
            foreach (PositionSnapshot position in recentPositions)
            {
                previous = current;
                current = position;
            }

            if (previous == null || current == null)
            {
                return double.MaxValue;
            }

            double dx = current.X - previous.X;
            double dz = current.Z - previous.Z;
            return Math.Sqrt(dx * dx + dz * dz);
        }

        private Candidate FindNearestCandidate(PositionSnapshot position)
        {
            Candidate best = null;
            double bestDistance = double.MaxValue;
            foreach (Candidate candidate in candidates)
            {
                double distance = DistanceToCandidate(position, candidate);
                if (distance <= EnterDistance && distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private static bool IsHeightMatched(PositionSnapshot position, Candidate candidate)
        {
            double heightOffset = position.Y - candidate.Mark.Y;
            return Math.Abs(heightOffset - StandingHeightOffset) <= StandingHeightTolerance;
        }

        private void UpdateBreakEvidence(PositionSnapshot position, Candidate candidate)
        {
            if (detectedCount == 0 || ReferenceEquals(candidate, lastDetectedCandidate))
            {
                return;
            }

            double heightOffset = position.Y - candidate.Mark.Y;
            if (DistanceToCandidate(position, candidate) <= GroundEvidenceDistance && heightOffset < GroundEvidenceHeightOffset)
            {
                breakBeforeNextDetection = true;
            }
        }

        private static double DistanceToCandidate(PositionSnapshot position, Candidate candidate)
        {
            double dx = position.X - candidate.CenterX;
            double dz = position.Z - candidate.CenterZ;
            return Math.Sqrt(dx * dx + dz * dz);
        }

        private static IEnumerable<Candidate> CreateCandidates(ZiplineMark mark)
        {
            yield return new Candidate(mark, mark.X + 1, mark.Z + 1, "北");
            yield return new Candidate(mark, mark.X - 1, mark.Z + 1, "西");
            yield return new Candidate(mark, mark.X - 1, mark.Z - 1, "南");
            yield return new Candidate(mark, mark.X + 1, mark.Z - 1, "东");
        }

        private sealed class Candidate
        {
            public Candidate(ZiplineMark mark, double centerX, double centerZ, string direction)
            {
                Mark = mark;
                CenterX = centerX;
                CenterZ = centerZ;
                Direction = direction;
            }

            public ZiplineMark Mark { get; }
            public double CenterX { get; }
            public double CenterZ { get; }
            public string Direction { get; }
        }
    }
}
