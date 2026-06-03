using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using endfield_player_position_display.Services;
using QRCoder;

namespace endfield_player_position_display
{
    public partial class LoginWindow : Window
    {
        private readonly DispatcherTimer cooldownTimer = new DispatcherTimer();
        private ScanLoginApiClient scanLoginClient;
        private CancellationTokenSource scanCancellationTokenSource;
        private int cooldownSeconds;
        private bool scanStarted;

        public LoginWindow()
        {
            InitializeComponent();
            Loaded += LoginWindowLoaded;
            Closed += LoginWindowClosed;
            DataObject.AddPastingHandler(CodePhoneTextBox, DigitsOnlyPasting);
            DataObject.AddPastingHandler(CodeTextBox, DigitsOnlyPasting);
            DataObject.AddPastingHandler(PasswordPhoneTextBox, DigitsOnlyPasting);
            cooldownTimer.Interval = TimeSpan.FromSeconds(1);
            cooldownTimer.Tick += CooldownTimerTick;
        }

        public string Token { get; private set; }

        private async void LoginWindowLoaded(object sender, RoutedEventArgs e)
        {
            await StartScanLoginAsync();
        }

        private void LoginWindowClosed(object sender, EventArgs e)
        {
            CancelScanLogin();
            cooldownTimer.Stop();
        }

        private async void LoginTabsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source != LoginTabs)
            {
                return;
            }

            if (LoginTabs.SelectedIndex == 0 && !scanStarted)
            {
                await StartScanLoginAsync();
                return;
            }

