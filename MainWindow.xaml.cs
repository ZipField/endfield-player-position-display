using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using endfield_player_position_display.Models;
using endfield_player_position_display.Services;
using endfield_player_position_display.ViewModels;

namespace endfield_player_position_display
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel viewModel = new MainViewModel();
        private readonly UserSettingsStore settingsStore = new UserSettingsStore();
        private readonly SklandApiClient apiClient = new SklandApiClient();
        private readonly ClipboardTextService clipboardTextService = new ClipboardTextService();
        private readonly GameWindowLocator gameWindowLocator = new GameWindowLocator();
        private readonly GlobalHotkeyService hotkeyService = new GlobalHotkeyService();
        private readonly DispatcherTimer followTimer = new DispatcherTimer();
        private PositionMonitorService monitorService;
        private CoordinateWindow coordinateWindow;
        private bool suppressControlEvents;
        private bool capturingHotkey;
        private bool coordinateWindowFollowMode;
        private bool isClosing;
        private bool isConnecting;
        private int monitorRunId;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = viewModel;
            Loaded += MainWindowLoaded;
            Closed += MainWindowClosed;
            PreviewKeyDown += MainWindowPreviewKeyDown;
            followTimer.Interval = TimeSpan.FromMilliseconds(500);
            followTimer.Tick += FollowTimerTick;
        }

        private async void MainWindowLoaded(object sender, RoutedEventArgs e)
        {
            viewModel.ApplySettings(settingsStore.Load());
            UpdateMainUi();
            RegisterHotkey();
            ApplyCoordinateWindowState();
            await StartMonitorAsync();
        }

        private void MainWindowClosed(object sender, EventArgs e)
        {
            isClosing = true;
            SaveSettings();
            followTimer.Stop();
            hotkeyService.Dispose();
            if (coordinateWindow != null)
            {
                coordinateWindow.Closed -= CoordinateWindowClosed;
                coordinateWindow.Close();
                coordinateWindow = null;
            }

            if (monitorService != null)
            {
                monitorService.Dispose();
                monitorService = null;
            }

            apiClient.Dispose();
        }

        private void ApplyUpdate(MonitorUpdate update)
        {
            Dispatcher.Invoke(() =>
            {
                viewModel.ApplyMonitorUpdate(update);
                if (update.IsError || update.SessionState != null || update.Position != null)
                {
                    isConnecting = false;
                }

                UpdateMainUi();
            });
        }

        private async void ReconnectButtonClick(object sender, RoutedEventArgs e)
        {
            await StartMonitorAsync();
        }

        private void CoordinateWindowChecked(object sender, RoutedEventArgs e)
        {
            if (suppressControlEvents)
            {
                return;
            }

            viewModel.IsCoordinateWindowOpen = true;
            ApplyCoordinateWindowState();
            SaveSettings();
        }

        private void CoordinateWindowUnchecked(object sender, RoutedEventArgs e)
        {
            if (suppressControlEvents)
            {
                return;
            }

            viewModel.IsCoordinateWindowOpen = false;
            ApplyCoordinateWindowState();
            SaveSettings();
        }

        private void FollowGameChanged(object sender, RoutedEventArgs e)
        {
            if (suppressControlEvents)
            {
                return;
            }

            viewModel.FollowGameWindow = FollowGameCheckBox.IsChecked == true;
            RecreateCoordinateWindowIfOpen();
            UpdateFollowTimer();
            SaveSettings();
            UpdateMainUi();
        }

        private void FollowPositionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (suppressControlEvents)
            {
                return;
            }

            viewModel.FollowPosition = GetSelectedFollowPosition();
            UpdateFollowPosition();
            SaveSettings();
        }

        private void CaptureHotkeyButtonClick(object sender, RoutedEventArgs e)
        {
            capturingHotkey = true;
            CaptureHotkeyButton.Content = "按一个键...";
            WarningText.Text = "请按下一个键作为切换坐标窗口的快捷键";
            Focus();
        }

        private void MainWindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!capturingHotkey)
            {
                return;
            }

            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key == Key.None || key == Key.LeftAlt || key == Key.RightAlt || key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftShift || key == Key.RightShift)
            {
                return;
            }

            capturingHotkey = false;
            viewModel.Hotkey = key;
            RegisterHotkey();
            SaveSettings();
            UpdateMainUi();
            e.Handled = true;
        }

        private async void LookupZiplineButtonClick(object sender, RoutedEventArgs e)
        {
            if (viewModel.CurrentPosition == null)
            {
                viewModel.ZiplineResult = ZiplineLookupResult.NotFound();
                ZiplineResultText.Text = "缺少当前位置，连接成功并获取坐标后再试";
                UpdateCopyButtons();
                return;
            }

            if (string.IsNullOrWhiteSpace(viewModel.MapId))
            {
                ZiplineResultText.Text = "缺少当前地图 ID，等待坐标同步后再试";
                UpdateCopyButtons();
                return;
            }

            if (viewModel.Credential == null || viewModel.RoleBinding == null)
            {
                ZiplineResultText.Text = "缺少登录或角色信息，连接成功后再试";
                UpdateCopyButtons();
                return;
            }

            LookupZiplineButton.IsEnabled = false;
            ZiplineResultText.Text = "正在获取滑索坐标...";
            try
            {
                var marks = await apiClient.GetZiplineMarksAsync(
                    viewModel.Credential,
                    viewModel.MapId,
                    viewModel.RoleBinding,
                    System.Threading.CancellationToken.None);
                viewModel.ZiplineResult = ZiplineMatcher.FindNearest(viewModel.CurrentPosition, marks);
                ZiplineResultText.Text = viewModel.ZiplineResult.Found
                    ? viewModel.ZiplineResult.ToTupleText()
                    : viewModel.ZiplineResult.Message;
            }
            catch (Exception ex)
            {
                ZiplineResultText.Text = ex is InvalidOperationException ? ex.Message : "获取滑索坐标失败";
                viewModel.ZiplineResult = null;
            }
            finally
            {
                LookupZiplineButton.IsEnabled = true;
                UpdateCopyButtons();
            }
        }

        private void CopyTupleButtonClick(object sender, RoutedEventArgs e)
        {
            if (viewModel.ZiplineResult != null && viewModel.ZiplineResult.Found)
            {
                CopyText(viewModel.ZiplineResult.ToTupleText());
            }
        }

        private void CopyJsonButtonClick(object sender, RoutedEventArgs e)
        {
            if (viewModel.ZiplineResult != null && viewModel.ZiplineResult.Found)
            {
                CopyText(viewModel.ZiplineResult.ToJsonText());
            }
        }

        private void CopyText(string text)
        {
            string error;
            if (clipboardTextService.TrySetText(text, out error))
            {
                viewModel.WarningText = "已复制到剪贴板";
            }
            else
            {
                viewModel.WarningText = error;
            }

            UpdateMainUi();
        }

        private void ToggleCoordinateWindow()
        {
            viewModel.IsCoordinateWindowOpen = !viewModel.IsCoordinateWindowOpen;
            ApplyCoordinateWindowState();
            SaveSettings();
            UpdateMainUi();
        }

        private void ApplyCoordinateWindowState()
        {
            if (viewModel.IsCoordinateWindowOpen)
            {
                if (coordinateWindow == null)
                {
                    coordinateWindowFollowMode = viewModel.FollowGameWindow;
                    coordinateWindow = new CoordinateWindow(viewModel, coordinateWindowFollowMode);
                    coordinateWindow.Closed += CoordinateWindowClosed;
                }

                if (!coordinateWindow.IsVisible)
                {
                    coordinateWindow.Show();
                }

                UpdateFollowPosition();
            }
            else if (coordinateWindow != null)
            {
                coordinateWindow.Closed -= CoordinateWindowClosed;
                coordinateWindow.Close();
                coordinateWindow = null;
            }

            UpdateFollowTimer();
            UpdateMainUi();
        }

        private void CoordinateWindowClosed(object sender, EventArgs e)
        {
            coordinateWindow.Closed -= CoordinateWindowClosed;
            coordinateWindow = null;
            if (isClosing)
            {
                return;
            }

            viewModel.IsCoordinateWindowOpen = false;
            UpdateFollowTimer();
            UpdateMainUi();
            SaveSettings();
        }

        private void RecreateCoordinateWindowIfOpen()
        {
            if (coordinateWindow == null)
            {
                return;
            }

            coordinateWindow.Closed -= CoordinateWindowClosed;
            coordinateWindow.Close();
            coordinateWindow = null;
            coordinateWindowFollowMode = viewModel.FollowGameWindow;
            coordinateWindow = new CoordinateWindow(viewModel, coordinateWindowFollowMode);
            coordinateWindow.Closed += CoordinateWindowClosed;
            coordinateWindow.Show();
            UpdateFollowPosition();
        }

        private void FollowTimerTick(object sender, EventArgs e)
        {
            UpdateFollowPosition();
        }

        private void UpdateFollowTimer()
        {
            if (viewModel.IsCoordinateWindowOpen && viewModel.FollowGameWindow)
            {
                if (!followTimer.IsEnabled)
                {
                    followTimer.Start();
                }
            }
            else
            {
                followTimer.Stop();
            }
        }

        private void UpdateFollowPosition()
        {
            if (coordinateWindow == null || !viewModel.FollowGameWindow)
            {
                return;
            }

            Rect gameRect;
            if (!gameWindowLocator.TryGetEndfieldWindowRect(out gameRect))
            {
                viewModel.WarningText = "未找到 endfield.exe 游戏窗口";
                UpdateMainUi();
                return;
            }

            viewModel.WarningText = string.Empty;
            double margin = 8;
            double left = gameRect.Left;
            double top = gameRect.Top;
            coordinateWindow.UpdateLayout();
            double width = coordinateWindow.ActualWidth > 0 ? coordinateWindow.ActualWidth : coordinateWindow.Width;
            double height = coordinateWindow.ActualHeight > 0 ? coordinateWindow.ActualHeight : 70;

            switch (viewModel.FollowPosition)
            {
                case "正左":
                    left = gameRect.Left + margin;
                    top = gameRect.Top + (gameRect.Height - height) / 2;
                    break;
                case "正下":
                    left = gameRect.Left + (gameRect.Width - width) / 2;
                    top = gameRect.Bottom - height - margin;
                    break;
                case "右下":
                    left = gameRect.Right - width - margin;
                    top = gameRect.Bottom - height - margin;
                    break;
                case "左下":
                    left = gameRect.Left + margin;
                    top = gameRect.Bottom - height - margin - 30;
                    break;
                default:
                    left = gameRect.Left + (gameRect.Width - width) / 2;
                    top = gameRect.Top + margin;
                    break;
            }

            coordinateWindow.Left = left;
            coordinateWindow.Top = top;
            UpdateMainUi();
        }

        private void RegisterHotkey()
        {
            var helper = new WindowInteropHelper(this);
            if (helper.Handle == IntPtr.Zero)
            {
                return;
            }

            bool ok = hotkeyService.Register(helper.Handle, viewModel.Hotkey, ToggleCoordinateWindow);
            viewModel.WarningText = ok ? string.Empty : "快捷键被占用，注册失败";
        }

        private void SaveSettings()
        {
            settingsStore.Save(viewModel.ToSettings());
        }

        private async Task StartMonitorAsync()
        {
            if (isConnecting)
            {
                return;
            }

            isConnecting = true;
            int runId = ++monitorRunId;
            viewModel.StatusText = "正在连接...";
            viewModel.WarningText = string.Empty;
            viewModel.CurrentPosition = null;
            viewModel.Credential = null;
            viewModel.RoleBinding = null;
            viewModel.MapId = null;
            UpdateMainUi();

            if (monitorService != null)
            {
                monitorService.Dispose();
                monitorService = null;
            }

            monitorService = new PositionMonitorService(AppDomain.CurrentDomain.BaseDirectory, ApplyUpdate);
            await Task.Run(() => monitorService.StartAsync());
            if (runId == monitorRunId)
            {
                isConnecting = false;
                UpdateMainUi();
            }
        }

        private void UpdateMainUi()
        {
            suppressControlEvents = true;
            CoordinateWindowCheckBox.IsChecked = viewModel.IsCoordinateWindowOpen;
            FollowGameCheckBox.IsChecked = viewModel.FollowGameWindow;
            HotkeyText.Text = viewModel.Hotkey.ToString();
            CaptureHotkeyButton.Content = capturingHotkey ? "按一个键..." : "设置快捷键";
            ReconnectButton.IsEnabled = !isConnecting;
            SelectFollowPosition(viewModel.FollowPosition);
            suppressControlEvents = false;

            StatusText.Text = string.IsNullOrWhiteSpace(viewModel.StatusText) ? "正在连接..." : viewModel.StatusText;
            WarningText.Text = viewModel.WarningText ?? string.Empty;
            if (viewModel.CurrentPosition == null)
            {
                PositionText.Text = "X: -  Y: -  Z: -";
            }
            else
            {
                PositionText.Text = string.Format(
                    CultureInfo.InvariantCulture,
                    "X: {0}  Y: {1}  Z: {2}",
                    CoordinateFormatter.Format(viewModel.CurrentPosition.X),
                    CoordinateFormatter.Format(viewModel.CurrentPosition.Y),
                    CoordinateFormatter.Format(viewModel.CurrentPosition.Z));
            }

            if (viewModel.ZiplineResult == null)
            {
                ZiplineResultText.Text = string.Empty;
            }
            else if (viewModel.ZiplineResult.Found)
            {
                ZiplineResultText.Text = viewModel.ZiplineResult.ToTupleText();
            }
            else
            {
                ZiplineResultText.Text = viewModel.ZiplineResult.Message;
            }

            UpdateCopyButtons();
        }

        private void UpdateCopyButtons()
        {
            bool canCopy = viewModel.ZiplineResult != null && viewModel.ZiplineResult.Found;
            CopyTupleButton.IsEnabled = canCopy;
            CopyJsonButton.IsEnabled = canCopy;
        }

        private string GetSelectedFollowPosition()
        {
            var item = FollowPositionComboBox.SelectedItem as ComboBoxItem;
            return item == null ? "正上" : Convert.ToString(item.Content);
        }

        private void SelectFollowPosition(string value)
        {
            string target = string.IsNullOrWhiteSpace(value) ? "正上" : value;
            foreach (object item in FollowPositionComboBox.Items)
            {
                var comboBoxItem = item as ComboBoxItem;
                if (comboBoxItem != null && string.Equals(Convert.ToString(comboBoxItem.Content), target, StringComparison.Ordinal))
                {
                    FollowPositionComboBox.SelectedItem = comboBoxItem;
                    return;
                }
            }

            FollowPositionComboBox.SelectedIndex = 0;
        }
    }
}
