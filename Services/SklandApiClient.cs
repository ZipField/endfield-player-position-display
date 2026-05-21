using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using endfield_player_position_display.Models;

namespace endfield_player_position_display.Services
{
    public sealed class SklandApiClient : IDisposable
    {
        private const string GrantUrl = "https://as.hypergryph.com/user/oauth2/v2/grant";
        private const string CredUrl = "https://zonai.skland.com/web/v1/user/auth/generate_cred_by_code";
        private const string BindingPath = "/api/v1/game/player/binding";
        private const string WebSocketTokenPath = "/api/v1/websocket/token";
        private const string ZonaiBaseUrl = "https://zonai.skland.com";
        internal const int SignedRequestTimestampOffsetSeconds = -3;

        private readonly HttpClient httpClient;
        private readonly bool ownsClient;
        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer();

        public SklandApiClient()
            : this(new HttpClient(), true)
        {
        }

        internal SklandApiClient(HttpClient httpClient, bool ownsClient)
        {
            this.httpClient = httpClient;
            this.ownsClient = ownsClient;
        }

        public async Task<string> GrantAsync(string token, CancellationToken cancellationToken)
        {
            var body = new Dictionary<string, object>
            {
                { "token", token },
                { "appCode", "4ca99fa6b56cc2ba" },
                { "type", 0 }
            };

            string json = await PostJsonAsync(GrantUrl, body, cancellationToken).ConfigureAwait(false);
            var root = DeserializeObject(json);
            if (GetInt(root, "status") != 0)
            {
                throw new InvalidOperationException("获取授权码失败");
            }

            IDictionary<string, object> data = GetObject(root, "data");
            string code = GetString(data, "code");
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new InvalidOperationException("获取授权码失败");
            }

            return code;
        }

        public async Task<CredentialResult> GenerateCredentialAsync(string code, CancellationToken cancellationToken)
        {
            var body = new Dictionary<string, object>
            {
                { "code", code },
                { "kind", 1 }
            };

            string json = await PostJsonAsync(CredUrl, body, cancellationToken).ConfigureAwait(false);
            var root = DeserializeObject(json);
            EnsureCodeOk(root, "获取凭证失败");
            IDictionary<string, object> data = GetObject(root, "data");
            string cred = GetString(data, "cred");
            string userId = GetString(data, "userId");
            string token = GetString(data, "token");
            if (string.IsNullOrWhiteSpace(cred) || string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException("获取凭证失败");
            }

            return new CredentialResult(cred, userId, token);
        }

        public async Task<RoleBinding> GetRoleBindingAsync(CredentialResult credential, CancellationToken cancellationToken)
        {
            string json = await GetSignedAsync(ZonaiBaseUrl + BindingPath, BindingPath, credential, cancellationToken).ConfigureAwait(false);
            return ParseRoleBinding(json);
        }

        public async Task<string> GetWebSocketTokenAsync(CredentialResult credential, CancellationToken cancellationToken)
        {
            string json = await GetSignedAsync(ZonaiBaseUrl + WebSocketTokenPath, WebSocketTokenPath, credential, cancellationToken).ConfigureAwait(false);
            return ParseWebSocketToken(json);
        }

        public static RoleBinding ParseRoleBinding(string json)
        {
            try
            {
                var serializer = new JavaScriptSerializer();
                IDictionary<string, object> root = serializer.DeserializeObject(json) as IDictionary<string, object>;
                EnsureCodeOk(root, "未找到终末地角色");

                IDictionary<string, object> data = GetObject(root, "data");
                IEnumerable list = GetArray(data, "list");
                foreach (object appObject in list)
                {
                    var app = appObject as IDictionary<string, object>;
                    if (app == null || !string.Equals(GetString(app, "appCode"), "endfield", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    IEnumerable bindingList = GetArray(app, "bindingList");
                    foreach (object bindingObject in bindingList)
                    {
                        var binding = bindingObject as IDictionary<string, object>;
                        if (binding == null)
                        {
                            continue;
                        }

                        IDictionary<string, object> defaultRole = GetObject(binding, "defaultRole");
                        string serverId = GetString(defaultRole, "serverId");
                        string roleId = GetString(defaultRole, "roleId");
                        if (!string.IsNullOrWhiteSpace(serverId) && !string.IsNullOrWhiteSpace(roleId))
                        {
                            return new RoleBinding(serverId, roleId);
                        }
                    }
                }
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch
            {
                throw new InvalidOperationException("未找到终末地角色");
            }

            throw new InvalidOperationException("未找到终末地角色");
        }

        public static string ParseWebSocketToken(string json)
        {
            try
            {
                var serializer = new JavaScriptSerializer();
                IDictionary<string, object> root = serializer.DeserializeObject(json) as IDictionary<string, object>;
                EnsureCodeOk(root, "获取 WebSocket token 失败");
                IDictionary<string, object> data = GetObject(root, "data");
                string token = GetString(data, "token");
                if (!string.IsNullOrWhiteSpace(token))
                {
                    return token;
                }
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch
            {
                throw new InvalidOperationException("获取 WebSocket token 失败");
            }

            throw new InvalidOperationException("获取 WebSocket token 失败");
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
            string contentJson = serializer.Serialize(body);
            using (var content = new StringContent(contentJson, Encoding.UTF8, "application/json"))
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

        private async Task<string> GetSignedAsync(string url, string path, CredentialResult credential, CancellationToken cancellationToken)
        {
            string timestamp = CreateSignedRequestTimestamp(DateTimeOffset.UtcNow);
            string headerJson = SklandSigner.CreateHeaderJson(timestamp);
            string sign = SklandSigner.CreateSign(path, GetQueryForSigning(url), timestamp, headerJson, credential.Token);

            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.TryAddWithoutValidation("platform", "3");
                request.Headers.TryAddWithoutValidation("timestamp", timestamp);
                request.Headers.TryAddWithoutValidation("dId", string.Empty);
                request.Headers.TryAddWithoutValidation("vName", "1.0.0");
                request.Headers.TryAddWithoutValidation("Cred", credential.Cred);
                request.Headers.TryAddWithoutValidation("Sign", sign);
                request.Headers.TryAddWithoutValidation("Content-Type", "application/json;charset=UTF-8");

                using (HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    string responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException("请求失败");
                    }

                    return responseJson;
                }
            }
        }

        internal static string CreateSignedRequestTimestamp(DateTimeOffset now)
        {
            return now.AddSeconds(SignedRequestTimestampOffsetSeconds).ToUnixTimeSeconds().ToString();
        }

        private IDictionary<string, object> DeserializeObject(string json)
        {
            var root = serializer.DeserializeObject(json) as IDictionary<string, object>;
            if (root == null)
            {
                throw new InvalidOperationException("请求失败");
            }

            return root;
        }

        private static string GetQueryForSigning(string url)
        {
            string query = new Uri(url).Query;
            return string.IsNullOrEmpty(query) ? string.Empty : query.TrimStart('?');
        }

        private static void EnsureCodeOk(IDictionary<string, object> root, string errorMessage)
        {
            if (root == null || GetInt(root, "code") != 0)
            {
                throw new InvalidOperationException(errorMessage);
            }
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

        private static IEnumerable GetArray(IDictionary<string, object> obj, string key)
        {
            if (obj == null || !obj.ContainsKey(key))
            {
                return new object[0];
            }

            return obj[key] as IEnumerable ?? new object[0];
        }
    }
}
