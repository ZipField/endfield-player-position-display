using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using endfield_player_position_display.Models;
using endfield_player_position_display.Services;

namespace endfield_player_position_display
{
    public partial class TokenManagerWindow : Window
    {
        private readonly string baseDirectory;
        private CancellationTokenSource cancellationTokenSource;

        public TokenManagerWindow(string baseDirectory)
        {
            InitializeComponent();
            this.baseDirectory = baseDirectory;
            Loaded += TokenManagerWindowLoaded;
            Closed += TokenManagerWindowClosed;
        }

        public bool TokensChanged { get; private set; }

        private async void TokenManagerWindowLoaded(object sender, RoutedEventArgs e)
        {
            await LoadTokensAsync();
        }

        private void TokenManagerWindowClosed(object sender, EventArgs e)
        {
            CancelLoad();
        }

        private async void RefreshButtonClick(object sender, RoutedEventArgs e)
        {
            await LoadTokensAsync();
        }

        private async void AddTokenButtonClick(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow { Owner = this };
            if (loginWindow.ShowDialog() == true)
            {
                TokenFileWriter.AppendToken(baseDirectory, loginWindow.Token);
                TokensChanged = true;
                await LoadTokensAsync();
            }
        }

        private async void RemoveTokenButtonClick(object sender, RoutedEventArgs e)
        {
            TokenAccountInfo account = TokenListBox.SelectedItem as TokenAccountInfo;
            if (account == null)
            {
                StatusText.Text = "请选择要移除的 token";
                return;
            }

            MessageBoxResult result = MessageBox.Show(this, "确定移除选中的 token？", "确认移除", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (result != MessageBoxResult.OK)
            {
                return;
            }

            try
            {
                TokenFileWriter.RemoveTokenAt(baseDirectory, account.TokenIndex);
                TokensChanged = true;
                await LoadTokensAsync();
            }
            catch (Exception ex)
            {
                StatusText.Text = ex is InvalidOperationException ? ex.Message : "移除 token 失败";
            }
        }

        private void CloseButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async Task LoadTokensAsync()
        {
            CancelLoad();
            cancellationTokenSource = new CancellationTokenSource();
            SetButtonsEnabled(false);
            StatusText.Text = "正在加载 token 和角色...";
            try
            {
                using (var service = new TokenAccountService())
                {
                    IList<TokenAccountInfo> accounts = await service.LoadAsync(baseDirectory, cancellationTokenSource.Token).ConfigureAwait(true);
                    TokenListBox.ItemsSource = accounts;
                    StatusText.Text = accounts.Count == 0 ? "没有 token" : "已加载 " + accounts.Count + " 个 token";
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private void CancelLoad()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }
        }

        private void SetButtonsEnabled(bool enabled)
        {
            AddTokenButton.IsEnabled = enabled;
            RemoveTokenButton.IsEnabled = enabled;
            RefreshButton.IsEnabled = enabled;
        }
    }
}
