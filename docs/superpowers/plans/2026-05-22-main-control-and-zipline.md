# 主页面、坐标悬浮窗和滑索查询实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 把当前单一坐标窗口改成“普通主页面 + 独立坐标悬浮窗”，增加 F12 全局快捷键、跟随游戏窗口、滑索坐标查询和复制功能。

**Architecture:** 使用轻量 MVVM：`MainWindow` 只做控制页，新增 `CoordinateWindow` 做悬浮坐标显示，新增 `MainViewModel` 保存共享状态。外部能力拆到服务：WebSocket/Skland API、滑索解析匹配、游戏窗口定位、全局快捷键、配置保存。

**Tech Stack:** C#, WPF, .NET Framework 4.8, `HttpClient`, `ClientWebSocket`, `JavaScriptSerializer`, Win32 P/Invoke, 当前 console-style 测试项目。

---

## 文件结构

- Modify: `endfield-player-position-display.csproj`，加入新增 XAML、ViewModel、Model、Service 文件。
- Modify: `MainWindow.xaml`，改为普通主控制页。
- Modify: `MainWindow.xaml.cs`，绑定 `MainViewModel`，管理坐标窗、热键、跟随定时器、滑索查询。
- Create: `CoordinateWindow.xaml`，独立坐标悬浮窗 UI。
- Create: `CoordinateWindow.xaml.cs`，根据跟随模式切换透明无标题栏样式。
- Create: `ViewModels\MainViewModel.cs`，保存当前坐标、状态、配置、滑索结果并实现 `INotifyPropertyChanged`。
- Create: `Models\MonitorSessionState.cs`，监控服务对外发布 credential、binding、mapId。
- Modify: `Models\MonitorUpdate.cs`，携带可选 session state 和 mapId。
- Modify: `Services\PositionMonitorService.cs`，认证完成后发布 credential/binding，WebSocket 位置更新时保留 mapId。
- Modify: `Services\PositionWebSocketClient.cs`，解析并回传 `mapId`。
- Create: `Models\ZiplineMark.cs`，保存接口中有效滑索 mark。
- Create: `Models\ZiplineLookupResult.cs`，保存最终滑索坐标和复制格式。
- Create: `Services\ZiplineMatcher.cs`，纯算法：候选中心、3 米内最近匹配、方向推导、复制格式。
- Modify: `Services\SklandApiClient.cs`，增加 signed mark/list 请求和解析方法。
- Create: `Services\GameWindowLocator.cs`，只通过进程名、`MainWindowHandle`、`GetWindowRect` 获取游戏窗口矩形。
- Create: `Services\GlobalHotkeyService.cs`，注册/注销单键全局快捷键。
- Create: `Services\UserSettingsStore.cs`，轻量本地配置文件读写，避免手改生成的 Settings。
- Create: `endfield-player-position-display.Tests\ZiplineMatcherTests.cs`。
- Modify: `endfield-player-position-display.Tests\SklandApiParsingTests.cs`，增加 mark/list 解析测试。
- Modify: `endfield-player-position-display.Tests\PositionWebSocketMessageTests.cs`，增加 `mapId` 解析测试。
- Modify: `endfield-player-position-display.Tests\Program.cs`，加入新测试。
- Modify: `endfield-player-position-display.Tests\endfield-player-position-display.Tests.csproj`，加入新测试文件。

## Task 1: WebSocket mapId 和监控状态

**Files:**
- Modify: `Models\MonitorUpdate.cs`
- Create: `Models\MonitorSessionState.cs`
- Modify: `Services\PositionWebSocketClient.cs`
- Modify: `Services\PositionMonitorService.cs`
- Modify: `endfield-player-position-display.Tests\PositionWebSocketMessageTests.cs`
- Modify: `endfield-player-position-display.Tests\Program.cs`

- [ ] **Step 1: 写失败测试，验证 WebSocket 能解析 mapId**

在 `endfield-player-position-display.Tests\PositionWebSocketMessageTests.cs` 增加：

```csharp
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
```

在 `Program.cs` 的 test 列表加入这两个方法。

- [ ] **Step 2: 运行测试确认失败**

Run: `msbuild endfield-player-position-display.Tests\endfield-player-position-display.Tests.csproj /t:Build /p:Configuration=Debug`

Expected: FAIL，原因是 `PositionWebSocketMessage.MapId` 不存在。

- [ ] **Step 3: 实现最小 mapId 解析**

修改 `PositionWebSocketMessage` 构造和属性，增加 `MapId`。`FromPosition` 改成：

```csharp
public static PositionWebSocketMessage FromPosition(PositionSnapshot position, string mapId)
{
    return new PositionWebSocketMessage(PositionWebSocketMessageKind.Position, position, null, mapId);
}
```

`ParseMessage` 的 `type == 1012` 分支改为：

```csharp
var data = GetObject(root, "data");
var pos = GetObject(data, "pos");
return PositionWebSocketMessage.FromPosition(
    new PositionSnapshot(GetDouble(pos, "x"), GetDouble(pos, "y"), GetDouble(pos, "z")),
    GetString(data, "mapId"));
```

