# Working on Plugin.Maui.SignaturePad

This guide is for contributors and AI coding agents. The README is for people who use the package.

`Plugin.Maui.SignaturePad` is a signature capture control for .NET MAUI on Android, iOS, Mac Catalyst and Windows. Its ink width follows velocity and pen pressure, every native input sample is used, and signatures export as lossless JSON, SVG, PNG and JPEG. It is pure managed code on top of `Microsoft.Maui.Graphics`, with no native binaries.

## Layout

| Path | What it is |
| --- | --- |
| `src/Plugin.Maui.SignaturePad` | The library. |
| `tests/Plugin.Maui.SignaturePad.Tests` | Unit tests (xUnit v3, `net10.0`). They cover all of the shared code. |
| `tests/Plugin.Maui.SignaturePad.UITests.HostApp` | The app the UI tests drive. Every page is a scenario in a known state. |
| `tests/Plugin.Maui.SignaturePad.UITests` | Appium UI tests (NUnit), screenshot baselines and Appium itself (`package.json`). |
| `samples/Plugin.Maui.SignaturePad.Sample` | The sample app for people who want to see the control. |
| `docs/` | Architecture and manual testing notes. |

Inside the library, code is grouped by feature (`Data`, `Ink`, `Capture`, `Controls`, `Rendering`, `Export`, `Handlers`, `Platforms`).

## Commands

```bash
dotnet build Plugin.Maui.SignaturePad.slnx                      # every TFM this OS can build
dotnet test tests/Plugin.Maui.SignaturePad.Tests                 # unit tests
dotnet test tests/Plugin.Maui.SignaturePad.Tests /p:CollectCoverage=true   # fails below 100% line or branch coverage
cd tests/Plugin.Maui.SignaturePad.UITests && npm ci && ./run.sh ios        # UI tests (also: android, maccatalyst)
dotnet pack src/Plugin.Maui.SignaturePad -c Release -o artifacts            # NuGet package
```

The Windows target builds only on Windows. On macOS and Linux the library builds without it.

## Conventions

- **Platform code by file suffix.** `*.android.cs`, `*.macios.cs` (iOS and Mac Catalyst), `*.windows.cs` and `*.net.cs` (the plain `net10.0` target) compile only for their target. Every other `.cs` file is shared.
- **Decisions belong in shared code.** Platform files are thin adapters that translate native events and call shared code. Logic such as pointer filtering, device mapping, timestamp conversion, dirty rectangles or export layout lives in shared, unit-tested helpers.
- **Units.** Geometry is in device-independent units (DIP), relative to the pad's top-left corner. Time is in milliseconds since the first point of the signature.
- **Public API.**
  - Default to `internal`. Public API must have a demonstrated use.
  - Every public member has XML documentation; a missing doc is a build error.
  - Every public member is listed in `PublicAPI.Unshipped.txt` (Microsoft.CodeAnalysis.PublicApiAnalyzers). Never disable the analyzer, and keep the `~` markers that the analyzer generates.
- **No reflection.** The library is marked `IsAotCompatible`, and JSON is read and written with `Utf8JsonReader`/`Utf8JsonWriter`.
- **Comments explain why**, not what.

## MAUI library rules

These are adapted from the dotnet/maui repository's own instructions (MIT).

- **Mappers and lifecycle**
  - Property changes reach the platform through `Handler.UpdateValue(nameof(Property))`. Never call mapper methods directly, because that bypasses `AppendToMapping` customizations.
  - `ConnectHandler` subscribes to events and captures native defaults. `DisconnectHandler` unsubscribes everything, disposes platform resources and nulls references. Always call the base methods.
  - Null-check `VirtualView` in every mapper method and platform callback, because it is null during disconnect. After an `await`, check that the handler is still connected.
- **Threading.** Platform views change only on the UI thread. Platform callbacks can fire after `DisconnectHandler`, so guard against them. Use `OperatingSystem.Is*VersionAtLeast` for OS checks.
- **Hot paths.** Input callbacks and `Draw` must not allocate per sample or per frame where it can be avoided: no LINQ, no capturing lambdas, no string building; use indexed `for` loops over `IReadOnlyList`. If a new value equals the old one, do not invalidate.

## Tests

- **Write the failing test first**, then the code.
- **Unit tests** run on `net10.0` without a device. The coverage gate is 100% line and branch for the shared code.
- **UI tests** check what only a device shows: native input, scrolling, navigation and rendering.
  - A gesture is never pixel-exact, so screenshot baselines exist only for scenarios that load fixed signature data.
  - Record baselines with `--update-baselines` and review the images before committing them.
- **Pen pressure and coalesced input** cannot be automated. Check them by hand with the matrix in `docs/manual-testing.md`.

## Commits

- Use Conventional Commits: `feat:`, `fix:`, `perf:`, `test:`, `docs:`, `ci:`, `chore:`.
- No `Co-Authored-By` or other attribution trailers.
- Keep commits small and buildable.

## Agent skills

`.claude/skills` holds pinned copies of the official MAUI and .NET skills from dotnet/maui-labs, dotnet/skills and davidortinau/maui-skills; their sources and licenses are listed in `.claude/skills/README.md`. Use `maui-custom-handlers`, `maui-graphics-drawing` and `maui-performance` for library work. Use `maui-devflow-*` only with the sample app or the UI test host app.
