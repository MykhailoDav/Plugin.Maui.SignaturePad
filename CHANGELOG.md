# Changelog

All notable changes to this package are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0-preview.1] - Unreleased

The first preview.

### Added

- `SignaturePadView`, a signature pad for Android, iOS, Mac Catalyst and Windows with touch, pen and mouse input.
- Ink whose width follows the speed of the stroke and the pressure of a pen. Every native input sample is used:
  Android's batched samples, iOS coalesced touches and Windows intermediate points. Far apart samples are joined by a
  curve, so fast strokes stay round.
- Undo, redo and clear as methods and as commands; `IsEmpty`, `CanUndo` and `CanRedo`; a placeholder and a signature
  line.
- `Signature`, a two-way bindable `SignatureData` with the strokes, their points, timing, pressure and input device.
- Export as PNG and JPEG (`ExportImageAsync`, `SignatureData.ToImageAsync`), as SVG (`ExportSvg`,
  `SignatureData.ToSvg`) and as versioned JSON that round-trips exactly (`ToJson`, `FromJson`).
- `StrokeStarted` and `StrokeCompleted` events and `StrokeCompletedCommand`.
- `DisplayStrokeColor`, which shows the ink on the pad in another color than the strokes keep, such as light ink on a
  dark pad in a dark theme while the signature and its exports stay dark ink.
- Drawing inside a `ScrollView` does not scroll it, the ink survives Shell tabs and navigation, and `IsEnabled` turns
  input off.

[0.1.0-preview.1]: https://github.com/MykhailoDav/Plugin.Maui.SignaturePad/releases/tag/v0.1.0-preview.1
