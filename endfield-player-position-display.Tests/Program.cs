using System;
using System.Collections.Generic;

namespace endfield_player_position_display.Tests
{
    internal static class Program
    {
        private static int Main()
        {
            var tests = new List<Action>
            {
                TokenFileReaderTests.ReadTokenReadsTrimmedTokenFromBaseDirectory,
                TokenFileReaderTests.ReadTokenThrowsChineseErrorWhenFileMissing,
                TokenFileReaderTests.ReadTokenThrowsChineseErrorWhenFileBlank,
                SklandSignerTests.CreateHeaderJsonUsesAcceptedClientHeaderValues,
                SklandSignerTests.CreateSignReturnsMd5OfHmacSha256Hex,
                SklandSignerTests.CreateSignIncludesQueryOrBodySegment,
                SklandApiParsingTests.CreateSignedRequestTimestampUsesServerAcceptedClockSkew,
                SklandApiParsingTests.ParseRoleBindingExtractsFirstEndfieldDefaultRole,
                SklandApiParsingTests.ParseRoleBindingThrowsChineseErrorWhenRoleMissing,
                SklandApiParsingTests.ParseWebSocketTokenExtractsDataToken,
                CoordinateFormatterTests.FormatPadsIntegerPartAndKeepsFiveFractionDigits,
                PositionWebSocketMessageTests.ParseMessageExtractsPositionFromType1012,
                PositionWebSocketMessageTests.ParseMessageExtractsRemoteCloseMessageFromType6,
                PositionWebSocketMessageTests.ParseMessageExtractsMapIdFromType1012,
                PositionWebSocketMessageTests.ParseMessageAllowsMissingMapId,
                PositionWebSocketMessageTests.ParseMessageUsesChineseErrorForInvalidPayload,
                ZiplineMatcherTests.FindNearestMatchesBottomLeftAsNorth,
                ZiplineMatcherTests.FindNearestMatchesBottomRightAsWest,
                ZiplineMatcherTests.FindNearestMatchesTopRightAsSouth,
                ZiplineMatcherTests.FindNearestMatchesTopLeftAsEast,
                ZiplineMatcherTests.FindNearestReturnsNoMatchBeyondThreeMeters,
                ZiplineMatcherTests.FindNearestChoosesClosestCandidate,
                ZiplineMatcherTests.FormatsCopyValues
            };

            int passed = 0;
            foreach (Action test in tests)
            {
                try
                {
                    test();
                    Console.WriteLine("PASS " + test.Method.DeclaringType.Name + "." + test.Method.Name);
                    passed++;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("FAIL " + test.Method.DeclaringType.Name + "." + test.Method.Name);
                    Console.Error.WriteLine(ex);
                    return 1;
                }
            }

            Console.WriteLine(passed + " tests passed.");
            return 0;
        }
    }
}