- [ ] **Step 4: 运行测试确认通过**

Run:

```powershell
msbuild endfield-player-position-display.Tests\endfield-player-position-display.Tests.csproj /t:Build /p:Configuration=Debug
.\endfield-player-position-display.Tests\bin\Debug\endfield-player-position-display.Tests.exe
```

Expected: 新增测试 PASS，旧测试 PASS。

- [ ] **Step 5: 增加监控状态模型**

创建 `Models\MonitorSessionState.cs`：

```csharp
namespace endfield_player_position_display.Models
{
    public sealed class MonitorSessionState
    {
        public MonitorSessionState(CredentialResult credential, RoleBinding roleBinding, string mapId)
        {
            Credential = credential;
            RoleBinding = roleBinding;
            MapId = mapId;
        }

        public CredentialResult Credential { get; }
        public RoleBinding RoleBinding { get; }
        public string MapId { get; }
    }
}
```

扩展 `MonitorUpdate`，增加 `SessionState`，并新增 factory：

```csharp
public MonitorSessionState SessionState { get; }

public static MonitorUpdate SessionReady(CredentialResult credential, RoleBinding roleBinding)
{
    return new MonitorUpdate(null, "已连接", false, new MonitorSessionState(credential, roleBinding, null));
}

public static MonitorUpdate FromPosition(PositionSnapshot position, MonitorSessionState sessionState)
{
    return new MonitorUpdate(position, null, false, sessionState);
}
```

同时保留旧的 `FromPosition(PositionSnapshot position)` 以兼容现有测试/调用。

- [ ] **Step 6: 让 PositionMonitorService 发布 session state 和 mapId**

在认证后调用：

```csharp
onUpdate(MonitorUpdate.SessionReady(credential, roleBinding));
```

在 WebSocket 回调中维护 `latestMapId`：

```csharp
string latestMapId = null;
...
(position, mapId) =>
{
    if (!string.IsNullOrWhiteSpace(mapId))
    {
        latestMapId = mapId;
    }

    onUpdate(MonitorUpdate.FromPosition(
        position,
        new MonitorSessionState(credential, roleBinding, latestMapId)));
}
```

同步修改 `PositionWebSocketClient.RunAsync` 的 `onPosition` 参数类型为 `Action<PositionSnapshot, string>`，调用处传 `message.Position, message.MapId`。

- [ ] **Step 7: 构建确认通过并提交**

Run:

```powershell
msbuild endfield-player-position-display.sln /t:Build /p:Configuration=Debug
.\endfield-player-position-display.Tests\bin\Debug\endfield-player-position-display.Tests.exe
```

Expected: Build PASS，测试 PASS。

Commit:

```powershell
git add Models\MonitorSessionState.cs Models\MonitorUpdate.cs Services\PositionWebSocketClient.cs Services\PositionMonitorService.cs endfield-player-position-display.csproj endfield-player-position-display.Tests\PositionWebSocketMessageTests.cs endfield-player-position-display.Tests\Program.cs
git commit -m "Add map id to position updates"
```

## Task 2: 滑索 mark 解析和匹配算法

**Files:**
- Create: `Models\ZiplineMark.cs`
- Create: `Models\ZiplineLookupResult.cs`
- Create: `Services\ZiplineMatcher.cs`
- Create: `endfield-player-position-display.Tests\ZiplineMatcherTests.cs`
- Modify: `endfield-player-position-display.Tests\Program.cs`
- Modify: `endfield-player-position-display.Tests\endfield-player-position-display.Tests.csproj`
- Modify: `endfield-player-position-display.csproj`

- [ ] **Step 1: 写失败测试，覆盖四个方向和复制格式**

创建 `endfield-player-position-display.Tests\ZiplineMatcherTests.cs`：

