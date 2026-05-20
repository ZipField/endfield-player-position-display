using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using endfield_player_position_display.Models;

namespace endfield_player_position_display.Services
{
    public enum PositionWebSocketMessageKind
    {
        Unknown,
        Authenticated,
        HeartbeatAck,
        Position,
        RemoteClose
    }

    public sealed class PositionWebSocketMessage
    {
        private PositionWebSocketMessage(PositionWebSocketMessageKind kind, PositionSnapshot position, string errorMessage)
        {
            Kind = kind;
            Position = position;
            ErrorMessage = errorMessage;
        }

        public PositionWebSocketMessageKind Kind { get; }
        public PositionSnapshot Position { get; }
        public string ErrorMessage { get; }

        public static PositionWebSocketMessage Unknown()
        {
            return new PositionWebSocketMessage(PositionWebSocketMessageKind.Unknown, null, null);
        }

        public static PositionWebSocketMessage Authenticated()
        {
            return new PositionWebSocketMessage(PositionWebSocketMessageKind.Authenticated, null, null);
        }

        public static PositionWebSocketMessage HeartbeatAck()
        {
            return new PositionWebSocketMessage(PositionWebSocketMessageKind.HeartbeatAck, null, null);
        }

        public static PositionWebSocketMessage FromPosition(PositionSnapshot position)
        {
            return new PositionWebSocketMessage(PositionWebSocketMessageKind.Position, position, null);
        }

        public static PositionWebSocketMessage RemoteClose(string message)
        {
            return new PositionWebSocketMessage(PositionWebSocketMessageKind.RemoteClose, null, message);
        }
    }

    public sealed class PositionWebSocketClient
    {
        private static readonly Uri WebSocketUri = new Uri("wss://ws.skland.com/ws/v1/game/endfield/map");
        private static readonly char[] MsgIdChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer();

        public async Task RunAsync(
            string websocketToken,
            RoleBinding roleBinding,
            Action<PositionSnapshot> onPosition,
            Action<string> onError,
            CancellationToken cancellationToken)
        {
            using (var socket = new ClientWebSocket())
            {
                await socket.ConnectAsync(WebSocketUri, cancellationToken).ConfigureAwait(false);

                string authMsgId = CreateMsgId();
                await SendAsync(socket, CreateAuthMessage(websocketToken, authMsgId), cancellationToken).ConfigureAwait(false);

                bool subscribed = false;
                CancellationTokenSource heartbeatCancellation = null;
                Task heartbeatTask = null;

                try
                {
                    while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                    {
                        string text = await ReceiveTextAsync(socket, cancellationToken).ConfigureAwait(false);

                        PositionWebSocketMessage message = ParseMessage(text);
                        if (message.Kind == PositionWebSocketMessageKind.Authenticated && !subscribed)
                        {
                            subscribed = true;
                            await SendAsync(socket, CreateSubscribeMessage(roleBinding, CreateMsgId()), cancellationToken).ConfigureAwait(false);
                        }
                        else if (message.Kind == PositionWebSocketMessageKind.Position)
                        {
                            onPosition(message.Position);
                            if (heartbeatTask == null)
                            {
                                heartbeatCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                                heartbeatTask = RunHeartbeatAsync(socket, heartbeatCancellation.Token);
                            }
                        }
                        else if (message.Kind == PositionWebSocketMessageKind.RemoteClose)
                        {
                            onError(string.IsNullOrWhiteSpace(message.ErrorMessage) ? "WebSocket 已关闭" : message.ErrorMessage);
                            return;
                        }
                    }
                }
                catch (WebSocketException)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        onError("WebSocket 已关闭");
                    }
                }
                finally
                {
                    if (heartbeatCancellation != null)
                    {
                        heartbeatCancellation.Cancel();
                        heartbeatCancellation.Dispose();
                    }
                }
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                onError("WebSocket 已关闭");
            }
        }

        public static PositionWebSocketMessage ParseMessage(string json)
        {
            try
            {
                var root = Serializer.DeserializeObject(json) as IDictionary<string, object>;
                if (root == null || !root.ContainsKey("type"))
                {
                    return PositionWebSocketMessage.Unknown();
                }

                int type = Convert.ToInt32(root["type"]);
                if (type == 2)
                {
                    return PositionWebSocketMessage.Authenticated();
                }

                if (type == 4)
                {
                    return PositionWebSocketMessage.HeartbeatAck();
                }

                if (type == 1012)
                {
                    var data = GetObject(root, "data");
                    var pos = GetObject(data, "pos");
                    return PositionWebSocketMessage.FromPosition(new PositionSnapshot(
                        GetDouble(pos, "x"),
                        GetDouble(pos, "y"),
                        GetDouble(pos, "z")));
                }

                if (type == 6)
                {
                    var data = GetObject(root, "data");
                    return PositionWebSocketMessage.RemoteClose(GetString(data, "message") ?? "WebSocket 已关闭");
                }
            }
            catch
            {
                return PositionWebSocketMessage.RemoteClose("收到无效的 WebSocket 消息");
            }

            return PositionWebSocketMessage.Unknown();
        }

        private static string CreateAuthMessage(string websocketToken, string msgId)
        {
            var root = new Dictionary<string, object>
            {
                { "type", 1 },
                { "data", new Dictionary<string, object> { { "token", websocketToken } } },
                { "msgId", msgId }
            };
            return Serializer.Serialize(root);
        }

        private static string CreateSubscribeMessage(RoleBinding roleBinding, string msgId)
        {
            var root = new Dictionary<string, object>
            {
                { "type", 1011 },
                { "data", new Dictionary<string, object> { { "roleId", roleBinding.RoleId }, { "serverId", roleBinding.ServerId } } },
                { "msgId", msgId }
            };
            return Serializer.Serialize(root);
        }

        private static string CreateHeartbeatMessage(string msgId)
        {
            var root = new Dictionary<string, object>
            {
                { "type", 3 },
                { "data", new Dictionary<string, object>() },
                { "msgId", msgId }
            };
            return Serializer.Serialize(root);
        }

        private static string CreateMsgId()
        {
            var bytes = new byte[8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            var builder = new StringBuilder(8);
            foreach (byte value in bytes)
            {
                builder.Append(MsgIdChars[value % MsgIdChars.Length]);
            }

            return builder.ToString();
        }

        private static async Task RunHeartbeatAsync(ClientWebSocket socket, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
                if (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
                {
                    await SendAsync(socket, CreateHeartbeatMessage(CreateMsgId()), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private static async Task SendAsync(ClientWebSocket socket, string text, CancellationToken cancellationToken)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<string> ReceiveTextAsync(ClientWebSocket socket, CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];
            var builder = new StringBuilder();
            WebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return "{\"type\":6,\"data\":{\"message\":\"WebSocket 已关闭\"}}";
                }

                builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            }
            while (!result.EndOfMessage);

            return builder.ToString();
        }

        private static IDictionary<string, object> GetObject(IDictionary<string, object> obj, string key)
        {
            if (obj == null || !obj.ContainsKey(key))
            {
                return null;
            }

            return obj[key] as IDictionary<string, object>;
        }

        private static string GetString(IDictionary<string, object> obj, string key)
        {
            if (obj == null || !obj.ContainsKey(key) || obj[key] == null)
            {
                return null;
            }

            return Convert.ToString(obj[key]);
        }

        private static double GetDouble(IDictionary<string, object> obj, string key)
        {
            if (obj == null || !obj.ContainsKey(key) || obj[key] == null)
            {
                throw new InvalidOperationException("位置数据缺失");
            }

            return Convert.ToDouble(obj[key]);
        }
    }
}
