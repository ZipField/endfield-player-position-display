using System;
using System.Runtime.InteropServices;
using endfield_player_position_display.Services;

namespace endfield_player_position_display.Tests
{
    internal static class ClipboardTextServiceTests
    {
        public static void TrySetTextRetriesWhenClipboardIsBusy()
        {
            int attempts = 0;
            var service = new ClipboardTextService(text =>
            {
                attempts++;
                if (attempts < 3)
                {
                    throw new COMException("busy", unchecked((int)0x800401D0));
                }
            }, delayMilliseconds: 0);

            string error;
            bool copied = service.TrySetText("abc", out error);

            TestAssert.AreEqual(true, copied);
            TestAssert.AreEqual(null, error);
            TestAssert.AreEqual(3, attempts);
        }

        public static void TrySetTextReturnsErrorWhenClipboardStaysBusy()
        {
            int attempts = 0;
            var service = new ClipboardTextService(text =>
            {
                attempts++;
                throw new COMException("busy", unchecked((int)0x800401D0));
            }, delayMilliseconds: 0);

            string error;
            bool copied = service.TrySetText("abc", out error);

            TestAssert.AreEqual(false, copied);
            TestAssert.AreEqual("剪贴板正被其他程序占用，请稍后重试", error);
            TestAssert.AreEqual(5, attempts);
        }
    }
}
