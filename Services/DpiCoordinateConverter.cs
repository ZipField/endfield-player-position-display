using System.Windows;
using System.Windows.Media;

namespace endfield_player_position_display.Services
{
    public static class DpiCoordinateConverter
    {
        public static Rect FromDevicePixels(Rect deviceRect, Matrix transformFromDevice)
        {
            Point topLeft = transformFromDevice.Transform(new Point(deviceRect.Left, deviceRect.Top));
            Point bottomRight = transformFromDevice.Transform(new Point(deviceRect.Right, deviceRect.Bottom));
            return new Rect(topLeft, bottomRight);
        }
    }
}
