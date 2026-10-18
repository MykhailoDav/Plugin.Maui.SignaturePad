# Contributing

Thank you for helping. Bug reports, fixes, tests and documentation are all welcome. For a new feature, please open an
issue first, so we can agree on the API before you write the code.

[AGENTS.md](AGENTS.md) describes the code: its layout, its conventions and the rules for MAUI handlers. It is written
for contributors and AI coding agents alike.

## Reporting a bug

Please include:
- the package version;
- the platform, OS version and device, or the simulator or emulator;
- the input: finger, pen or mouse;
- what you did, what you expected and what happened.

The smallest page that shows the problem helps most. A signature saved with `ToJson()` reproduces a rendering problem
exactly.

To report a vulnerability, follow [SECURITY.md](SECURITY.md) instead.

## Setting up

- The .NET SDK in [global.json](global.json), with the MAUI workload: `dotnet workload install maui`.
- **Android:** the Android SDK, installed with Visual Studio, Rider or Android Studio.
- **iOS and Mac Catalyst:** a Mac with the Xcode version that the workload needs. CI uses the Xcode in
  [.github/workflows/ci.yml](.github/workflows/ci.yml).
- **Windows:** the Windows target builds only on Windows. AGENTS.md shows how to compile it on another OS.

## Building and testing

```bash
dotnet build Plugin.Maui.SignaturePad.slnx
dotnet test --project tests/Plugin.Maui.SignaturePad.Tests --coverlet --coverlet-threshold 100 \
  --coverlet-threshold-type line,branch --coverlet-threshold-stat total
```

- **Unit tests** cover all of the shared code. The build fails below 100% line or branch coverage, so a change comes with
  its tests. Please write the failing test first.
- **UI tests** drive the UI test host app with Appium on a simulator, an emulator or the desktop.
  [tests/Plugin.Maui.SignaturePad.UITests/README.md](tests/Plugin.Maui.SignaturePad.UITests/README.md) explains how to
  run them and how to update the screenshot baselines.
- **Manual checks.** Pen pressure and real devices cannot be automated. [docs/manual-testing.md](docs/manual-testing.md)
  lists what to check by hand.
- **The sample app** in `samples/Plugin.Maui.SignaturePad.Sample` is a quick way to try a change on a device.

## Making a change

- **Style.** Follow [.editorconfig](.editorconfig). CI treats warnings as errors.
- **Public API.**
  - Every public member has XML documentation.
  - Every public member is listed in `src/Plugin.Maui.SignaturePad/PublicAPI`. After an API change, run
    `eng/update-public-api.sh`.
- **Documentation.**
  - When behavior or API changes, update [README.md](README.md) and [llms-full.txt](llms-full.txt), the reference for
    AI coding assistants.
  - Add a line to [CHANGELOG.md](CHANGELOG.md) under the next version.
- **Commits** follow [Conventional Commits](https://www.conventionalcommits.org): `feat:`, `fix:`, `perf:`, `test:`,
  `docs:`, `ci:`, `build:`, `chore:`. Keep each commit small and buildable.
- **Pull requests** go into `main`. CI builds every platform, runs the unit tests with the coverage gate and runs the UI
  tests on iOS and Android.

## Releasing (maintainers)

1. Set `<Version>` in `src/Plugin.Maui.SignaturePad/Plugin.Maui.SignaturePad.csproj`.
2. In CHANGELOG.md, replace "Unreleased" with the date.
3. Move the entries of every `PublicAPI.Unshipped.txt` into the `PublicAPI.Shipped.txt` next to it. Later changes then
   show up as changes to the released API.
4. Merge into `main`.
5. Tag that commit with the version, for example `v0.1.0` or `v0.2.0-preview.1`, and push the tag.

The [release workflow](.github/workflows/release.yml) checks the tag against the project version, builds and tests the
package on Windows (the only OS that builds all five targets) and publishes it to nuget.org.

Publishing uses [NuGet trusted publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing), so no API
key is stored. It needs a one-time setup:
- On nuget.org, under **Trusted publishing**, add a policy with:
  - repository owner `MykhailoDav`;
  - repository `Plugin.Maui.SignaturePad`;
  - workflow file `release.yml`;
  - environment `release`.
- In the repository settings, add the environment `release` with the secret `NUGET_USER`: the nuget.org profile name,
  not the email address. Required reviewers on the environment make every release wait for an approval.
