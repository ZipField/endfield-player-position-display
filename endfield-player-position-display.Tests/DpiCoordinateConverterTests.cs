using System.Windows;
using System.Windows.Media;
using endfield_player_position_display.Services;

namespace endfield_player_position_display.Tests
{
    internal static class DpiCoordinateConverterTests
    {
        public static void FromDevicePixelsConvertsRectToDips()
        {
            var transform = new Matrix(2.0 / 3.0, 0, 0, 2.0 / 3.0, 0, 0);
            var deviceRect = new Rect(150, 300, 900, 600);

            Rect dips = DpiCoordinateConverter.FromDevicePixels(deviceRect, transform);

            TestAssert.AreEqual(100.0, dips.Left);
            TestAssert.AreEqual(200.0, dips.Top);
            TestAssert.AreEqual(600.0, dips.Width);
            TestAssert.AreEqual(400.0, dips.Height);
        }
    }
}
