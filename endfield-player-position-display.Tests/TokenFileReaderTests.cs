using System;
using System.IO;
using endfield_player_position_display.Services;

namespace endfield_player_position_display.Tests
{
    internal static class TokenFileReaderTests
    {
        public static void ReadTokenReadsTrimmedTokenFromBaseDirectory()
        {
            string dir = CreateTempDirectory();
            File.WriteAllText(Path.Combine(dir, "token.txt"), " abc123 \r\n");

            string token = TokenFileReader.ReadToken(dir);

            TestAssert.AreEqual("abc123", token);
        }

        public static void ReadTokensKeepsDuplicatesWhenRequested()
        {
            string dir = CreateTempDirectory();
            File.WriteAllText(Path.Combine(dir, "token.txt"), " abc123 \r\nabc123\r\n def456 \r\n");

            var tokens = TokenFileReader.ReadTokens(dir, false);

            TestAssert.AreEqual(3, tokens.Count);
            TestAssert.AreEqual("abc123", tokens[0]);
            TestAssert.AreEqual("abc123", tokens[1]);
            TestAssert.AreEqual("def456", tokens[2]);
        }

        public static void ReadTokensRemovesDuplicatesWhenRequested()
        {
            string dir = CreateTempDirectory();
            File.WriteAllText(Path.Combine(dir, "token.txt"), " abc123 \r\nabc123\r\n def456 \r\n");

            var tokens = TokenFileReader.ReadTokens(dir, true);

            TestAssert.AreEqual(2, tokens.Count);
            TestAssert.AreEqual("abc123", tokens[0]);
            TestAssert.AreEqual("def456", tokens[1]);
        }

        public static void ReadTokenThrowsChineseErrorWhenFileMissing()
        {
            string dir = CreateTempDirectory();

            InvalidOperationException ex = TestAssert.Throws<InvalidOperationException>(
                () => TokenFileReader.ReadToken(dir));

            TestAssert.AreEqual("未找到 token.txt", ex.Message);
        }

        public static void ReadTokenThrowsChineseErrorWhenFileBlank()
        {
            string dir = CreateTempDirectory();
            File.WriteAllText(Path.Combine(dir, "token.txt"), "   ");

            InvalidOperationException ex = TestAssert.Throws<InvalidOperationException>(
                () => TokenFileReader.ReadToken(dir));

            TestAssert.AreEqual("token.txt 内容为空", ex.Message);
        }

        public static void HasAnyTokenReturnsFalseWhenMissingOrBlank()
        {
            string missingDir = CreateTempDirectory();
            string blankDir = CreateTempDirectory();
            File.WriteAllText(Path.Combine(blankDir, "token.txt"), "   \r\n");

            TestAssert.AreEqual(false, TokenFileWriter.HasAnyToken(missingDir));
            TestAssert.AreEqual(false, TokenFileWriter.HasAnyToken(blankDir));
        }

        public static void AppendTokenAddsLineWithoutOverwritingExistingTokens()
        {
            string dir = CreateTempDirectory();
            File.WriteAllText(Path.Combine(dir, "token.txt"), "first\r\n");

            TokenFileWriter.AppendToken(dir, " second ");

            var tokens = TokenFileReader.ReadTokens(dir, false);
            TestAssert.AreEqual(2, tokens.Count);
            TestAssert.AreEqual("first", tokens[0]);
            TestAssert.AreEqual("second", tokens[1]);
        }

        public static void RemoveTokenAtRemovesOnlySelectedTokenLine()
        {
            string dir = CreateTempDirectory();
            File.WriteAllText(Path.Combine(dir, "token.txt"), "first\r\nsecond\r\nthird\r\n");

            TokenFileWriter.RemoveTokenAt(dir, 1);

            var tokens = TokenFileReader.ReadTokens(dir, false);
            TestAssert.AreEqual(2, tokens.Count);
            TestAssert.AreEqual("first", tokens[0]);
            TestAssert.AreEqual("third", tokens[1]);
        }

        public static void MaskTokenKeepsOnlyShortEdges()
        {
            TestAssert.AreEqual("abcd...7890", TokenTextFormatter.MaskToken("abcdef1234567890"));
            TestAssert.AreEqual("***", TokenTextFormatter.MaskToken("short"));
        }

        private static string CreateTempDirectory()
        {
            string dir = Path.Combine(Path.GetTempPath(), "endfield-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }
    }
}
