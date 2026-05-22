using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace endfield_player_position_display.Services
{
    public sealed class GameWindowLocator
    {
        public bool TryGetEndfieldWindowRect(out Rect rect)
        {
            rect = Rect.Empty;
            foreach (Process process in Process.GetProcessesByName("endfield"))
            {
                IntPtr handle = process.MainWindowHandle;
                if (handle == IntPtr.Zero)
                {
                    continue;
                }

                NativeRect nativeRect;
                if (GetWindowRect(handle, out nativeRect))
                {
                    rect = new Rect(
                        nativeRect.Left,
                        nativeRect.Top,
                        nativeRect.Right - nativeRect.Left,
                        nativeRect.Bottom - nativeRect.Top);
                    return true;
                }
            }

            return false;
        }

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}