```csharp
using System.Collections.Generic;
using endfield_player_position_display.Models;
using endfield_player_position_display.Services;

namespace endfield_player_position_display.Tests
{
    internal static class ZiplineMatcherTests
    {
        public static void FindNearestMatchesBottomLeftAsNorth()
        {
            var marks = new[] { new ZiplineMark(10.2, 7.5, 20.8) };

            ZiplineLookupResult result = ZiplineMatcher.FindNearest(new PositionSnapshot(11.3, 99, 21.9), marks);

            TestAssert.AreEqual(true, result.Found);
            TestAssert.AreEqual(11, result.X);
            TestAssert.AreEqual(7.5, result.Y);
            TestAssert.AreEqual(21, result.Z);
            TestAssert.AreEqual("北", result.Direction);
        }

        public static void FindNearestMatchesBottomRightAsWest()
        {
            var marks = new[] { new ZiplineMark(10, 8, 20) };

            ZiplineLookupResult result = ZiplineMatcher.FindNearest(new PositionSnapshot(9, 1, 21), marks);

            TestAssert.AreEqual(true, result.Found);
            TestAssert.AreEqual("西", result.Direction);
        }

        public static void FindNearestMatchesTopRightAsSouth()
        {
            var marks = new[] { new ZiplineMark(10, 8, 20) };

            ZiplineLookupResult result = ZiplineMatcher.FindNearest(new PositionSnapshot(9, 1, 19), marks);

            TestAssert.AreEqual(true, result.Found);
            TestAssert.AreEqual("南", result.Direction);
        }

        public static void FindNearestMatchesTopLeftAsEast()
        {
            var marks = new[] { new ZiplineMark(10, 8, 20) };

            ZiplineLookupResult result = ZiplineMatcher.FindNearest(new PositionSnapshot(11, 1, 19), marks);

            TestAssert.AreEqual(true, result.Found);
            TestAssert.AreEqual("东", result.Direction);
        }

        public static void FindNearestReturnsNoMatchBeyondThreeMeters()
        {
            var marks = new[] { new ZiplineMark(10, 8, 20) };

            ZiplineLookupResult result = ZiplineMatcher.FindNearest(new PositionSnapshot(100, 1, 100), marks);

            TestAssert.AreEqual(false, result.Found);
            TestAssert.AreEqual("未找到，刚放置的滑索可能需要过一小会才能查找到", result.Message);
        }

        public static void FindNearestChoosesClosestCandidate()
        {
            var marks = new[]
            {
                new ZiplineMark(50, 1, 50),
                new ZiplineMark(10, 2, 20)
            };

            ZiplineLookupResult result = ZiplineMatcher.FindNearest(new PositionSnapshot(9.1, 1, 20.9), marks);

            TestAssert.AreEqual(true, result.Found);
            TestAssert.AreEqual(2, result.Y);
            TestAssert.AreEqual("西", result.Direction);
        }

        public static void FormatsCopyValues()
        {
            var result = ZiplineLookupResult.FoundResult(1, 2.5, 3, "北");

            TestAssert.AreEqual("(1,2.5,3,北)", result.ToTupleText());
            TestAssert.AreEqual("{\"x\":1,\"y\":2.5,\"z\":3,\"d\":\"北\"}", result.ToJsonText());
        }
    }
}
```

在 `Program.cs` 加入这些测试方法，在测试 csproj 加入 `ZiplineMatcherTests.cs`。

- [ ] **Step 2: 运行测试确认失败**

Run: `msbuild endfield-player-position-display.Tests\endfield-player-position-display.Tests.csproj /t:Build /p:Configuration=Debug`

Expected: FAIL，原因是 `ZiplineMark`、`ZiplineLookupResult`、`ZiplineMatcher` 不存在。

- [ ] **Step 3: 实现模型和算法**

创建 `Models\ZiplineMark.cs`：

```csharp
namespace endfield_player_position_display.Models
{
    public sealed class ZiplineMark
    {
        public ZiplineMark(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double X { get; }
        public double Y { get; }
        public double Z { get; }
    }
}
```

创建 `Models\ZiplineLookupResult.cs`：

```csharp
using System.Globalization;

namespace endfield_player_position_display.Models
{
    public sealed class ZiplineLookupResult
    {
        private ZiplineLookupResult(bool found, int x, double y, int z, string direction, string message)
        {
            Found = found;
            X = x;
            Y = y;
            Z = z;
            Direction = direction;
            Message = message;
        }

        public bool Found { get; }
        public int X { get; }
        public double Y { get; }
        public int Z { get; }
        public string Direction { get; }
        public string Message { get; }

        public static ZiplineLookupResult FoundResult(int x, double y, int z, string direction)
        {
            return new ZiplineLookupResult(true, x, y, z, direction, null);
        }

        public static ZiplineLookupResult NotFound()
        {
            return new ZiplineLookupResult(false, 0, 0, 0, null, "未找到，刚放置的滑索可能需要过一小会才能查找到");
        }

        public string ToTupleText()
        {
            return string.Format(CultureInfo.InvariantCulture, "({0},{1},{2},{3})", X, Y, Z, Direction);
        }

        public string ToJsonText()
        {
            return string.Format(CultureInfo.InvariantCulture, "{{\"x\":{0},\"y\":{1},\"z\":{2},\"d\":\"{3}\"}}", X, Y, Z, Direction);
        }
    }
}
```

创建 `Services\ZiplineMatcher.cs`：

