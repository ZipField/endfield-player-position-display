using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly NetworkTimeService networkTimeService;
        private readonly string selectedRoleKey;
        private CancellationTokenSource cancellationTokenSource;

        public PositionMonitorService(string baseDirectory, Action<MonitorUpdate> onUpdate)
            : this(baseDirectory, onUpdate, null)
        {
        }

        public PositionMonitorService(string baseDirectory, Action<MonitorUpdate> onUpdate, string selectedRoleKey)
            : this(baseDirectory, onUpdate, new SklandApiClient(), new PositionWebSocketClient(), new NetworkTimeService(), selectedRoleKey)
        {
        }

        internal PositionMonitorService(
            string baseDirectory,
            Action<MonitorUpdate> onUpdate,
            SklandApiClient apiClient,
            PositionWebSocketClient webSocketClient)
            : this(baseDirectory, onUpdate, apiClient, webSocketClient, new NetworkTimeService(), null)
        {
        }

        internal PositionMonitorService(
            string baseDirectory,
            Action<MonitorUpdate> onUpdate,
            SklandApiClient apiClient,
            PositionWebSocketClient webSocketClient,
            NetworkTimeService networkTimeService)
            : this(baseDirectory, onUpdate, apiClient, webSocketClient, networkTimeService, null)
        {
        }

        internal PositionMonitorService(
            string baseDirectory,
            Action<MonitorUpdate> onUpdate,
            SklandApiClient apiClient,
            PositionWebSocketClient webSocketClient,
            NetworkTimeService networkTimeService,
            string selectedRoleKey)
        {
            this.baseDirectory = baseDirectory;
            this.onUpdate = onUpdate;
            this.apiClient = apiClient;
            this.webSocketClient = webSocketClient;
            this.networkTimeService = networkTimeService;
            this.selectedRoleKey = selectedRoleKey;
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
            networkTimeService.Dispose();
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                onUpdate(MonitorUpdate.Connecting("正在连接..."));
                await SyncNetworkTimeAsync(cancellationToken).ConfigureAwait(false);
                IList<string> tokens = TokenFileReader.ReadTokens(baseDirectory);
                List<RoleSession> roleSessions = await LoadRoleSessionsAsync(tokens, cancellationToken).ConfigureAwait(false);
                if (roleSessions.Count == 0)
                {
                    throw new InvalidOperationException("未找到终末地角色");
                }

                RoleSession activeRole = SelectRole(roleSessions);
                CredentialResult credential = activeRole.Credential;
                RoleBinding roleBinding = activeRole.RoleBinding;
                string webSocketToken = await apiClient.GetWebSocketTokenAsync(credential, cancellationToken).ConfigureAwait(false);
                onUpdate(MonitorUpdate.SessionReady(activeRole, roleSessions));

                string latestMapId = null;

                await webSocketClient.RunAsync(
                    webSocketToken,
                    roleBinding,
                    (position, mapId) =>
                    {
                        if (!string.IsNullOrWhiteSpace(mapId))
                        {
                            latestMapId = mapId;
                        }

                        onUpdate(MonitorUpdate.FromPosition(
                            position,
                            new MonitorSessionState(credential, roleBinding, latestMapId)));
                    },
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

        private async Task<List<RoleSession>> LoadRoleSessionsAsync(IList<string> tokens, CancellationToken cancellationToken)
        {
            Task<List<RoleSession>>[] tasks = tokens
                .Select((token, index) => LoadRoleSessionsForTokenAsync(token, index, cancellationToken))
                .ToArray();
            List<RoleSession>[] results = await Task.WhenAll(tasks).ConfigureAwait(false);
            return results.SelectMany(item => item).ToList();
        }

        private async Task<List<RoleSession>> LoadRoleSessionsForTokenAsync(string token, int tokenIndex, CancellationToken cancellationToken)
        {
            string code = await apiClient.GrantAsync(token, cancellationToken).ConfigureAwait(false);
            CredentialResult credential = await apiClient.GenerateCredentialAsync(code, cancellationToken).ConfigureAwait(false);
            IList<RoleBinding> bindings = await apiClient.GetRoleBindingsAsync(credential, cancellationToken).ConfigureAwait(false);
            return bindings.Select(binding => new RoleSession(tokenIndex, credential, binding)).ToList();
        }

        private RoleSession SelectRole(List<RoleSession> roleSessions)
        {
            if (!string.IsNullOrWhiteSpace(selectedRoleKey))
            {
                RoleSession selected = roleSessions.FirstOrDefault(role => string.Equals(role.Key, selectedRoleKey, StringComparison.Ordinal));
                if (selected != null)
                {
                    return selected;
                }
            }

            return roleSessions[0];
        }

        private async Task SyncNetworkTimeAsync(CancellationToken cancellationToken)
        {
            TimeSpan? offset = await networkTimeService.TryGetOffsetAsync(cancellationToken).ConfigureAwait(false);
            if (offset.HasValue)
            {
                apiClient.NetworkTimeOffset = offset.Value;
            }
        }
    }
}
