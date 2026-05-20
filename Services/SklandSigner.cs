using System;
using System.Security.Cryptography;
using System.Text;

namespace endfield_player_position_display.Services
{
    public static class SklandSigner
    {
        public static string CreateHeaderJson(string timestamp)
        {
            return "{\"platform\":\"3\",\"timestamp\":\"" + timestamp + "\",\"dId\":\"\",\"vName\":\"1.0.0\"}";
        }

        public static string CreateSign(string path, string queryOrBody, string timestamp, string headerJson, string token)
        {
            string signSource = path + (queryOrBody ?? string.Empty) + timestamp + headerJson;
            string hmacHex;
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(token)))
            {
                hmacHex = ToHex(hmac.ComputeHash(Encoding.UTF8.GetBytes(signSource)));
            }

            using (var md5 = MD5.Create())
            {
                return ToHex(md5.ComputeHash(Encoding.UTF8.GetBytes(hmacHex)));
            }
        }

        private static string ToHex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (byte value in bytes)
            {
                builder.Append(value.ToString("x2"));
            }

            return builder.ToString();
        }
    }
}