```csharp
using System;
using System.Collections.Generic;
using endfield_player_position_display.Models;

namespace endfield_player_position_display.Services
{
    public static class ZiplineMatcher
    {
        private const double MaxDistance = 3.0;

        public static ZiplineLookupResult FindNearest(PositionSnapshot player, IEnumerable<ZiplineMark> marks)
        {
            if (player == null || marks == null)
            {
                return ZiplineLookupResult.NotFound();
            }

            Candidate best = null;
            foreach (ZiplineMark mark in marks)
            {
                foreach (Candidate candidate in CreateCandidates(mark))
                {
                    double dx = player.X - candidate.CenterX;
                    double dz = player.Z - candidate.CenterZ;
                    double distance = Math.Sqrt(dx * dx + dz * dz);
                    if (distance <= MaxDistance && (best == null || distance < best.Distance))
                    {
                        candidate.Distance = distance;
                        best = candidate;
                    }
                }
            }

            if (best == null)
            {
                return ZiplineLookupResult.NotFound();
            }

            return ZiplineLookupResult.FoundResult(
                (int)Math.Floor(best.CenterX),
                best.Mark.Y,
                (int)Math.Floor(best.CenterZ),
                best.Direction);
        }

        private static IEnumerable<Candidate> CreateCandidates(ZiplineMark mark)
        {
            yield return new Candidate(mark, mark.X + 1, mark.Z + 1, "北");
            yield return new Candidate(mark, mark.X - 1, mark.Z + 1, "西");
            yield return new Candidate(mark, mark.X - 1, mark.Z - 1, "南");
            yield return new Candidate(mark, mark.X + 1, mark.Z - 1, "东");
        }

        private sealed class Candidate
        {
            public Candidate(ZiplineMark mark, double centerX, double centerZ, string direction)
            {
                Mark = mark;
                CenterX = centerX;
                CenterZ = centerZ;
                Direction = direction;
            }

            public ZiplineMark Mark { get; }
            public double CenterX { get; }
            public double CenterZ { get; }
            public string Direction { get; }
            public double Distance { get; set; }
        }
    }
}
```

主项目 csproj 加入三个新文件。

- [ ] **Step 4: 运行测试确认通过并提交**

Run:

```powershell
msbuild endfield-player-position-display.sln /t:Build /p:Configuration=Debug
.\endfield-player-position-display.Tests\bin\Debug\endfield-player-position-display.Tests.exe
```

Expected: Build PASS，测试 PASS。

Commit:

```powershell
git add Models\ZiplineMark.cs Models\ZiplineLookupResult.cs Services\ZiplineMatcher.cs endfield-player-position-display.csproj endfield-player-position-display.Tests\ZiplineMatcherTests.cs endfield-player-position-display.Tests\Program.cs endfield-player-position-display.Tests\endfield-player-position-display.Tests.csproj
git commit -m "Add zipline matching logic"
```

## Task 3: Skland mark/list API

**Files:**
- Modify: `Services\SklandApiClient.cs`
- Modify: `endfield-player-position-display.Tests\SklandApiParsingTests.cs`

- [ ] **Step 1: 写失败测试，验证 mark/list 只解析滑索模板**

在 `SklandApiParsingTests.cs` 增加：

```csharp
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
```

在 `Program.cs` 加入这两个测试方法。

- [ ] **Step 2: 运行测试确认失败**

Run: `msbuild endfield-player-position-display.Tests\endfield-player-position-display.Tests.csproj /t:Build /p:Configuration=Debug`

Expected: FAIL，原因是 `ParseZiplineMarks` 不存在。

- [ ] **Step 3: 实现解析和 API 方法**

在 `SklandApiClient` 增加：

```csharp
private const string MarkListPath = "/web/v1/game/endfield/map/mark/list";
private static readonly HashSet<string> ZiplineTemplateIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "0f45150a59b97bd0de9a4eed7a0fbf23",
    "5d53bdb714ba42c1e1a1b748b55b686f"
};

public async Task<IList<ZiplineMark>> GetZiplineMarksAsync(
    CredentialResult credential,
    string mapId,
    RoleBinding roleBinding,
    CancellationToken cancellationToken)
{
    string query = "mapId=" + Uri.EscapeDataString(mapId)
        + "&roleId=" + Uri.EscapeDataString(roleBinding.RoleId)
        + "&serverId=" + Uri.EscapeDataString(roleBinding.ServerId);
    string json = await GetSignedAsync(ZonaiBaseUrl + MarkListPath + "?" + query, MarkListPath, credential, cancellationToken).ConfigureAwait(false);
    return ParseZiplineMarks(json);
}

public static IList<ZiplineMark> ParseZiplineMarks(string json)
{
    try
    {
        var serializer = new JavaScriptSerializer();
        IDictionary<string, object> root = serializer.DeserializeObject(json) as IDictionary<string, object>;
        EnsureCodeOk(root, "获取滑索标记失败");
        IDictionary<string, object> data = GetObject(root, "data");
        var result = new List<ZiplineMark>();
        foreach (object markObject in GetArray(data, "saveMarks"))
        {
            var mark = markObject as IDictionary<string, object>;
            if (mark == null || !ZiplineTemplateIds.Contains(GetString(mark, "templateId") ?? string.Empty))
            {
                continue;
            }

            IDictionary<string, object> pos = GetObject(mark, "pos");
            result.Add(new ZiplineMark(GetDouble(pos, "x"), GetDouble(pos, "y"), GetDouble(pos, "z")));
        }

        return result;
    }
    catch (InvalidOperationException)
    {
        throw;
    }
    catch
    {
        throw new InvalidOperationException("获取滑索标记失败");
    }
}
```

新增 private helper:

```csharp
private static double GetDouble(IDictionary<string, object> obj, string key)
{
    if (obj == null || !obj.ContainsKey(key) || obj[key] == null)
    {
        throw new InvalidOperationException("获取滑索标记失败");
    }

    return Convert.ToDouble(obj[key]);
}
```

