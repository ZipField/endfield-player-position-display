using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace endfield_player_position_display.Services
{
    public sealed class ClipboardTextService
    {
        private const int ClipboardBusyHResult = unchecked((int)0x800401D0);
        private readonly Action<string, bool> setDataObject;
        private readonly Action flush;
        private readonly Action<Action> runAsync;
        private readonly int delayMilliseconds;

        public ClipboardTextService()
            : this(
                (text, copy) => Clipboard.SetDataObject(text, copy),
                Clipboard.Flush,
                StartBackgroundStaThread,
                40)
        {
        }

        internal ClipboardTextService(
            Action<string, bool> setDataObject,
            Action flush,
            Action<Action> runAsync,
            int delayMilliseconds)
        {
            this.setDataObject = setDataObject;
            this.flush = flush;
            this.runAsync = runAsync;
            this.delayMilliseconds = delayMilliseconds;
        }

        public bool TrySetText(string text, out string error)
        {
            error = null;
            for (int attempt = 1; attempt <= 5; attempt++)
            {
                try
                {
                    setDataObject(text, false);
                    StartFlushAsync();
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

        private void StartFlushAsync()
        {
            try
            {
                runAsync(TryFlush);
            }
            catch
            {
            }
        }

        private void TryFlush()
        {
            try
            {
                flush();
            }
            catch
            {
            }
        }

        private static void StartBackgroundStaThread(Action action)
        {
            var thread = new Thread(() => action());
            thread.IsBackground = true;
            thread.Name = "Clipboard flush";
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
    }
}
