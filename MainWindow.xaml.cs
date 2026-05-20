using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using endfield_player_position_display.Models;
using endfield_player_position_display.Services;

namespace endfield_player_position_display
{
    public partial class MainWindow : Window
    {
        private PositionMonitorService monitorService;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindowLoaded;
            Closed += MainWindowClosed;
        }

        private async void MainWindowLoaded(object sender, RoutedEventArgs e)
        {
            monitorService = new PositionMonitorService(AppDomain.CurrentDomain.BaseDirectory, ApplyUpdate);
            await Task.Run(() => monitorService.StartAsync());
        }

        private void MainWindowClosed(object sender, EventArgs e)
        {
            if (monitorService != null)
            {
                monitorService.Dispose();
                monitorService = null;
            }
        }

        private void ApplyUpdate(MonitorUpdate update)
        {
            Dispatcher.Invoke(() =>
            {
                if (update.IsError)
                {
                    StatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 180, 171));
                    StatusText.Text = update.Status;
                    StatusText.Visibility = Visibility.Visible;
                    PositionPanel.Visibility = Visibility.Collapsed;
                    return;
                }

                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(232, 234, 237));
                if (update.Position == null)
                {
                    StatusText.Text = update.Status;
                    StatusText.Visibility = Visibility.Visible;
                    PositionPanel.Visibility = Visibility.Collapsed;
                    return;
                }

                StatusText.Visibility = Visibility.Collapsed;
                PositionPanel.Visibility = Visibility.Visible;
                XText.Text = CoordinateFormatter.Format(update.Position.X);
                YText.Text = CoordinateFormatter.Format(update.Position.Y);
                ZText.Text = CoordinateFormatter.Format(update.Position.Z);
            });
        }
    }
}
