using System;
using System.IO;

namespace endfield_player_position_display.Services
{
    public static class TokenFileReader
    {
        public static string ReadToken(string baseDirectory)
        {
            string path = Path.Combine(baseDirectory, "token.txt");
            if (!File.Exists(path))
            {
                throw new InvalidOperationException("未找到 token.txt");
            }

            string token = File.ReadAllText(path).Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException("token.txt 内容为空");
            }

            return token;
        }
    }
}
