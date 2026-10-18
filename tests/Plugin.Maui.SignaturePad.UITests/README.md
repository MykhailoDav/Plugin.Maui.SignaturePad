# UI tests

These tests drive a real app with [Appium](https://appium.io). They check the pad the way a user meets it:
- they draw strokes, dots and swipes with the platform's pointer;
- they tap the action buttons;
- they read the pad's state from the screen;
- they compare screenshots of fixed signatures with baseline images.

| Folder | What it is |
| --- | --- |
| `tests/Plugin.Maui.SignaturePad.UITests.HostApp` | The app under test. Every page is a *scenario*: a pad in a known state. |
| `tests/Plugin.Maui.SignaturePad.UITests` | The NUnit tests, Appium (`package.json`) and the baselines (`Baselines/<platform>`). |

The unit tests in `tests/Plugin.Maui.SignaturePad.Tests` check all of the shared code without a device. The UI tests check what only a device shows:
- native input, including Android's batched samples and iOS gesture arbitration;
- drawing inside scroll views, Shell tabs and BindableLayout;
- disabled pads;
- export;
- rendering.

## How the host app makes the tests repeatable

**Scenarios.** Every scenario page shows:
- the pad (`Pad`, 320 × 200 DIP) with the placeholder "Sign here";
- the action buttons `Undo`, `Redo`, `Clear`, `ExportPng` and `ExportSvg`;
- the pad's state as text the tests read: `IsEmpty`, `StrokeCount`, `PointCount`, `CanUndo`, `CanRedo`, `LastDevice`, `HasPressure`, `LastExport` and `Events`.

**Opening a scenario.** The tests type the scenario's name into `ScenarioEntry` on the first page and tap `GoButton`.

**Baselines.** A drawn gesture never repeats exactly, because the driver's timing varies, and the ink depends on speed. So screenshot baselines exist only for the empty pad and for the fixed signatures of `Fixtures` in the host app (pen pressure, velocity, dots and loops). The tests about drawing check what the pad recorded instead.

**Theme.** The theme is light.

| Scenario | What it has |
| --- | --- |
| `Basic` | One pad. |
| `InScrollView` | The pad in a scroll view, a `ScrollHandle` below it to scroll by, and `ScrollY`. |
| `Disabled` | A disabled pad, `ToggleEnabled` and `PadEnabled`. |
| `Rendering` | `LoadPenPressure`, `LoadVelocity` and `LoadDotsAndLoops`, which load fixed signatures. |
| `Tabs` | The pad on the first tab of a Shell (`PadTab`, `OtherTab`). |
| `BindableLayout` | Three pads (`Pad0`..`Pad2`) made by an item template, each with its stroke count (`Count0`..`Count2`). |

## Setup

- **Every platform:**
  - the .NET 10 SDK with the MAUI workload;
  - Node.js 20 or later, then once `npm ci` in this folder. This installs Appium and its drivers here. The tests start this Appium themselves, listening on 127.0.0.1 only and with `APPIUM_HOME` set to this folder, so nothing is installed globally.
- **iOS:** Xcode and the simulator the baselines were recorded on (see below). The first run builds Appium's WebDriverAgent, which takes a few minutes.
- **Android:** the Android SDK, Java 17 or later, and either an emulator that is running or an AVD named in `UITEST_DEVICE`.
- **Mac Catalyst:**
  - Xcode and macOS *Automation Mode*. To turn off the password prompt once, run `sudo automationmodetool enable-automationmode-without-authentication`.
  - Allow the terminal to control the computer (System Settings › Privacy & Security › Accessibility).
- **Windows:** WinAppDriver (`appium-windows-driver` uses it) and Developer Mode.

## Running

On macOS, `run.sh` builds the host app for the platform and runs the tests:

```bash
cd tests/Plugin.Maui.SignaturePad.UITests
./run.sh ios
./run.sh android
./run.sh maccatalyst
./run.sh android --filter DrawingTests
```

Or by hand (also on Windows):

```bash
dotnet build tests/Plugin.Maui.SignaturePad.UITests.HostApp -f net10.0-android
UITEST_PLATFORM=android dotnet test --project tests/Plugin.Maui.SignaturePad.UITests
```

| Variable | Meaning |
| --- | --- |
| `UITEST_PLATFORM` | `ios`, `android`, `maccatalyst` or `windows` (default: `ios` on macOS, `windows` on Windows) |
| `UITEST_DEVICE` | The iOS simulator (default `iPhone 17`) or the Android AVD to start |
| `UITEST_PLATFORM_VERSION` | The iOS or Android version of that device |
| `UITEST_CONFIGURATION` | The configuration the host app was built with (default `Debug`) |
| `UITEST_APP` | An app to test instead of the host app's build output |
| `UITEST_APPIUM_URL` | An Appium server that is already running (otherwise the tests start one) |
| `UITEST_UPDATE_BASELINES` | `1` saves the screenshots as the new baselines instead of comparing them |
| `UITEST_SCREENSHOTS` | `off` skips the screenshot comparisons and checks the behavior only |

The Appium log of a run is in `TestResults/Screenshots/<platform>/appium.log`, together with the screen and element tree of every failed test.

### Pen input

The UiAutomator2 driver turns every W3C pointer action into a finger. So the stylus test draws with `adb shell input stylus`, which sends real stylus events with full pressure, directly from the test process.

On iOS, a simulator has no Apple Pencil, and Mac Catalyst has only a mouse. Pencil pressure and coalesced touches are covered by the unit tests of the shared input code and by the manual checks in `docs/manual-testing.md`.

### Gesture timing on iOS

XCUITest plays a gesture back frame by frame. When the steps of a pointer move are shorter than a frame (about 16 ms), every touch of the stroke gets the same timestamp. The pad then records the stroke as drawn in no time, and its ink is as thin as fast ink. A test that depends on the speed of a stroke has to use steps of 20 ms or longer.

## Screenshots and baselines

A screenshot of the pad is compared with `Baselines/<platform>/<name>.png`. A pixel counts as different when a color channel differs by more than 32 (out of 255). The comparison fails when more than 0.1 % of the pixels differ.

On a failure, `TestResults/Screenshots/<platform>` holds three images, which the test lists as attachments:
- the screenshot (`.actual.png`);
- the baseline (`.expected.png`);
- the differences in red (`.diff.png`).

Rendering differs between devices and OS versions, so the baselines only match on the devices they were recorded on:

| Platform | Device the baselines were recorded on |
| --- | --- |
| iOS | iPhone 17 simulator, iOS 26.5 (Xcode 26.6) |
| Android | Pixel 8 emulator (`Pixel_8` AVD), Android 14 (API 34) |
| Mac Catalyst, Windows | No baselines yet: record them with `--update-baselines` on the machine that runs the tests |

On another device, run with `UITEST_SCREENSHOTS=off` to check the behavior only, or record baselines for it.

When a change is meant to change the look:

1. Run the tests; the failures show which screenshots changed.
2. Look at the `.diff.png` files and check that only the intended things changed.
3. Run with `--update-baselines` (or `UITEST_UPDATE_BASELINES=1`) to save the new screenshots.
4. Review the changed images in `git diff` before you commit them.
