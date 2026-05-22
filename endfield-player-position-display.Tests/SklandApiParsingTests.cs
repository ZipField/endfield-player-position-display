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

        public static void ParseZiplineMarksFiltersSupportedTemplates()
        {
            string json = "{\"code\":0,\"data\":{\"saveMarks\":[{\"templateId\":\"ignored\",\"pos\":{\"x\":1,\"y\":2,\"z\":3}},{\"templateId\":\"0f45150a59b97bd0de9a4eed7a0fbf23\",\"pos\":{\"x\":10,\"y\":20,\"z\":30}},{\"templateId\":\"5d53bdb714ba42c1e1a1b748b55b686f\",\"pos\":{\"x\":11,\"y\":21,\"z\":31}}]}}";

            var marks = SklandApiClient.ParseZiplineMarks(json);

            TestAssert.AreEqual(2, marks.Count);
            TestAssert.AreEqual(10, marks[0].X);
            TestAssert.AreEqual(20, marks[0].Y);
            TestAssert.AreEqual(30, marks[0].Z);
            TestAssert.AreEqual(11, marks[1].X);
        }

        public static void ParseZiplineMarksThrowsChineseErrorForBadResponse()
        {
            InvalidOperationException ex = TestAssert.Throws<InvalidOperationException>(
                () => SklandApiClient.ParseZiplineMarks("{\"code\":1,\"data\":{}}"));

            TestAssert.AreEqual("获取滑索标记失败", ex.Message);
        }
    }
}