- [ ] **Step 4: 运行测试确认通过并提交**

Run:

```powershell
msbuild endfield-player-position-display.sln /t:Build /p:Configuration=Debug
.\endfield-player-position-display.Tests\bin\Debug\endfield-player-position-display.Tests.exe
```

Expected: Build PASS，测试 PASS。

Commit:

```powershell
git add Services\SklandApiClient.cs endfield-player-position-display.Tests\SklandApiParsingTests.cs endfield-player-position-display.Tests\Program.cs
git commit -m "Add zipline mark API parsing"
```

## Task 4: ViewModel 和配置保存

**Files:**
- Create: `ViewModels\MainViewModel.cs`
- Create: `Services\UserSettingsStore.cs`
- Modify: `endfield-player-position-display.csproj`

- [ ] **Step 1: 创建配置存储**

创建 `Services\UserSettingsStore.cs`，使用 `%AppData%\endfield-player-position-display\settings.json` 保存：

```csharp
using System;
using System.IO;
using System.Web.Script.Serialization;
using System.Windows.Input;

namespace endfield_player_position_display.Services
{
    public sealed class UserSettingsStore
    {
        private readonly string path;
        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer();

        public UserSettingsStore()
            : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "endfield-player-position-display", "settings.json"))
        {
        }

        internal UserSettingsStore(string path)
        {
            this.path = path;
        }

        public UserSettings Load()
        {
            if (!File.Exists(path))
            {
                return UserSettings.Default();
            }

            try
            {
                return serializer.Deserialize<UserSettings>(File.ReadAllText(path)) ?? UserSettings.Default();
            }
            catch
            {
                return UserSettings.Default();
            }
        }

        public void Save(UserSettings settings)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, serializer.Serialize(settings));
        }
    }

    public sealed class UserSettings
    {
        public bool IsCoordinateWindowOpen { get; set; }
        public bool FollowGameWindow { get; set; }
        public string FollowPosition { get; set; }
        public string Hotkey { get; set; }

        public static UserSettings Default()
        {
            return new UserSettings
            {
                IsCoordinateWindowOpen = false,
                FollowGameWindow = false,
                FollowPosition = "正上",
                Hotkey = Key.F12.ToString()
            };
        }
    }
}
```

- [ ] **Step 2: 创建 MainViewModel**

创建 `ViewModels\MainViewModel.cs`，实现 `INotifyPropertyChanged`，至少包含：

```csharp
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using endfield_player_position_display.Models;

namespace endfield_player_position_display.ViewModels
{
    public sealed class MainViewModel : INotifyPropertyChanged
    {
        private bool isCoordinateWindowOpen;
        private bool followGameWindow;
        private string followPosition;
        private Key hotkey;
        private string statusText;
        private string warningText;
        private PositionSnapshot currentPosition;
        private CredentialResult credential;
        private RoleBinding roleBinding;
        private string mapId;
        private ZiplineLookupResult ziplineResult;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsCoordinateWindowOpen { get { return isCoordinateWindowOpen; } set { Set(ref isCoordinateWindowOpen, value); } }
        public bool FollowGameWindow { get { return followGameWindow; } set { Set(ref followGameWindow, value); } }
        public string FollowPosition { get { return followPosition; } set { Set(ref followPosition, value); } }
        public Key Hotkey { get { return hotkey; } set { Set(ref hotkey, value); } }
        public string StatusText { get { return statusText; } set { Set(ref statusText, value); } }
        public string WarningText { get { return warningText; } set { Set(ref warningText, value); } }
        public PositionSnapshot CurrentPosition { get { return currentPosition; } set { Set(ref currentPosition, value); } }
        public CredentialResult Credential { get { return credential; } set { Set(ref credential, value); } }
        public RoleBinding RoleBinding { get { return roleBinding; } set { Set(ref roleBinding, value); } }
        public string MapId { get { return mapId; } set { Set(ref mapId, value); } }
        public ZiplineLookupResult ZiplineResult { get { return ziplineResult; } set { Set(ref ziplineResult, value); } }

        public void ApplySettings(Services.UserSettings settings)
        {
            IsCoordinateWindowOpen = settings.IsCoordinateWindowOpen;
            FollowGameWindow = settings.FollowGameWindow;
            FollowPosition = string.IsNullOrWhiteSpace(settings.FollowPosition) ? "正上" : settings.FollowPosition;
            Key parsed;
            Hotkey = Key.TryParse(settings.Hotkey, out parsed) ? parsed : Key.F12;
        }

        public Services.UserSettings ToSettings()
        {
            return new Services.UserSettings
            {
                IsCoordinateWindowOpen = IsCoordinateWindowOpen,
                FollowGameWindow = FollowGameWindow,
                FollowPosition = FollowPosition,
                Hotkey = Hotkey.ToString()
            };
        }

        public void ApplyMonitorUpdate(MonitorUpdate update)
        {
            if (!string.IsNullOrWhiteSpace(update.Status))
            {
                StatusText = update.Status;
            }

            if (update.Position != null)
            {
                CurrentPosition = update.Position;
            }

            if (update.SessionState != null)
            {
                Credential = update.SessionState.Credential ?? Credential;
                RoleBinding = update.SessionState.RoleBinding ?? RoleBinding;
                if (!string.IsNullOrWhiteSpace(update.SessionState.MapId))
                {
                    MapId = update.SessionState.MapId;
                }
            }
        }

        private void Set<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (object.Equals(field, value))
            {
                return;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
```

