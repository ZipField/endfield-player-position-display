using endfield_player_position_display.Services;

namespace endfield_player_position_display.Tests
{
    internal static class SklandSignerTests
    {
        public static void CreateHeaderJsonUsesAcceptedClientHeaderValues()
        {
            string headerJson = SklandSigner.CreateHeaderJson("1779204182");

            TestAssert.AreEqual(
                "{\"platform\":\"\",\"timestamp\":\"1779204182\",\"dId\":\"\",\"vName\":\"\"}",
                headerJson);
        }

        public static void CreateSignReturnsMd5OfHmacSha256Hex()
        {
            string sign = SklandSigner.CreateSign(
                "/api/v1/example",
                string.Empty,
                "1779204182",
                "{\"platform\":\"3\",\"timestamp\":\"1779204182\",\"dId\":\"\",\"vName\":\"1.0.0\"}",
                "secret");

            TestAssert.AreEqual("87515cce47a113ab3bba900b3f2de3c5", sign);
        }

        public static void CreateSignIncludesQueryOrBodySegment()
        {
            string sign = SklandSigner.CreateSign(
                "/api/v1/example",
                "a=1&b=2",
                "1779204182",
                "{\"platform\":\"3\",\"timestamp\":\"1779204182\",\"dId\":\"\",\"vName\":\"1.0.0\"}",
                "secret");

            TestAssert.AreEqual("be840a71b77d605ce9d63b27ae279391", sign);
        }
    }
}
