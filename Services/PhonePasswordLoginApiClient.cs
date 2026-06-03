using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace endfield_player_position_display.Services
{
    public sealed class PhonePasswordLoginApiClient : IDisposable
    {
        private const string TokenByPhonePasswordUrl = "https://as.hypergryph.com/user/auth/v1/token_by_phone_password";

        private readonly HttpClient httpClient;
        private readonly bool ownsClient;
        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer();

        public PhonePasswordLoginApiClient()
            : this(new HttpClient(), true)
        {
        }

        internal PhonePasswordLoginApiClient(HttpClient httpClient, bool ownsClient)
        {
            this.httpClient = httpClient;
            this.ownsClient = ownsClient;
        }

        public async Task<string> LoginAsync(string phone, string password, CancellationToken cancellationToken)
        {
            var body = new Dictionary<string, object>
            {
                { "phone", phone },
                { "password", password }
            };

            string json = await PostJsonAsync(TokenByPhonePasswordUrl, body, cancellationToken).ConfigureAwait(false);
            return ParseToken(json);
        }

        public static string ParseToken(string json)
        {
            IDictionary<string, object> root = DeserializeRoot(json, "帐号密码登录失败");
            if (GetInt(root, "status") != 0)
            {
                throw new InvalidOperationException("帐号密码登录失败");
            }

            IDictionary<string, object> data = GetObject(root, "data");
            string token = GetString(data, "token");
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException("帐号密码登录失败");
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