如果 `Key.TryParse` 在目标框架不可用，改用 `Enum.TryParse<Key>`。

- [ ] **Step 3: 构建并提交**

Run: `msbuild endfield-player-position-display.sln /t:Build /p:Configuration=Debug`

Expected: Build PASS。

Commit:

```powershell
git add ViewModels\MainViewModel.cs Services\UserSettingsStore.cs endfield-player-position-display.csproj
git commit -m "Add shared view model and settings"
```

## Task 5: 游戏窗口定位和全局快捷键服务

**Files:**
- Create: `Services\GameWindowLocator.cs`
- Create: `Services\GlobalHotkeyService.cs`
- Modify: `endfield-player-position-display.csproj`

- [ ] **Step 1: 创建 GameWindowLocator**

创建 `Services\GameWindowLocator.cs`：

```csharp
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace endfield_player_position_display.Services
{
    public sealed class GameWindowLocator
    {
        public bool TryGetEndfieldWindowRect(out Rect rect)
        {
            rect = Rect.Empty;
            foreach (Process process in Process.GetProcessesByName("endfield"))
            {
                IntPtr handle = process.MainWindowHandle;
                if (handle == IntPtr.Zero)
                {
                    continue;
                }

                NativeRect nativeRect;
                if (GetWindowRect(handle, out nativeRect))
                {
                    rect = new Rect(nativeRect.Left, nativeRect.Top, nativeRect.Right - nativeRect.Left, nativeRect.Bottom - nativeRect.Top);
                    return true;
                }
            }

            return false;
        }

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}
```

- [ ] **Step 2: 创建 GlobalHotkeyService**

创建 `Services\GlobalHotkeyService.cs`：

```csharp
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
```

- [ ] **Step 3: 构建并提交**

Run: `msbuild endfield-player-position-display.sln /t:Build /p:Configuration=Debug`

Expected: Build PASS。

Commit:

```powershell
git add Services\GameWindowLocator.cs Services\GlobalHotkeyService.cs endfield-player-position-display.csproj
git commit -m "Add window follow and hotkey services"
```

## Task 6: 坐标悬浮窗

**Files:**
- Create: `CoordinateWindow.xaml`
- Create: `CoordinateWindow.xaml.cs`
- Modify: `endfield-player-position-display.csproj`

- [ ] **Step 1: 创建坐标窗 XAML**

创建 `CoordinateWindow.xaml`：

```xml
<Window x:Class="endfield_player_position_display.CoordinateWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="坐标"
        Width="160"
        SizeToContent="Height"
        ResizeMode="NoResize"
        Topmost="True"
        Background="#202124">
    <Border x:Name="RootBorder" Padding="10" Background="#202124">
        <Grid>
            <TextBlock x:Name="StatusText"
                       Foreground="#E8EAED"
                       FontSize="12"
                       TextWrapping="Wrap"
                       TextTrimming="CharacterEllipsis" />
            <Grid x:Name="PositionPanel" Visibility="Collapsed">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="20" />
                    <ColumnDefinition Width="*" />
                </Grid.ColumnDefinitions>
                <Grid.RowDefinitions>
                    <RowDefinition Height="18" />
                    <RowDefinition Height="18" />
                    <RowDefinition Height="18" />
                </Grid.RowDefinitions>
                <TextBlock Grid.Row="0" Grid.Column="0" Foreground="#9AA0A6" FontSize="12" Text="X" />
                <TextBlock x:Name="XText" Grid.Row="0" Grid.Column="1" Foreground="#FFFFFF" FontFamily="Consolas" FontSize="12" Text="-" TextAlignment="Right" />
                <TextBlock Grid.Row="1" Grid.Column="0" Foreground="#9AA0A6" FontSize="12" Text="Y" />
                <TextBlock x:Name="YText" Grid.Row="1" Grid.Column="1" Foreground="#FFFFFF" FontFamily="Consolas" FontSize="12" Text="-" TextAlignment="Right" />
                <TextBlock Grid.Row="2" Grid.Column="0" Foreground="#9AA0A6" FontSize="12" Text="Z" />
                <TextBlock x:Name="ZText" Grid.Row="2" Grid.Column="1" Foreground="#FFFFFF" FontFamily="Consolas" FontSize="12" Text="-" TextAlignment="Right" />
            </Grid>
        </Grid>
    </Border>
</Window>
```

- [ ] **Step 2: 创建坐标窗代码**

创建 `CoordinateWindow.xaml.cs`：

