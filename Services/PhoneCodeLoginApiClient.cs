using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace endfield_player_position_display.Services
{
    public sealed class PhoneCodeLoginApiClient : IDisposable
    {
        private const string SendPhoneCodeUrl = "https://as.hypergryph.com/general/v1/send_phone_code";
        private const string TokenByPhoneCodeUrl = "https://as.hypergryph.com/user/auth/v2/token_by_phone_code";

        private readonly HttpClient httpClient;
        private readonly bool ownsClient;
        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer();

        public PhoneCodeLoginApiClient()
            : this(new HttpClient(), true)
        {
        }

        internal PhoneCodeLoginApiClient(HttpClient httpClient, bool ownsClient)
        {
            this.httpClient = httpClient;
            this.ownsClient = ownsClient;
        }

        public async Task SendCodeAsync(string phone, CancellationToken cancellationToken)
        {
            var body = new Dictionary<string, object>
            {
                { "phone", phone },
                { "type", 2 }
            };

            string json = await PostJsonAsync(SendPhoneCodeUrl, body, cancellationToken).ConfigureAwait(false);
            ParseSendCodeResult(json);
        }

        public async Task<string> LoginAsync(string phone, string code, CancellationToken cancellationToken)
        {
            var body = new Dictionary<string, object>
            {
                { "phone", phone },
                { "code", code },
                { "appCode", ScanLoginApiClient.AppCode }
            };

            string json = await PostJsonAsync(TokenByPhoneCodeUrl, body, cancellationToken).ConfigureAwait(false);
            return ParseToken(json);
        }

        public static void ParseSendCodeResult(string json)
        {
            IDictionary<string, object> root = DeserializeRoot(json, "发送验证码失败");
            if (GetInt(root, "status") == 0)
            {
                return;
            }

            string message = GetString(root, "msg");
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(message) ? "发送验证码失败" : message);
        }

        public static string ParseToken(string json)
        {
            IDictionary<string, object> root = DeserializeRoot(json, "手机验证码登录失败");
            if (GetInt(root, "status") != 0)
            {
                string message = GetString(root, "msg");
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(message) ? "手机验证码登录失败" : message);
            }

            IDictionary<string, object> data = GetObject(root, "data");
            string token = GetString(data, "token");
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException("手机验证码登录失败");
            }

            return token;
        }

        public void Dispose()
        {
            if (ownsClient)
            {
                httpClient.Dispose();
            }
        }

        private async Task<string> PostJsonAsync(string url, IDictionary<string, object> body, CancellationToken cancellationToken)
        {
            string requestJson = serializer.Serialize(body);
            using (var content = new StringContent(requestJson, Encoding.UTF8, "application/json"))
            using (HttpResponseMessage response = await httpClient.PostAsync(url, content, cancellationToken).ConfigureAwait(false))
            {
                string responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException("请求失败");
                }

                return responseJson;
            }
        }

        private static IDictionary<string, object> DeserializeRoot(string json, string errorMessage)
        {
            try
            {
                var serializer = new JavaScriptSerializer();
                IDictionary<string, object> root = serializer.DeserializeObject(json) as IDictionary<string, object>;
                if (root != null)
                {
                    return root;
                }
            }
            catch
            {
            }

            throw new InvalidOperationException(errorMessage);
        }

        private static int GetInt(IDictionary<string, object> obj, string key)
        {
            if (obj == null || !obj.ContainsKey(key) || obj[key] == null)
            {
                return int.MinValue;
            }

            return Convert.ToInt32(obj[key]);
        }

        private static string GetString(IDictionary<string, object> obj, string key)
        {
            if (obj == null || !obj.ContainsKey(key) || obj[key] == null)
            {
                return null;
            }

            return Convert.ToString(obj[key]);
        }

        private static IDictionary<string, object> GetObject(IDictionary<string, object> obj, string key)
        {
            if (obj == null || !obj.ContainsKey(key))
            {
                return null;
            }

            return obj[key] as IDictionary<string, object>;
        }
    }
}
