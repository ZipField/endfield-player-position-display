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
                TokenFileReaderTests.ReadTokensKeepsDuplicatesWhenRequested,
                TokenFileReaderTests.ReadTokensRemovesDuplicatesWhenRequested,
                TokenFileReaderTests.ReadTokenThrowsChineseErrorWhenFileMissing,
                TokenFileReaderTests.ReadTokenThrowsChineseErrorWhenFileBlank,
                SklandSignerTests.CreateHeaderJsonUsesAcceptedClientHeaderValues,
                SklandSignerTests.CreateSignReturnsMd5OfHmacSha256Hex,
                SklandSignerTests.CreateSignIncludesQueryOrBodySegment,
                SklandApiParsingTests.CreateSignedRequestTimestampUsesServerAcceptedClockSkew,
                SklandApiParsingTests.CreateSignedRequestTimestampAppliesNetworkTimeOffset,
                NetworkTimeServiceTests.CalculateOffsetUsesNetworkDateMinusLocalDate,
                SklandApiParsingTests.ParseRoleBindingExtractsFirstEndfieldDefaultRole,
                SklandApiParsingTests.ParseRoleBindingsExtractsNicknameAndChannelName,
                SklandApiParsingTests.ParseRoleBindingThrowsChineseErrorWhenRoleMissing,
                SklandApiParsingTests.ParseWebSocketTokenExtractsDataToken,
                SklandApiParsingTests.ParseZiplineMarksFiltersSupportedTemplates,
                SklandApiParsingTests.ParseZiplineMarksThrowsChineseErrorForBadResponse,
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
                ZiplineMatcherTests.FormatsCopyValues,
                ClipboardTextServiceTests.TrySetTextRetriesWhenClipboardIsBusy,
                ClipboardTextServiceTests.TrySetTextReturnsErrorWhenClipboardStaysBusy,
                DpiCoordinateConverterTests.FromDevicePixelsConvertsRectToDips,
                FollowWindowPlacementTests.CalculatePlacesAllEightDirections,
                FollowWindowPlacementTests.CalculateAppliesDirectionalOffsetsTowardInside,
                FollowWindowPlacementTests.CalculateAllowsNegativeCenterAxisOffsets,
                PositionCaptureRecorderTests.RecorderWritesUtf8BomCsvWithMovementMetrics,
                ZiplineMotionAnalyzerTests.AnalyzeDetectsStopPointsAndTurnIntersections,
                ZiplineMotionAnalyzerTests.AnalyzeInfersZiplineRangeWithoutManualStartOrEnd,
                ZiplineMotionAnalyzerTests.AnalyzeRealCaptureFindsTenZiplinePoints,
                ZiplineMotionAnalyzerTests.AnalyzeNewRealCapturesUsesExpectedPointCounts,
                ZiplineCollectionExporterTests.ExportMarksJsonUsesNewFormatWithBidirectionalConnections,
                ZiplineCollectionExporterTests.ExportRoutesJsonGroupsConnectedMarks,
                ZiplineCollectionExporterTests.ExportRoutesJsonSplitsWhenStopDoesNotConnectToPrevious,
                ZiplineRealtimeDetectorTests.DetectorConfirmsStablePositionNearMarkWithHeightOffset,
                ZiplineRealtimeDetectorTests.DetectorRejectsGroundPositionNearMarkWithWrongHeight,
                ZiplineRealtimeDetectorTests.DetectorDoesNotRepeatUntilLeavingPreviousMark,
                ZiplineRealtimeDetectorTests.DetectorReplaysLatestCaptureAndFindsFourStops,
                ZiplineRealtimeDetectorTests.DetectorReplaysCaptureWithFastConfirm,
                ZiplineRealtimeDetectorTests.DetectorReplaysCapture164946AndSplitsGroundTransition
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
