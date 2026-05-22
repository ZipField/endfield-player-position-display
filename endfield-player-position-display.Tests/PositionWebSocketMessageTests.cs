using endfield_player_position_display.Services;

namespace endfield_player_position_display.Tests
{
    internal static class PositionWebSocketMessageTests
    {
        public static void ParseMessageExtractsPositionFromType1012()
        {
            string json = "{\"type\":1012,\"data\":{\"pos\":{\"x\":-502.3604,\"y\":96.402985,\"z\":-464.56967}},\"msgId\":\"abc\"}";

            PositionWebSocketMessage result = PositionWebSocketClient.ParseMessage(json);

            TestAssert.AreEqual(PositionWebSocketMessageKind.Position, result.Kind);
            TestAssert.AreNear(-502.3604, result.Position.X, 0.00001);
            TestAssert.AreNear(96.402985, result.Position.Y, 0.00001);
            TestAssert.AreNear(-464.56967, result.Position.Z, 0.00001);
        }

        public static void ParseMessageExtractsRemoteCloseMessageFromType6()
        {
            string json = "{\"type\":6,\"data\":{\"code\":19003,\"message\":\"服务器已关闭连接\"},\"msgId\":\"abc\"}";

            PositionWebSocketMessage result = PositionWebSocketClient.ParseMessage(json);

            TestAssert.AreEqual(PositionWebSocketMessageKind.RemoteClose, result.Kind);
            TestAssert.AreEqual("服务器已关闭连接", result.ErrorMessage);
        }

        public static void ParseMessageExtractsMapIdFromType1012()
        {
            string json = "{\"type\":1012,\"data\":{\"mapId\":\"map01\",\"pos\":{\"x\":1,\"y\":2,\"z\":3}},\"msgId\":\"abc\"}";

            PositionWebSocketMessage result = PositionWebSocketClient.ParseMessage(json);

            TestAssert.AreEqual(PositionWebSocketMessageKind.Position, result.Kind);
            TestAssert.AreEqual("map01", result.MapId);
        }

        public static void ParseMessageAllowsMissingMapId()
        {
            string json = "{\"type\":1012,\"data\":{\"pos\":{\"x\":1,\"y\":2,\"z\":3}},\"msgId\":\"abc\"}";

            PositionWebSocketMessage result = PositionWebSocketClient.ParseMessage(json);

            TestAssert.AreEqual(PositionWebSocketMessageKind.Position, result.Kind);
            TestAssert.AreEqual(null, result.MapId);
        }

        public static void ParseMessageUsesChineseErrorForInvalidPayload()
        {
            PositionWebSocketMessage result = PositionWebSocketClient.ParseMessage("{bad json");

            TestAssert.AreEqual(PositionWebSocketMessageKind.RemoteClose, result.Kind);
            TestAssert.AreEqual("收到无效的 WebSocket 消息", result.ErrorMessage);
        }

    }
}
