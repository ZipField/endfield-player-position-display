using System;
using System.IO;
using endfield_player_position_display.Services;

namespace endfield_player_position_display.Tests
{
    internal static class ScanLoginTests
    {
        public static void ParseScanLoginSessionExtractsScanIdAndUrl()
        {
            string json = "{\"data\":{\"scanId\":\"35fe249074b4e40607198027feccd512\",\"scanUrl\":\"hypergryph://scan_login?scanId=35fe249074b4e40607198027feccd512\"},\"msg\":\"OK\",\"status\":0,\"type\":\"A\"}";

            ScanLoginSession session = ScanLoginApiClient.ParseScanLoginSession(json);

            TestAssert.AreEqual("35fe249074b4e40607198027feccd512", session.ScanId);
            TestAssert.AreEqual("hypergryph://scan_login?scanId=35fe249074b4e40607198027feccd512", session.ScanUrl);
        }

        public static void ParseScanStatusMapsPendingScannedAndConfirmed()
        {
            ScanLoginStatus pending = ScanLoginApiClient.ParseScanStatus("{\"msg\":\"未扫码\",\"status\":100,\"type\":\"A\"}");
            ScanLoginStatus scanned = ScanLoginApiClient.ParseScanStatus("{\"msg\":\"已扫码待确认\",\"status\":101,\"type\":\"A\"}");
            ScanLoginStatus confirmed = ScanLoginApiClient.ParseScanStatus("{\"data\":{\"scanCode\":\"irAWmp/q9HC2hJoK/1uVch5QpIg\"},\"msg\":\"OK\",\"status\":0,\"type\":\"A\"}");

            TestAssert.AreEqual(ScanLoginState.PendingScan, pending.State);
            TestAssert.AreEqual("未扫码", pending.Message);
            TestAssert.AreEqual(ScanLoginState.PendingConfirm, scanned.State);
            TestAssert.AreEqual("已扫码待确认", scanned.Message);
            TestAssert.AreEqual(ScanLoginState.Confirmed, confirmed.State);
            TestAssert.AreEqual("irAWmp/q9HC2hJoK/1uVch5QpIg", confirmed.ScanCode);
        }

        public static void ParseTokenByScanCodeExtractsToken()
        {
            string json = "{\"data\":{\"token\":\"abcdef1234567890\",\"hgId\":\"\",\"deviceToken\":\"\",\"rememberLogin\":true},\"msg\":\"OK\",\"status\":0,\"type\":\"A\"}";

            string token = ScanLoginApiClient.ParseTokenByScanCode(json);

            TestAssert.AreEqual("abcdef1234567890", token);
        }

        public static void ParsePhonePasswordLoginExtractsToken()
        {
            string json = "{\"data\":{\"token\":\"x6wabcdef1234567890\"},\"msg\":\"OK\",\"status\":0,\"type\":\"A\"}";

            string token = PhonePasswordLoginApiClient.ParseToken(json);

            TestAssert.AreEqual("x6wabcdef1234567890", token);
        }

        public static void ParseSendPhoneCodeAcceptsOkResponse()
        {
            PhoneCodeLoginApiClient.ParseSendCodeResult("{\"msg\":\"OK\",\"status\":0,\"type\":\"A\"}");
        }

        public static void ParseSendPhoneCodeThrowsReturnedMessageForError()
        {
            InvalidOperationException ex = TestAssert.Throws<InvalidOperationException>(
                () => PhoneCodeLoginApiClient.ParseSendCodeResult("{\"msg\":\"获取验证码过于频繁\",\"status\":101,\"type\":\"A\"}"));

            TestAssert.AreEqual("获取验证码过于频繁", ex.Message);
        }

        public static void ParsePhoneCodeLoginExtractsToken()
        {
            string json = "{\"data\":{\"token\":\"zbabcdef1234567890\",\"hgId\":\"27\",\"deviceToken\":\"sNNabcdef\"},\"msg\":\"OK\",\"status\":0,\"type\":\"A\"}";

            string token = PhoneCodeLoginApiClient.ParseToken(json);

            TestAssert.AreEqual("zbabcdef1234567890", token);
        }

        public static void TokenFileWriterWritesUtf8TokenTxt()
        {
            string dir = Path.Combine(Path.GetTempPath(), "endfield-token-writer-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                TokenFileWriter.WriteToken(dir, " abc123 ");

                string token = File.ReadAllText(Path.Combine(dir, "token.txt")).Trim();
                TestAssert.AreEqual("abc123", token);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
