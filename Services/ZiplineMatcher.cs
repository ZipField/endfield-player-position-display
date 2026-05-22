using System;
using System.Collections.Generic;
using endfield_player_position_display.Models;

namespace endfield_player_position_display.Services
{
    public static class ZiplineMatcher
    {
        private const double MaxDistance = 3.0;

        public static ZiplineLookupResult FindNearest(PositionSnapshot player, IEnumerable<ZiplineMark> marks)
        {
            if (player == null || marks == null)
            {
                return ZiplineLookupResult.NotFound();
            }

            Candidate best = null;
            foreach (ZiplineMark mark in marks)
            {
                foreach (Candidate candidate in CreateCandidates(mark))
                {
                    double dx = player.X - candidate.CenterX;
                    double dz = player.Z - candidate.CenterZ;
                    double distance = Math.Sqrt(dx * dx + dz * dz);
                    if (distance <= MaxDistance && (best == null || distance < best.Distance))
                    {
                        candidate.Distance = distance;
                        best = candidate;
                    }
                }
            }

            if (best == null)
            {
                return ZiplineLookupResult.NotFound();
            }

            return ZiplineLookupResult.FoundResult(
                (int)Math.Floor(best.CenterX),
                best.Mark.Y,
                (int)Math.Floor(best.CenterZ),
                best.Direction);
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
            public double Distance { get; set; }
        }
    }
}
