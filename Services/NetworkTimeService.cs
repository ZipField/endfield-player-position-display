using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace endfield_player_position_display.Services
{
    public sealed class NetworkTimeService : IDisposable
    {
        private static readonly Uri TimeSourceUri = new Uri("https://www.baidu.com/");
        private readonly HttpClient httpClient;
        private readonly bool ownsClient;

        public NetworkTimeService()
            : this(new HttpClient(), true)
        {
        }

        internal NetworkTimeService(HttpClient httpClient, bool ownsClient)
        {
            this.httpClient = httpClient;
            this.ownsClient = ownsClient;
        }

        public async Task<TimeSpan?> TryGetOffsetAsync(CancellationToken cancellationToken)
        {
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Head, TimeSourceUri))
                using (HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    DateTimeOffset? networkDate = response.Headers.Date;
                    if (!networkDate.HasValue)
                    {
                        return null;
                    }

                    return CalculateOffset(DateTimeOffset.UtcNow, networkDate.Value.ToUniversalTime());
                }
            }
            catch
            {
                return null;
            }
        }

        public static TimeSpan CalculateOffset(DateTimeOffset localUtc, DateTimeOffset networkUtc)
        {
            return networkUtc.ToUniversalTime() - localUtc.ToUniversalTime();
        }

        public void Dispose()
        {
            if (ownsClient)
            {
                httpClient.Dispose();
            }
        }
    }
}
