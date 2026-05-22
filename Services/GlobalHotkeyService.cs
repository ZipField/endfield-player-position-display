using System;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace endfield_player_position_display.Services
{
    public sealed class GlobalHotkeyService : IDisposable
    {
        private const int HotkeyId = 0x4546;
        private const int WmHotkey = 0x0312;
        private HwndSource source;
        private IntPtr handle;
        private Action pressed;
        private bool registered;

        public bool Register(IntPtr windowHandle, Key key, Action onPressed)
        {
            Unregister();
            handle = windowHandle;
            pressed = onPressed;
            source = HwndSource.FromHwnd(handle);
            if (source != null)
            {
                source.AddHook(WndProc);
            }

            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            registered = RegisterHotKey(handle, HotkeyId, 0, virtualKey);
            return registered;
        }

        public void Unregister()
        {
            if (registered)
            {
                UnregisterHotKey(handle, HotkeyId);
                registered = false;
            }

            if (source != null)
            {
                source.RemoveHook(WndProc);
                source = null;
            }
        }

        public void Dispose()
        {
            Unregister();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
            {
                pressed?.Invoke();
                handled = true;
            }

            return IntPtr.Zero;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
