using System;
using System.Windows;

namespace endfield_player_position_display.Services
{
    public static class FollowWindowPlacement
    {
        private const double BaseMargin = 8;

        public static Point Calculate(Rect gameRect, Size windowSize, string position, double horizontalOffset, double verticalOffset)
        {
            double left = gameRect.Left + (gameRect.Width - windowSize.Width) / 2;
            double top = gameRect.Top + (gameRect.Height - windowSize.Height) / 2;

            switch (position)
            {
                case "左上":
                    left = gameRect.Left + BaseMargin + horizontalOffset;
                    top = gameRect.Top + BaseMargin + verticalOffset;
                    break;
                case "右上":
                    left = gameRect.Right - windowSize.Width - BaseMargin - horizontalOffset;
                    top = gameRect.Top + BaseMargin + verticalOffset;
                    break;
                case "正左":
                    left = gameRect.Left + BaseMargin + horizontalOffset;
                    top = gameRect.Top + (gameRect.Height - windowSize.Height) / 2 + verticalOffset;
                    break;
                case "正右":
                    left = gameRect.Right - windowSize.Width - BaseMargin - horizontalOffset;
                    top = gameRect.Top + (gameRect.Height - windowSize.Height) / 2 + verticalOffset;
                    break;
                case "左下":
                    left = gameRect.Left + BaseMargin + horizontalOffset;
                    top = gameRect.Bottom - windowSize.Height - BaseMargin - 30 - verticalOffset;
                    break;
                case "右下":
                    left = gameRect.Right - windowSize.Width - BaseMargin - horizontalOffset;
                    top = gameRect.Bottom - windowSize.Height - BaseMargin - verticalOffset;
                    break;
                case "正下":
                    left = gameRect.Left + (gameRect.Width - windowSize.Width) / 2 + horizontalOffset;
                    top = gameRect.Bottom - windowSize.Height - BaseMargin - verticalOffset;
                    break;
                default:
                    left = gameRect.Left + (gameRect.Width - windowSize.Width) / 2 + horizontalOffset;
                    top = gameRect.Top + BaseMargin + verticalOffset;
                    break;
            }

            return new Point(Math.Round(left, 4), Math.Round(top, 4));
        }
    }
}
