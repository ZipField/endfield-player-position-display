# Main Control Window, Followed Coordinate Overlay, and Zipline Lookup Design

## Goal

Add a normal main control window for operating the tool, split the coordinate display into its own overlay window, support a global hotkey for toggling that overlay, optionally follow the Endfield game window, and add a zipline coordinate lookup feature based on the current in-game position.

## Confirmed Scope

- The app starts with a normal main window, including standard title bar behavior and a minimize button.
- The coordinate display is a separate window that can be opened or closed from the main window.
- The coordinate display window remains topmost because that behavior exists for the overlay, not for the main window.
- The default global hotkey is a single key: `F12`. Pressing it toggles the coordinate display window.
- The hotkey can be changed to another single key in the main window.
- The coordinate display can optionally follow the game window.
- When following the game window, the coordinate display uses a transparent background, white text, dark gray text outline, and no title bar.
- Game-window following must only locate the game process and read its top-level window rectangle. It must not read or write game memory, inject code, send input, inspect modules, or perform other unnecessary operations on the game process.
- Add a zipline lookup action that reads saved map marks from the Skland endpoint and finds the nearest zipline around the current player position.

## Architecture

Use a lightweight MVVM-style split without adding a third-party framework.

- `MainWindow` becomes the main control surface.
- Add `CoordinateWindow` for the coordinate/status overlay.
- Add a small state/view-model layer, such as `MainViewModel` plus simple observable properties, to hold shared UI state.
- Keep service classes responsible for external work: authentication, WebSocket position stream, signed API calls, game window lookup, hotkey registration, settings, and zipline matching.

The main window and coordinate window both consume state from the view model. The main window exposes controls and query results. The coordinate window only renders the current coordinate or connection/error state.

## Main Window

The main window is a regular WPF window:

- Not topmost.
- Standard title bar and minimize button.
- Contains controls for:
  - Opening or closing the coordinate window.
  - Showing and changing the single-key global hotkey.
  - Enabling or disabling game-window following.
  - Choosing the follow position: top, left, bottom, bottom-right, or bottom-left.
  - Showing current connection and coordinate status.
  - Running zipline lookup.
  - Displaying zipline lookup result.
  - Copying zipline result in either supported format.

## Coordinate Window

The coordinate window is separate from the main window.

Default non-follow mode:

- Small coordinate/status display.
- Topmost.
- May retain a normal compact window style.

Follow mode:

- Topmost.
- `WindowStyle=None`.
- Transparent background.
- White coordinate text.
- Dark gray text outline.
- Positioned relative to the detected game window.

The follow positions are:

- Top: horizontally centered above the game window.
- Left: vertically centered to the left of the game window.
- Bottom: horizontally centered below the game window.
- Bottom-right: near the game window bottom-right.
- Bottom-left: near the game window bottom-left.

Exact margins can be small fixed offsets chosen during implementation to keep the overlay readable without covering the window border.

## Game Window Following

Add a game window locator service.

Behavior:

- Use `Process.GetProcessesByName("endfield")` to locate `endfield.exe`.
- Use only `MainWindowHandle`.
- Use Win32 `GetWindowRect` to read the game window bounds.
- Poll with a lightweight timer, for example every 500 ms, while follow mode is enabled and the coordinate window is open.
- Move only the tool's coordinate window.
- If no suitable game window is found, keep the coordinate window in its last/default position and show a main-window warning such as `未找到 endfield.exe 游戏窗口`.

Safety boundary:

- Do not inspect or mutate game process memory.
- Do not inject code or hooks into the game process.
- Do not enumerate modules or handles beyond the top-level window handle needed for `GetWindowRect`.
- Do not send input or window messages to the game.

## Global Hotkey

Add a global hotkey service using standard Win32 registration for the app window.

Behavior:

- Default key: `F12`.
- Pressing the hotkey toggles the coordinate window open/closed.
- The main window allows changing to another single key.
- Save the selected key in local settings.
- If registration fails, show a clear main-window warning that the hotkey is occupied. Other app features continue to work.
- Unregister the hotkey when changing keys and when the app closes.