            if (LoginTabs.SelectedIndex != 0)
            {
                CancelScanLogin();
                scanStarted = false;
                StatusText.Text = string.Empty;
            }
        }

        private async void RefreshQrButtonClick(object sender, RoutedEventArgs e)
        {
            await StartScanLoginAsync();
        }

        private async Task StartScanLoginAsync()
        {
            CancelScanLogin();
            scanStarted = true;
            QrImage.Source = null;
            StatusText.Text = "正在生成二维码...";
            RefreshQrButton.IsEnabled = false;
            scanCancellationTokenSource = new CancellationTokenSource();
            scanLoginClient = new ScanLoginApiClient();

            try
            {
                ScanLoginSession session = await scanLoginClient.CreateScanLoginSessionAsync(scanCancellationTokenSource.Token).ConfigureAwait(true);
                QrImage.Source = CreateQrImage(session.ScanUrl);
                StatusText.Text = "请使用森空岛扫码";
                await PollScanStatusAsync(session.ScanId, scanCancellationTokenSource.Token).ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                StatusText.Text = ex is InvalidOperationException ? ex.Message : "扫码登录失败";
            }
            finally
            {
                RefreshQrButton.IsEnabled = true;
            }
        }

        private async Task PollScanStatusAsync(string scanId, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ScanLoginStatus status = await scanLoginClient.GetScanStatusAsync(scanId, cancellationToken).ConfigureAwait(true);
                if (status.State == ScanLoginState.PendingScan || status.State == ScanLoginState.PendingConfirm)
                {
                    if (LoginTabs.SelectedIndex == 0)
                    {
                        StatusText.Text = string.IsNullOrWhiteSpace(status.Message) ? status.State.ToString() : status.Message;
                    }

                    await Task.Delay(1500, cancellationToken).ConfigureAwait(true);
                    continue;
                }

                if (status.State == ScanLoginState.Confirmed)
                {
                    StatusText.Text = "已确认，正在登录...";
                    string token = await scanLoginClient.ExchangeScanCodeAsync(status.ScanCode, cancellationToken).ConfigureAwait(true);
                    CompleteLogin(token);
                    return;
                }

                throw new InvalidOperationException(string.IsNullOrWhiteSpace(status.Message) ? "扫码状态异常" : status.Message);
            }
        }

        private async void SendCodeButtonClick(object sender, RoutedEventArgs e)
        {
            string phone = GetText(CodePhoneTextBox);
            if (!IsElevenDigitPhone(phone))
            {
                StatusText.Text = "请输入 11 位数字手机号";
                return;
            }

            SendCodeButton.IsEnabled = false;
            StatusText.Text = "正在发送验证码...";
            try
            {
                using (var client = new PhoneCodeLoginApiClient())
                {
                    await client.SendCodeAsync(phone, CancellationToken.None).ConfigureAwait(true);
                }

                StatusText.Text = "验证码已发送";
                StartCooldown();
            }
            catch (Exception ex)
            {
                StatusText.Text = ex is InvalidOperationException ? ex.Message : "发送验证码失败";
                SendCodeButton.IsEnabled = true;
            }
        }

        private async void CodeLoginButtonClick(object sender, RoutedEventArgs e)
        {
            string phone = GetText(CodePhoneTextBox);
            string code = GetText(CodeTextBox);
            if (!IsElevenDigitPhone(phone))
            {
                StatusText.Text = "请输入 11 位数字手机号";
                return;
            }

            if (!IsSixDigitCode(code))
            {
                StatusText.Text = "请输入 6 位数字验证码";
                return;
            }

            CodeLoginButton.IsEnabled = false;
            StatusText.Text = "正在登录...";
            try
            {
                using (var client = new PhoneCodeLoginApiClient())
                {
                    string token = await client.LoginAsync(phone, code, CancellationToken.None).ConfigureAwait(true);
                    CompleteLogin(token);
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = ex is InvalidOperationException ? ex.Message : "手机验证码登录失败";
            }
            finally
            {
                CodeLoginButton.IsEnabled = true;
            }
        }

        private async void PasswordLoginButtonClick(object sender, RoutedEventArgs e)
        {
            string phone = GetText(PasswordPhoneTextBox);
            string password = PasswordBox.Password;
            if (!IsElevenDigitPhone(phone))
            {
                StatusText.Text = "请输入 11 位数字手机号";
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                StatusText.Text = "请输入密码";
                return;
            }

            PasswordLoginButton.IsEnabled = false;
            StatusText.Text = "正在登录...";
            try
            {
                using (var client = new PhonePasswordLoginApiClient())
                {
                    string token = await client.LoginAsync(phone, password, CancellationToken.None).ConfigureAwait(true);
                    CompleteLogin(token);
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = ex is InvalidOperationException ? ex.Message : "帐号密码登录失败";
            }
            finally
            {
                PasswordLoginButton.IsEnabled = true;
            }
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void CompleteLogin(string token)
        {
            Token = token == null ? string.Empty : token.Trim();
            if (string.IsNullOrWhiteSpace(Token))
            {
                StatusText.Text = "登录失败";
                return;
            }

            DialogResult = true;
        }

        private void CancelScanLogin()
        {
            if (scanCancellationTokenSource != null)
            {
                scanCancellationTokenSource.Cancel();
                scanCancellationTokenSource.Dispose();
                scanCancellationTokenSource = null;
            }

            if (scanLoginClient != null)
            {
                scanLoginClient.Dispose();
                scanLoginClient = null;
            }
        }

        private void CooldownTimerTick(object sender, EventArgs e)
        {
            cooldownSeconds--;
            if (cooldownSeconds <= 0)
            {
                cooldownTimer.Stop();
                SendCodeButton.Content = "发送验证码";
                SendCodeButton.IsEnabled = true;
                return;
            }

            SendCodeButton.Content = cooldownSeconds.ToString(CultureInfo.InvariantCulture) + " 秒";
        }

        private void StartCooldown()
        {
            cooldownSeconds = 60;
            SendCodeButton.Content = "60 秒";
            SendCodeButton.IsEnabled = false;
            cooldownTimer.Stop();
            cooldownTimer.Start();
        }

        private static string GetText(TextBox textBox)
        {
            return textBox.Text == null ? string.Empty : textBox.Text.Trim();
        }

        private static bool IsSixDigitCode(string code)
        {
            if (code == null || code.Length != 6)
            {
                return false;
            }

            for (int i = 0; i < code.Length; i++)
            {
                if (!char.IsDigit(code[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsElevenDigitPhone(string phone)
        {
            if (phone == null || phone.Length != 11)
            {
                return false;
            }

            for (int i = 0; i < phone.Length; i++)
            {
                if (!char.IsDigit(phone[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private void DigitsOnlyPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsAllDigits(e.Text);
        }

        private void DigitsOnlyTextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox == null)
            {
                return;
            }

            string filtered = FilterDigits(textBox.Text, textBox.MaxLength);
            if (!string.Equals(textBox.Text, filtered, StringComparison.Ordinal))
            {
                textBox.Text = filtered;
                textBox.CaretIndex = textBox.Text.Length;
            }
        }

        private void DigitsOnlyPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            string text = e.DataObject.GetData(DataFormats.Text) as string;
            if (!IsAllDigits(text))
            {
                e.CancelCommand();
            }
        }

        private static bool IsAllDigits(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            for (int i = 0; i < text.Length; i++)
            {
                if (!char.IsDigit(text[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static string FilterDigits(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var chars = new char[text.Length];
            int count = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsDigit(text[i]))
                {
                    chars[count++] = text[i];
                    if (maxLength > 0 && count >= maxLength)
                    {
                        break;
                    }
                }
            }

            return new string(chars, 0, count);
        }

        private static BitmapImage CreateQrImage(string content)
        {
            using (var generator = new QRCodeGenerator())
            using (QRCodeData data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q))
            using (var renderer = new PngByteQRCode(data))
            {
                byte[] bytes = renderer.GetGraphic(10);
                var image = new BitmapImage();
                using (var stream = new MemoryStream(bytes))
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                    image.Freeze();
                }

                return image;
            }
        }
    }
}
