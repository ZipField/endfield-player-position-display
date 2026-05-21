using System;
using endfield_player_position_display.Services;

namespace endfield_player_position_display.Tests
{
    internal static class SklandApiParsingTests
    {
        public static void CreateSignedRequestTimestampUsesServerAcceptedClockSkew()
        {
            var now = DateTimeOffset.FromUnixTimeSeconds(1779204182);

            string timestamp = SklandApiClient.CreateSignedRequestTimestamp(now);

            TestAssert.AreEqual("1779204179", timestamp);
        }

        public static void ParseRoleBindingExtractsFirstEndfieldDefaultRole()
        {
            string json = "{\"code\":0,\"message\":\"OK\",\"data\":{\"list\":[{\"appCode\":\"other\",\"bindingList\":[]},{\"appCode\":\"endfield\",\"bindingList\":[{\"defaultRole\":{\"serverId\":\"1\",\"roleId\":\"1538309069\"}}]}]}}";

            var binding = SklandApiClient.ParseRoleBinding(json);

            TestAssert.AreEqual("1", binding.ServerId);
            TestAssert.AreEqual("1538309069", binding.RoleId);
        }

        public static void ParseRoleBindingThrowsChineseErrorWhenRoleMissing()
        {
            string json = "{\"code\":0,\"message\":\"OK\",\"data\":{\"list\":[{\"appCode\":\"endfield\",\"bindingList\":[]}]}}";

            InvalidOperationException ex = TestAssert.Throws<InvalidOperationException>(
                () => SklandApiClient.ParseRoleBinding(json));

            TestAssert.AreEqual("未找到终末地角色", ex.Message);
        }

        public static void ParseWebSocketTokenExtractsDataToken()
        {
            string json = "{\"code\":0,\"message\":\"OK\",\"data\":{\"token\":\"ws-token\"}}";

            string token = SklandApiClient.ParseWebSocketToken(json);

            TestAssert.AreEqual("ws-token", token);
        }
    }
}
