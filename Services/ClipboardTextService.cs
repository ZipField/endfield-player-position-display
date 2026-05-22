using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace endfield_player_position_display.Services
{
    public sealed class ClipboardTextService
    {
        private const int ClipboardBusyHResult = unchecked((int)0x800401D0);
        private readonly Action<string> setText;
        private readonly int delayMilliseconds;

        public ClipboardTextService()
            : this(Clipboard.SetText, 40)
        {
        }

        internal ClipboardTextService(Action<string> setText, int delayMilliseconds)
        {
            this.setText = setText;
            this.delayMilliseconds = delayMilliseconds;
        }

        public bool TrySetText(string text, out string error)
        {
            error = null;
            for (int attempt = 1; attempt <= 5; attempt++)
            {
                try
                {
                    setText(text);
                    return true;
                }
                catch (COMException ex)
                {
                    if (ex.ErrorCode != ClipboardBusyHResult)
                    {
                        error = "复制失败";
                        return false;
                    }

                    if (attempt == 5)
                    {
                        error = "剪贴板正被其他程序占用，请稍后重试";
                        return false;
                    }

                    if (delayMilliseconds > 0)
                    {
                        Thread.Sleep(delayMilliseconds);
                    }
                }
            }

            error = "复制失败";
            return false;
        }
    }
}
