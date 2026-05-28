using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        public static IList<string> ReadTokens(string baseDirectory)
        {
#if DEBUG
            return ReadTokens(baseDirectory, false);
#else
            return ReadTokens(baseDirectory, true);
#endif
        }

        public static IList<string> ReadTokens(string baseDirectory, bool distinct)
        {
            string path = Path.Combine(baseDirectory, "token.txt");
            if (!File.Exists(path))
            {
                throw new InvalidOperationException("未找到 token.txt");
            }

            IEnumerable<string> tokens = File.ReadAllLines(path)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line));

            if (distinct)
            {
                tokens = tokens.Distinct(StringComparer.Ordinal);
            }

            var result = tokens.ToList();
            if (result.Count == 0)
            {
                throw new InvalidOperationException("token.txt 内容为空");
            }

            return result;
        }
    }
}