## Position Stream and Shared State

Extend the current monitoring flow so state includes:

- Latest connection/status message.
- Latest `PositionSnapshot`.
- Latest map id from the WebSocket payload.
- Current `CredentialResult`.
- Current `RoleBinding`.

`PositionMonitorService` should keep the existing authentication and WebSocket responsibilities, but publish enough data for zipline lookup:

- `CredentialResult` from credential generation.
- `RoleBinding` from binding lookup.
- WebSocket positions.
- WebSocket `mapId` when present in messages.

If a WebSocket message lacks `mapId`, retain the last known map id.

## Zipline Lookup API

Add a signed Skland API call for:

`https://zonai.skland.com/web/v1/game/endfield/map/mark/list?mapId={mapId}&roleId={roleId}&serverId={serverId}`

Inputs:

- `mapId` from the WebSocket coordinate stream.
- `roleId` and `serverId` from `RoleBinding`.
- Credential/signing data from the current authenticated session.

The request should reuse the existing Skland signed GET mechanism and headers.

## Zipline Mark Parsing

Parse `.data.saveMarks` and filter only marks whose `templateId` is one of:

- `0f45150a59b97bd0de9a4eed7a0fbf23`
- `5d53bdb714ba42c1e1a1b748b55b686f`

Each candidate uses `pos.x`, `pos.y`, and `pos.z`.

## Zipline Matching Algorithm

The API mark position is one corner of a 3x3 zipline footprint. The player position should be near the footprint center.

For each candidate mark, compute four possible centers and directions:

- Record point is bottom-left: center `(x + 1, z + 1)`, direction `北`.
- Record point is bottom-right: center `(x - 1, z + 1)`, direction `西`.
- Record point is top-right: center `(x - 1, z - 1)`, direction `南`.
- Record point is top-left: center `(x + 1, z - 1)`, direction `东`.

Compare the player's current `x/z` against each possible center using planar distance only. Ignore height for matching.

Select the nearest possible center within 3 meters. If none are within 3 meters, there is no match.

Result coordinate:

- `x` and `z` are the selected center point, floored to integer values.
- `y` is the API mark's `pos.y`.
- Direction is the matched direction.

No match message:

`未找到，刚放置的滑索可能需要过一小会才能查找到`

## Copy Formats

When a zipline is found, expose two copy actions:

- Tuple-like format: `(x,y,z,方向)`
- JSON format: `{"x":x,"y":y,"z":z,"d":"方向"}`

The JSON copy action must include braces and quote the direction string.

## Settings

Persist user-facing options:

- Coordinate window open/closed.
- Follow game window enabled/disabled.
- Follow position.
- Single-key global hotkey.

Defaults:

- Coordinate window: closed unless implementation chooses to preserve the previous app behavior by opening it on first launch.
- Follow game window: disabled.
- Follow position: top.
- Hotkey: `F12`.

Use the existing project settings mechanism where practical. A small local config file is acceptable if it keeps the code simpler than modifying generated settings artifacts manually.

## Error Handling

Existing token, login, binding, and WebSocket errors continue to surface in the UI.

Zipline lookup should show clear errors for:

- No current player position.
- No current `mapId`.
- No current role binding.
- No current credential.
- API request failure.
- Invalid or unexpected mark response.
- No matching zipline within 3 meters.

Game-window following should show a non-fatal warning when `endfield.exe` is not found or has no top-level window.

Hotkey registration failure should be non-fatal.

## Testing

Extend the existing console-style tests.

Required coverage:

- WebSocket parser extracts position and `mapId`.
- WebSocket parser preserves behavior for messages without `mapId`.
- Mark list parsing filters the two zipline template ids.
- Zipline matching returns all four confirmed directions.
- Zipline matching picks the nearest possible center within 3 meters.
- Zipline matching returns no result over 3 meters.
- Zipline result floors `x/z` and keeps API `y`.
- JSON copy format includes braces and quotes the direction.

Build verification:

- Run the existing test program.
- Build the WPF project.