```csharp
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using endfield_player_position_display.Services;
using endfield_player_position_display.ViewModels;

namespace endfield_player_position_display
{
    public partial class CoordinateWindow : Window
    {
        private readonly MainViewModel viewModel;

        public CoordinateWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            this.viewModel = viewModel;
            DataContext = viewModel;
            viewModel.PropertyChanged += ViewModelPropertyChanged;
            Closed += CoordinateWindowClosed;
            ApplyAll();
        }

        public void ApplyFollowMode()
        {
            Topmost = true;
            if (viewModel.FollowGameWindow)
            {
                WindowStyle = WindowStyle.None;
                AllowsTransparency = true;
                Background = Brushes.Transparent;
                RootBorder.Background = Brushes.Transparent;
            }
            else
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                AllowsTransparency = false;
                Background = new SolidColorBrush(Color.FromRgb(32, 33, 36));
                RootBorder.Background = Background;
            }
        }

        private void ViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Dispatcher.Invoke(ApplyAll);
        }

        private void ApplyAll()
        {
            ApplyFollowMode();
            if (viewModel.CurrentPosition == null)
            {
                StatusText.Text = string.IsNullOrWhiteSpace(viewModel.StatusText) ? "正在连接..." : viewModel.StatusText;
                StatusText.Visibility = Visibility.Visible;
                PositionPanel.Visibility = Visibility.Collapsed;
                return;
            }

            StatusText.Visibility = Visibility.Collapsed;
            PositionPanel.Visibility = Visibility.Visible;
            XText.Text = CoordinateFormatter.Format(viewModel.CurrentPosition.X);
            YText.Text = CoordinateFormatter.Format(viewModel.CurrentPosition.Y);
            ZText.Text = CoordinateFormatter.Format(viewModel.CurrentPosition.Z);
        }

        private void CoordinateWindowClosed(object sender, System.EventArgs e)
        {
            viewModel.PropertyChanged -= ViewModelPropertyChanged;
        }
    }
}
```

如果 WPF 不允许运行时从 `AllowsTransparency=true` 切回 `false`，实现时改为关闭并重建坐标窗；主窗口的 `EnsureCoordinateWindow` 负责重建。

- [ ] **Step 3: 构建并提交**

Run: `msbuild endfield-player-position-display.sln /t:Build /p:Configuration=Debug`

Expected: Build PASS。

Commit:

```powershell
git add CoordinateWindow.xaml CoordinateWindow.xaml.cs endfield-player-position-display.csproj
git commit -m "Add coordinate overlay window"
```

## Task 7: 主页面 UI 和交互

**Files:**
- Modify: `MainWindow.xaml`
- Modify: `MainWindow.xaml.cs`

- [ ] **Step 1: 重写主页面 XAML**

把 `MainWindow.xaml` 改成普通窗口：`Width="420"`、`Height="520"`、`ResizeMode="CanMinimize"`、不设置 `Topmost`。主体包含：

```xml
<StackPanel Margin="16">
    <TextBlock Text="终末地坐标工具" FontSize="18" FontWeight="SemiBold" Margin="0,0,0,12" />
    <CheckBox x:Name="CoordinateWindowCheckBox" Content="显示坐标窗口" Margin="0,0,0,8" Checked="CoordinateWindowChecked" Unchecked="CoordinateWindowUnchecked" />
    <StackPanel Orientation="Horizontal" Margin="0,0,0,8">
        <TextBlock Text="快捷键：" VerticalAlignment="Center" />
        <TextBlock x:Name="HotkeyText" Margin="8,0,8,0" VerticalAlignment="Center" />
        <Button x:Name="CaptureHotkeyButton" Content="设置快捷键" Click="CaptureHotkeyButtonClick" />
    </StackPanel>
    <CheckBox x:Name="FollowGameCheckBox" Content="跟随游戏窗口" Margin="0,0,0,8" Checked="FollowGameChanged" Unchecked="FollowGameChanged" />
    <StackPanel Orientation="Horizontal" Margin="0,0,0,12">
        <TextBlock Text="显示位置：" VerticalAlignment="Center" />
        <ComboBox x:Name="FollowPositionComboBox" Width="120" Margin="8,0,0,0" SelectionChanged="FollowPositionChanged">
            <ComboBoxItem Content="正上" />
            <ComboBoxItem Content="正左" />
            <ComboBoxItem Content="正下" />
            <ComboBoxItem Content="右下" />
            <ComboBoxItem Content="左下" />
        </ComboBox>
    </StackPanel>
    <TextBlock Text="状态" FontWeight="SemiBold" />
    <TextBlock x:Name="StatusText" TextWrapping="Wrap" Margin="0,4,0,8" />
    <TextBlock x:Name="WarningText" Foreground="#B3261E" TextWrapping="Wrap" Margin="0,0,0,12" />
    <Button Content="获取滑索坐标" Click="LookupZiplineButtonClick" Margin="0,0,0,8" />
    <TextBlock x:Name="ZiplineResultText" TextWrapping="Wrap" Margin="0,0,0,8" />
    <StackPanel Orientation="Horizontal">
        <Button x:Name="CopyTupleButton" Content="复制 (x,y,z,方向)" Click="CopyTupleButtonClick" Margin="0,0,8,0" />
        <Button x:Name="CopyJsonButton" Content="复制 JSON" Click="CopyJsonButtonClick" />
    </StackPanel>
</StackPanel>
```

