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

        private static string CreateTempDirectory()
        {
            string dir = Path.Combine(Path.GetTempPath(), "endfield-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }
    }
}
