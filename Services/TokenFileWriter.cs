using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace endfield_player_position_display.Services
{
    public static class TokenFileWriter
    {
        public static void WriteToken(string baseDirectory, string token)
        {
            string trimmedToken = token == null ? string.Empty : token.Trim();
            if (string.IsNullOrWhiteSpace(trimmedToken))
            {
                throw new InvalidOperationException("token 为空");
            }

            string path = Path.Combine(baseDirectory, "token.txt");
            File.WriteAllText(path, trimmedToken + Environment.NewLine, Encoding.UTF8);
        }

        public static void AppendToken(string baseDirectory, string token)
        {
            string trimmedToken = token == null ? string.Empty : token.Trim();
            if (string.IsNullOrWhiteSpace(trimmedToken))
            {
                throw new InvalidOperationException("token 为空");
            }

            string path = Path.Combine(baseDirectory, "token.txt");
            string prefix = string.Empty;
            if (File.Exists(path))
            {
                string existing = File.ReadAllText(path, Encoding.UTF8);
                if (existing.Length > 0 && !existing.EndsWith("\n", StringComparison.Ordinal))
                {
                    prefix = Environment.NewLine;
                }
            }

            File.AppendAllText(path, prefix + trimmedToken + Environment.NewLine, Encoding.UTF8);
        }

        public static void RemoveTokenAt(string baseDirectory, int tokenIndex)
        {
            string path = Path.Combine(baseDirectory, "token.txt");
            if (!File.Exists(path))
            {
                throw new InvalidOperationException("未找到 token.txt");
            }

            List<string> tokens = File.ReadAllLines(path, Encoding.UTF8)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();
            if (tokenIndex < 0 || tokenIndex >= tokens.Count)
            {
                throw new InvalidOperationException("token 不存在");
            }

            tokens.RemoveAt(tokenIndex);
            File.WriteAllLines(path, tokens, Encoding.UTF8);
        }

        public static bool HasAnyToken(string baseDirectory)
        {
            string path = Path.Combine(baseDirectory, "token.txt");
            if (!File.Exists(path))
            {
                return false;
            }

            return File.ReadAllLines(path, Encoding.UTF8)
                .Any(line => !string.IsNullOrWhiteSpace(line));
        }
    }
}
