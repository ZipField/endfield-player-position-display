using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using endfield_player_position_display.Services;
using endfield_player_position_display.ViewModels;

namespace endfield_player_position_display
{
    public partial class CoordinateWindow : Window
    {
        private readonly MainViewModel viewModel;
        private readonly bool followMode;

        public CoordinateWindow(MainViewModel viewModel, bool followMode)
        {
            InitializeComponent();
            this.viewModel = viewModel;
            this.followMode = followMode;
            DataContext = viewModel;
            viewModel.PropertyChanged += ViewModelPropertyChanged;
            Closed += CoordinateWindowClosed;
            ApplyWindowStyle();
            ApplyContent();
        }

        private void ApplyWindowStyle()
        {
            ShowInTaskbar = false;
            ShowActivated = false;
            Topmost = !followMode;
            if (followMode)
            {
                WindowStyle = WindowStyle.None;
                AllowsTransparency = true;
                Background = Brushes.Transparent;
                RootBorder.Background = Brushes.Transparent;
                RootBorder.Padding = new Thickness(4);
                ApplyFontScale(1.2);
            }
            else
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                AllowsTransparency = false;
                Background = new SolidColorBrush(Color.FromRgb(32, 33, 36));
                RootBorder.Background = Background;
                RootBorder.Padding = new Thickness(10);
                ApplyFontScale(1.0);
            }
        }

        private void ApplyFontScale(double scale)
        {
            StatusText.FontSize = 12 * scale;
            foreach (UIElement child in PositionPanel.Children)
            {
                var textBlock = child as System.Windows.Controls.TextBlock;
                if (textBlock != null)
                {
                    textBlock.FontSize = 12 * scale;
                }
            }
        }

        private void ViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Dispatcher.Invoke(ApplyContent);
        }

        private void ApplyContent()
        {
            Brush foreground = followMode ? Brushes.White : new SolidColorBrush(Color.FromRgb(232, 234, 237));
            Brush labelForeground = followMode ? Brushes.White : new SolidColorBrush(Color.FromRgb(154, 160, 166));
            StatusText.Foreground = viewModel.IsError ? new SolidColorBrush(Color.FromRgb(255, 180, 171)) : foreground;
            StatusText.Effect = followMode ? CreateTextOutlineEffect() : null;

            foreach (UIElement child in PositionPanel.Children)
            {
                var textBlock = child as System.Windows.Controls.TextBlock;
                if (textBlock != null)
                {
                    textBlock.Foreground = textBlock.Name == "XText" || textBlock.Name == "YText" || textBlock.Name == "ZText"
                        ? foreground
                        : labelForeground;
                    if (followMode)
                    {
                        textBlock.Effect = CreateTextOutlineEffect();
                    }
                    else
                    {
                        textBlock.Effect = null;
                    }
                }
            }

            if (viewModel.CurrentPosition == null)
            {
                StatusText.Text = string.IsNullOrWhiteSpace(viewModel.StatusText) ? "正在连接..." : viewModel.StatusText;
                StatusText.Visibility = Visibility.Visible;
                PositionPanel.Visibility = Visibility.Collapsed;
                return;
            }

            StatusText.Visibility = Visibility.Collapsed;
            PositionPanel.Visibility = Visibility.Visible;
            XText.Text = CoordinateFormatter.Format(viewModel.CurrentPosition.X);
            YText.Text = CoordinateFormatter.Format(viewModel.CurrentPosition.Y);
            ZText.Text = CoordinateFormatter.Format(viewModel.CurrentPosition.Z);
        }

        private static Effect CreateTextOutlineEffect()
        {
            return new DropShadowEffect
            {
                Color = Color.FromRgb(25, 25, 25),
                BlurRadius = 5,
                ShadowDepth = 0,
                Opacity = 1
            };
        }

        private void CoordinateWindowClosed(object sender, EventArgs e)
        {
            viewModel.PropertyChanged -= ViewModelPropertyChanged;
        }
    }
}
