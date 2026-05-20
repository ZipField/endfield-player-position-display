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
                SklandSignerTests.CreateSignReturnsMd5OfHmacSha256Hex,
                SklandSignerTests.CreateSignIncludesQueryOrBodySegment,
                SklandApiParsingTests.ParseRoleBindingExtractsFirstEndfieldDefaultRole,
                SklandApiParsingTests.ParseRoleBindingThrowsChineseErrorWhenRoleMissing,
                SklandApiParsingTests.ParseWebSocketTokenExtractsDataToken,
                CoordinateFormatterTests.FormatPadsIntegerPartAndKeepsFiveFractionDigits,
                PositionWebSocketMessageTests.ParseMessageExtractsPositionFromType1012,
                PositionWebSocketMessageTests.ParseMessageExtractsRemoteCloseMessageFromType6,
                PositionWebSocketMessageTests.ParseMessageUsesChineseErrorForInvalidPayload
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
