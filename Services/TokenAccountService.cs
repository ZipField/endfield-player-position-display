using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using endfield_player_position_display.Models;

namespace endfield_player_position_display.Services
{
    public sealed class TokenAccountService : IDisposable
    {
        private readonly SklandApiClient apiClient;
        private readonly bool ownsClient;

        public TokenAccountService()
            : this(new SklandApiClient(), true)
        {
        }

        internal TokenAccountService(SklandApiClient apiClient, bool ownsClient)
        {
            this.apiClient = apiClient;
            this.ownsClient = ownsClient;
        }

        public async Task<IList<TokenAccountInfo>> LoadAsync(string baseDirectory, CancellationToken cancellationToken)
        {
            IList<string> tokens;
            try
            {
                tokens = TokenFileReader.ReadTokens(baseDirectory, false);
            }
            catch (InvalidOperationException)
            {
                return new List<TokenAccountInfo>();
            }

            var result = new List<TokenAccountInfo>();
            for (int i = 0; i < tokens.Count; i++)
            {
                result.Add(await LoadOneAsync(tokens[i], i, cancellationToken).ConfigureAwait(false));
            }

            return result;
        }

        public void Dispose()
        {
            if (ownsClient)
            {
                apiClient.Dispose();
            }
        }

        private async Task<TokenAccountInfo> LoadOneAsync(string token, int tokenIndex, CancellationToken cancellationToken)
        {
            string maskedToken = TokenTextFormatter.MaskToken(token);
            try
            {
                string code = await apiClient.GrantAsync(token, cancellationToken).ConfigureAwait(false);
                CredentialResult credential = await apiClient.GenerateCredentialAsync(code, cancellationToken).ConfigureAwait(false);
                IList<RoleBinding> bindings = await apiClient.GetRoleBindingsAsync(credential, cancellationToken).ConfigureAwait(false);
                string rolesText = bindings.Count == 0
                    ? "未找到终末地角色"
                    : string.Join("；", bindings.Select(binding => "服务器 " + binding.ServerId + " / " + binding.DisplayName));
                return new TokenAccountInfo(tokenIndex, maskedToken, rolesText, string.Empty);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                string message = ex is InvalidOperationException ? ex.Message : "加载失败";
                return new TokenAccountInfo(tokenIndex, maskedToken, string.Empty, message);
            }
        }
    }
}
