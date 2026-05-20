using System;
using System.Threading;
using System.Threading.Tasks;
using endfield_player_position_display.Models;

namespace endfield_player_position_display.Services
{
    public sealed class PositionMonitorService : IDisposable
    {
        private readonly string baseDirectory;
        private readonly Action<MonitorUpdate> onUpdate;
        private readonly SklandApiClient apiClient;
        private readonly PositionWebSocketClient webSocketClient;
        private CancellationTokenSource cancellationTokenSource;

        public PositionMonitorService(string baseDirectory, Action<MonitorUpdate> onUpdate)
            : this(baseDirectory, onUpdate, new SklandApiClient(), new PositionWebSocketClient())
        {
        }

        internal PositionMonitorService(
            string baseDirectory,
            Action<MonitorUpdate> onUpdate,
            SklandApiClient apiClient,
            PositionWebSocketClient webSocketClient)
        {
            this.baseDirectory = baseDirectory;
            this.onUpdate = onUpdate;
            this.apiClient = apiClient;
            this.webSocketClient = webSocketClient;
        }

        public Task StartAsync()
        {
            Stop();
            cancellationTokenSource = new CancellationTokenSource();
            return RunAsync(cancellationTokenSource.Token);
        }

        public void Stop()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }
        }

        public void Dispose()
        {
            Stop();
            apiClient.Dispose();
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                onUpdate(MonitorUpdate.Connecting("正在连接..."));
                string token = TokenFileReader.ReadToken(baseDirectory);

                string code = await apiClient.GrantAsync(token, cancellationToken).ConfigureAwait(false);
                CredentialResult credential = await apiClient.GenerateCredentialAsync(code, cancellationToken).ConfigureAwait(false);
                RoleBinding roleBinding = await apiClient.GetRoleBindingAsync(credential, cancellationToken).ConfigureAwait(false);
                string webSocketToken = await apiClient.GetWebSocketTokenAsync(credential, cancellationToken).ConfigureAwait(false);

                await webSocketClient.RunAsync(
                    webSocketToken,
                    roleBinding,
                    position => onUpdate(MonitorUpdate.FromPosition(position)),
                    error => onUpdate(MonitorUpdate.Error(error)),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (InvalidOperationException ex)
            {
                onUpdate(MonitorUpdate.Error(ex.Message));
            }
            catch (Exception)
            {
                onUpdate(MonitorUpdate.Error("连接失败"));
            }
        }
    }
}
