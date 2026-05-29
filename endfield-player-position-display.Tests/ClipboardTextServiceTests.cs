using System;
using System.Runtime.InteropServices;
using endfield_player_position_display.Services;

namespace endfield_player_position_display.Tests
{
    internal static class ClipboardTextServiceTests
    {
        public static void TrySetTextSetsDataObjectWithoutFlushAndSchedulesFlush()
        {
            string copiedText = null;
            bool? copyFlag = null;
            bool scheduled = false;
            bool flushed = false;
            var service = new ClipboardTextService(
                (text, copy) =>
                {
                    copiedText = text;
                    copyFlag = copy;
                },
                () => flushed = true,
                action => scheduled = true,
                delayMilliseconds: 0);

            string error;
            bool copied = service.TrySetText("abc", out error);

            TestAssert.AreEqual(true, copied);
            TestAssert.AreEqual(null, error);
            TestAssert.AreEqual("abc", copiedText);
            TestAssert.AreEqual(false, copyFlag);
            TestAssert.AreEqual(true, scheduled);
            TestAssert.AreEqual(false, flushed);
        }

        public static void TrySetTextIgnoresAsyncFlushFailure()
        {
            var service = new ClipboardTextService(
                (text, copy) => { },
                () => { throw new COMException("busy", unchecked((int)0x800401D0)); },
                action => action(),
                delayMilliseconds: 0);

            string error;
            bool copied = service.TrySetText("abc", out error);

            TestAssert.AreEqual(true, copied);
            TestAssert.AreEqual(null, error);
        }

        public static void TrySetTextRetriesWhenClipboardIsBusy()
        {
            int attempts = 0;
            var service = new ClipboardTextService(
                (text, copy) =>
                {
                    attempts++;
                    if (attempts < 3)
                    {
                        throw new COMException("busy", unchecked((int)0x800401D0));
                    }
                },
                () => { },
                action => { },
                delayMilliseconds: 0);

            string error;
            bool copied = service.TrySetText("abc", out error);

            TestAssert.AreEqual(true, copied);
            TestAssert.AreEqual(null, error);
            TestAssert.AreEqual(3, attempts);
        }

        public static void TrySetTextReturnsErrorWhenClipboardStaysBusy()
        {
            int attempts = 0;
            var service = new ClipboardTextService(
                (text, copy) =>
                {
                    attempts++;
                    throw new COMException("busy", unchecked((int)0x800401D0));
                },
                () => { },
                action => { },
                delayMilliseconds: 0);

            string error;
            bool copied = service.TrySetText("abc", out error);

            TestAssert.AreEqual(false, copied);
            TestAssert.AreEqual("剪贴板正被其他程序占用，请稍后重试", error);
            TestAssert.AreEqual(5, attempts);
        }
    }
}
