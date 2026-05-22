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

                NativeRect clientRect;
                NativePoint clientTopLeft = new NativePoint();
                if (GetClientRect(handle, out clientRect) && ClientToScreen(handle, ref clientTopLeft))
                {
                    rect = new Rect(
                        clientTopLeft.X,
                        clientTopLeft.Y,
                        clientRect.Right - clientRect.Left,
                        clientRect.Bottom - clientRect.Top);
                    return true;
                }

                NativeRect windowRect;
                if (GetWindowRect(handle, out windowRect))
                {
                    rect = new Rect(
                        windowRect.Left,
                        windowRect.Top,
                        windowRect.Right - windowRect.Left,
                        windowRect.Bottom - windowRect.Top);
                    return true;
                }
            }

            return false;
        }

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hwnd, out NativeRect rect);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hwnd, ref NativePoint point);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            public int X;
            public int Y;
        }
    }
}
