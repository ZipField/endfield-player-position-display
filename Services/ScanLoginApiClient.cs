using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace endfield_player_position_display.Services
{
    public sealed class ScanLoginApiClient : IDisposable
    {
        public const string AppCode = "4ca99fa6b56cc2ba";
        private const string GenScanLoginUrl = "https://as.hypergryph.com/general/v1/gen_scan/login";
        private const string ScanStatusUrl = "https://as.hypergryph.com/general/v1/scan_status?scanId=";
        private const string TokenByScanCodeUrl = "https://as.hypergryph.com/user/auth/v1/token_by_scan_code";

        private readonly HttpClient httpClient;
        private readonly bool ownsClient;
        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer();

        public ScanLoginApiClient()
            : this(new HttpClient(), true)
        {
        }

        internal ScanLoginApiClient(HttpClient httpClient, bool ownsClient)
        {
            this.httpClient = httpClient;
            this.ownsClient = ownsClient;
        }

        public async Task<ScanLoginSession> CreateScanLoginSessionAsync(CancellationToken cancellationToken)
        {
            var body = new Dictionary<string, object>
            {
                { "appCode", AppCode }
            };

            string json = await PostJsonAsync(GenScanLoginUrl, body, cancellationToken).ConfigureAwait(false);
            return ParseScanLoginSession(json);
        }

        public async Task<ScanLoginStatus> GetScanStatusAsync(string scanId, CancellationToken cancellationToken)
        {
            string url = ScanStatusUrl + Uri.EscapeDataString(scanId);
            string json = await GetJsonAsync(url, cancellationToken).ConfigureAwait(false);
            return ParseScanStatus(json);
        }

        public async Task<string> ExchangeScanCodeAsync(string scanCode, CancellationToken cancellationToken)
        {
            var body = new Dictionary<string, object>
            {
                { "scanCode", scanCode },
                { "appCode", AppCode }
            };

            string json = await PostJsonAsync(TokenByScanCodeUrl, body, cancellationToken).ConfigureAwait(false);
            return ParseTokenByScanCode(json);
        }

        public static ScanLoginSession ParseScanLoginSession(string json)
        {
            IDictionary<string, object> root = DeserializeRoot(json, "生成扫码登录失败");
            if (GetInt(root, "status") != 0)
            {
                throw new InvalidOperationException("生成扫码登录失败");
            }

            IDictionary<string, object> data = GetObject(root, "data");
            string scanId = GetString(data, "scanId");
            string scanUrl = GetString(data, "scanUrl");
            if (string.IsNullOrWhiteSpace(scanId) || string.IsNullOrWhiteSpace(scanUrl))
            {
                throw new InvalidOperationException("生成扫码登录失败");
            }

            return new ScanLoginSession(scanId, scanUrl);
        }

        public static ScanLoginStatus ParseScanStatus(string json)
        {
            IDictionary<string, object> root = DeserializeRoot(json, "获取扫码状态失败");
            int status = GetInt(root, "status");
            string message = GetString(root, "msg");
            if (status == 100)
            {
                return new ScanLoginStatus(ScanLoginState.PendingScan, message, null);
            }

            if (status == 101)
            {
                return new ScanLoginStatus(ScanLoginState.PendingConfirm, message, null);
            }

            if (status == 0)
            {
                IDictionary<string, object> data = GetObject(root, "data");
                string scanCode = GetString(data, "scanCode");
                if (string.IsNullOrWhiteSpace(scanCode))
                {
                    throw new InvalidOperationException("获取扫码状态失败");
                }

                return new ScanLoginStatus(ScanLoginState.Confirmed, message, scanCode);
            }

            return new ScanLoginStatus(ScanLoginState.Error, message, null);
        }

        public static string ParseTokenByScanCode(string json)
        {
            IDictionary<string, object> root = DeserializeRoot(json, "扫码登录失败");
            if (GetInt(root, "status") != 0)
            {
                throw new InvalidOperationException("扫码登录失败");
            }

            IDictionary<string, object> data = GetObject(root, "data");
            string token = GetString(data, "token");
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException("扫码登录失败");
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

        private async Task<string> GetJsonAsync(string url, CancellationToken cancellationToken)
        {
            using (HttpResponseMessage response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false))
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

    public sealed class ScanLoginSession
    {
        public ScanLoginSession(string scanId, string scanUrl)
        {
            ScanId = scanId;
            ScanUrl = scanUrl;
        }

        public string ScanId { get; private set; }

        public string ScanUrl { get; private set; }
    }

    public sealed class ScanLoginStatus
    {
        public ScanLoginStatus(ScanLoginState state, string message, string scanCode)
        {
            State = state;
            Message = message;
            ScanCode = scanCode;
        }

        public ScanLoginState State { get; private set; }

        public string Message { get; private set; }

        public string ScanCode { get; private set; }
    }

    public enum ScanLoginState
    {
        PendingScan,
        PendingConfirm,
        Confirmed,
        Error
    }

}
