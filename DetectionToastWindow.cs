using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace endfield_player_position_display
{
    public sealed class DetectionToastWindow : Window
    {
        private readonly TextBlock textBlock;
        private readonly DispatcherTimer timer = new DispatcherTimer();

        public DetectionToastWindow()
        {
            Width = 260;
            Height = 76;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Topmost = true;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowActivated = false;

            textBlock = new TextBlock
            {
                Foreground = Brushes.White,
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };

            Content = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(232, 32, 33, 36)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(14, 10, 14, 10),
                Child = textBlock
            };

            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += (sender, args) =>
            {
                timer.Stop();
                Hide();
            };
        }

        public void ShowMessage(string message, double left, double top)
        {
            textBlock.Text = message;
            Left = left;
            Top = top;
            Show();
            timer.Stop();
            timer.Start();
        }
    }
}