- [ ] **Step 2: 实现 MainWindow 代码**

`MainWindow.xaml.cs` 改为：

- 构造 `MainViewModel`、`UserSettingsStore`、`PositionMonitorService`、`SklandApiClient`、`GameWindowLocator`、`GlobalHotkeyService`。
- `Loaded` 时加载配置、注册热键、启动监控服务。
- `Closed` 时保存配置、关闭坐标窗、释放服务。
- `ApplyUpdate` 调 `viewModel.ApplyMonitorUpdate(update)` 并刷新 UI。
- 复选框打开/关闭坐标窗。
- `F12` 回调切换 `viewModel.IsCoordinateWindowOpen`。
- 跟随开启时启动 `DispatcherTimer`，每 500ms 用 `GameWindowLocator` 更新坐标窗位置。
- 滑索查询按钮校验 `CurrentPosition`、`MapId`、`Credential`、`RoleBinding`，调用 `apiClient.GetZiplineMarksAsync` 和 `ZiplineMatcher.FindNearest`。
- 复制按钮使用 `Clipboard.SetText`。

关键方法：

```csharp
private void ToggleCoordinateWindow()
{
    viewModel.IsCoordinateWindowOpen = !viewModel.IsCoordinateWindowOpen;
    ApplyCoordinateWindowState();
    SaveSettings();
}

private void ApplyCoordinateWindowState()
{
    if (viewModel.IsCoordinateWindowOpen)
    {
        if (coordinateWindow == null)
        {
            coordinateWindow = new CoordinateWindow(viewModel);
            coordinateWindow.Closed += (s, e) => { coordinateWindow = null; viewModel.IsCoordinateWindowOpen = false; UpdateMainUi(); };
        }

        coordinateWindow.Show();
        UpdateFollowPosition();
    }
    else if (coordinateWindow != null)
    {
        coordinateWindow.Close();
        coordinateWindow = null;
    }
}
```

`UpdateFollowPosition` 按 `正上/正左/正下/右下/左下` 计算 `Left/Top`。

快捷键录入：点击设置按钮后设置 `capturingHotkey = true`，主窗口 `PreviewKeyDown` 中取 `e.Key == Key.System ? e.SystemKey : e.Key`，保存并重新注册。

- [ ] **Step 3: 构建并提交**

Run: `msbuild endfield-player-position-display.sln /t:Build /p:Configuration=Debug`

Expected: Build PASS。

Commit:

```powershell
git add MainWindow.xaml MainWindow.xaml.cs
git commit -m "Add main control window interactions"
```

## Task 8: UI 收尾、人工验证和文档

**Files:**
- Modify: `README.md`

- [ ] **Step 1: 更新 README 用法**

补充：

- 启动后主页面是控制页。
- `F12` 默认切换坐标窗口。
- 跟随游戏窗口只读取 `endfield.exe` 顶层窗口位置。
- 滑索查询需要当前坐标和森空岛接口能返回最新 mark。
- JSON 复制格式为 `{"x":x,"y":y,"z":z,"d":"方向"}`。

- [ ] **Step 2: 全量构建和测试**

Run:

```powershell
msbuild endfield-player-position-display.sln /t:Build /p:Configuration=Debug
.\endfield-player-position-display.Tests\bin\Debug\endfield-player-position-display.Tests.exe
```

Expected: Build PASS，全部测试 PASS。

- [ ] **Step 3: 启动应用做人工验证**

Run:

```powershell
Start-Process .\bin\Debug\endfield-player-position-display.exe
```

人工检查：

- 主窗口有标题栏和最小化按钮，且不置顶。
- “显示坐标窗口”能打开/关闭独立坐标窗。
- `F12` 能切换坐标窗。
- 坐标窗置顶。
- 跟随游戏开启后坐标窗无标题栏、透明背景。
- 找不到游戏窗口时主窗口显示 `未找到 endfield.exe 游戏窗口`。
- 滑索查询缺少当前位置时显示明确错误。
- 复制按钮在没有结果时不可用或不复制空结果。

- [ ] **Step 4: 提交 README 和最终修改**

Run: `git status --short`

Commit:

```powershell
git add README.md
git commit -m "Document main window and zipline features"
```

如果 Task 7 的 UI 调整和 README 修改一起完成，可以合并为同一个提交，但不要把无关 `.idea/` 加进去。

## 自检清单

- spec 的主窗口、坐标窗、快捷键、跟随游戏、滑索 API、算法、复制格式、配置、错误处理、测试都有对应任务。
- 纯逻辑改动先写失败测试：WebSocket mapId、滑索匹配、mark/list 解析。
- Win32 服务和 WPF UI 难以用当前 console 测试覆盖，用构建和人工验证覆盖。
- 所有新增文件都在 csproj 或 test csproj 中列出。
- 无 `TODO`、`TBD`、占位实现。
