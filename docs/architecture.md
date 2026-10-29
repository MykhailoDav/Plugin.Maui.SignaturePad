# Architecture

This document explains how the library works inside: how input becomes a stroke, how a stroke becomes ink, and how a
signature is drawn and exported. It is for contributors. [README.md](../README.md) describes the public API, and
[AGENTS.md](../AGENTS.md) the conventions for changing the code.

## Overview

```
SignaturePadView (MAUI View: bindable properties, commands, events)
 ├─ owns SignatureSession (capture, undo history, signature time)
 │   └─ produces immutable SignatureData → SignatureStroke → SignaturePoint
 └─ handler: SignaturePadHandler
     └─ PlatformSignaturePadView (a Microsoft.Maui.Graphics PlatformGraphicsView per platform)
         ├─ native input → IInkInputSink (the session)
         └─ Drawable = SignaturePadDrawable → InkRenderer → ICanvas

Ink engine (shared C#, no MAUI Controls types)
  StrokeBuilder → StrokeSpline → StrokeOutline → StrokeGeometry (PathF and SVG path data)

Export
  ExportLayout, SvgExporter, SignatureJson (shared)
  SignatureImageExporter.{android,macios,windows,net}.cs (a bitmap per platform, the same InkRenderer)
```

The library depends only on `Microsoft.Maui.Controls`. The ink engine uses the geometry types of
`Microsoft.Maui.Graphics` (`PointF`, `RectF`, `PathF`, `Color`), which are plain .NET, so all of it is unit tested on
`net10.0` without a device.

## Code layout

The library's folders are features:

| Folder | Contents |
| --- | --- |
| `Data` | The public data types, JSON and colors. |
| `Ink` | The ink engine. |
| `Capture` | The session, the input sink and the input mapping helpers. |
| `Controls` | `SignaturePadView` and the size it asks for (`PadSize`). |
| `Rendering` | The drawable, the renderer and the path cache. |
| `Export` | Export options, layout, SVG, and image export per platform. |
| `Handlers` | The handler and `UseSignaturePad`. |
| `PlatformViews` | The native views and their input. |

Platform code is chosen by file suffix: `*.android.cs`, `*.macios.cs` (iOS and Mac Catalyst), `*.windows.cs`, and
`*.net.cs` for the plain `net10.0` target. The project file removes each suffix from the other targets. Platform files
only translate native events and call shared code, so every decision can be unit tested.

**Units.** Coordinates are in device-independent units (DIP) from the pad's top-left corner. Android pixels are divided
by the display density; iOS points and Windows effective pixels already are DIP. Time is in milliseconds since the first
point of the signature.

**Size.** The pad has no content to measure. `SignaturePadView.MeasureOverride` asks `PadSize` for its size: the space
the layout gives it, or 300 × 200 where the layout leaves a length open, as a vertical stack, a scroll view or an Auto
row leaves the height. As in MAUI's own measuring, a requested size wins, the minimum and maximum bound the result, and
it never exceeds the space.

## Input

All platforms feed the same internal interface:

```csharp
internal interface IInkInputSink
{
    bool IsInputEnabled { get; }
    void BeginStroke(in InkSample sample);
    void ContinueStroke(ReadOnlySpan<InkSample> samples);
    void EndStroke(in InkSample sample);
    void CancelStroke();
}
```

`InkSample` is a struct: position, a timestamp in milliseconds, pressure (`NaN` when unknown) and the device.

- **One pointer.** One pointer draws at a time; others are ignored until it lifts.
- **Cancelled input.** A stroke that the platform cancels keeps its ink, because the user saw it being drawn.

| Platform | Source | Every sample | Device and pressure | Inside scrolling views |
| --- | --- | --- | --- | --- |
| Android | `OnTouchEvent` | The historical samples of each `MotionEvent`, then the current one; nanosecond event times on API 34 and later | Tool type: stylus (pressure), mouse (primary button only), finger; the eraser is ignored | `RequestDisallowInterceptTouchEvent` on down |
| iOS, Mac Catalyst | `InkGestureRecognizer`, a `UIGestureRecognizer` on the view | Coalesced touches, with precise locations | Touch type: pencil (force / maximum force), direct touch, indirect pointer (mouse) | Ancestor pan recognizers must wait for the pad's recognizer to fail; touches outside the pad never reach it |
| Windows | Pointer events, with the pointer captured | `GetIntermediatePoints`, put in time order | Pointer type; a pen without eraser or barrel button (pressure), the left mouse button, touch | `ManipulationMode = All` keeps touch from becoming direct manipulation |

The iOS recognizer holds the session through a weak reference. The native view retains the recognizer, and a strong
reference back would keep the whole pad alive.

## The session

`SignatureSession` holds:
- the committed signature (`Current`);
- the stroke being drawn (`Active`);
- the undo and redo stacks;
- the clock of the signature.

It lives as long as the `SignaturePadView`, not the handler, so the ink survives the native view being recreated by
navigation, tabs or recycling.

- **History.**
  - Every stroke, and every clear, is an undo step. Undoing a clear brings back all the strokes it removed.
  - A new stroke empties the redo stack.
  - Setting `Signature` from outside loads that signature and forgets the history.
- **Time.**
  - The first stroke of an empty signature starts at time 0.
  - Later strokes keep the real pause since the previous one. A stroke never starts before the end of the ink already
    there, so time keeps increasing after loading data or undoing a clear.
- **Canvas size.** The size of the pad is recorded with the next change of the signature, not as a change of its own,
  so layout passes do not ripple through bindings.
- **Updating `Signature`.** The view sets `Signature` with `SetValueFromRenderer`, so a one-way binding on it keeps
  working.

## The ink engine

The constants are internal (`Ink/InkDefaults.cs`). The unit tests pin the formulas that use them.

**1. Keeping samples** (`StrokeBuilder`)
- **Quantizing.** Samples are rounded as they will be stored: 0.01 DIP, 0.01 ms, pressure to 0.001. The ink is
  computed from the rounded values, so a signature restored from JSON renders exactly like the live one.
- **Dropping.**
  - A sample closer than 0.25 DIP to the last kept one is dropped, unless a pen's pressure changed by 0.02 or more.
  - The last sample is kept unless it is identical to the previous one.
- **Pressure.** Only a pen's pressure is stored. A finger's "pressure" is its contact size, which would make the ink
  jump.

**2. Width** for each kept point i:

```
dt  = max(tᵢ − tᵢ₋₁, 1 ms)
v   = distance / dt
vf₀ = (MaxWidth − MinWidth) / (MaxWidth + MinWidth) DIP/ms
vfᵢ = 0.7·v + 0.3·vfᵢ₋₁                            (filtered velocity)
kv  = 1 / (1 + vf / 1 DIP/ms)                      (0.5 at 1 DIP/ms)

without pressure:     width = max(MinWidth, MaxWidth·kv)
pen with pressure p:  width = MinWidth + (MaxWidth − MinWidth)·p·(0.7 + 0.3·kv)
```

- **Without pressure**, speed alone thins the ink, as a pen's does. The ink is the widest at rest and half that at
  1 DIP/ms. From (MaxWidth / MinWidth − 1) DIP/ms on, 2.5 with the default widths, it is the narrowest.
- **A pen's** pressure sets the width, and speed thins it by at most 30%.
- **The start.** A stroke starts at the velocity at which ink without pressure is halfway between its extreme widths.
  It does not begin with a blob, and a tap leaves a dot of the average width.

**3. Streamline.** The drawn centre line trails the input to smooth out jitter:

```
s₀ = raw₀
sᵢ = sᵢ₋₁ + (rawᵢ − sᵢ₋₁)·f
f  = 0.575 for steps up to 2 DIP, rising to 1 at 12 DIP and more
```

Short steps are mostly jitter and get the full smoothing. Long steps are movement: smoothing them would only drag the
line inside every turn. A completed stroke ends exactly at its last sample.

**4. Spline** (`StrokeSpline`)
- **Which segments.** Segments longer than 2 DIP get points in between, on a centripetal Catmull-Rom spline through the
  centre line: Yuksel, Schaefer and Keyser, 2011. Each segment gets at most 64 steps.
- **Why centripetal.** Unlike the uniform spline, it never forms a cusp or a loop within a segment.
- **The ends.** A point mirrored across each end continues the stroke straight.
- **Radius.** The radius changes linearly along each segment.

**5. Outline** (`StrokeOutline`)
- **Origin.** A port of perfect-freehand 1.2 (MIT), with an explicit radius per point. Credited in
  [THIRD-PARTY-NOTICES.md](../THIRD-PARTY-NOTICES.md).
- **The polygon.** One closed polygon per stroke: the left side forwards, a round end cap, the right side backwards and
  a round start cap.
- **Sharp corners.** A turn of more than a right angle gets a round cap instead of crossing sides.
- **Noise.** The last 3 DIP before the end, usually the pen lifting off, are covered by the end cap.
- **Dots.** A stroke whose points all lie within 0.5 DIP of its first point is a dot as wide as the stroke got.

**6. Path** (`StrokeGeometry`)
- **The curve.** The path runs through the midpoints of the outline's edges, with each outline point as the control
  point of a quadratic Bézier, and is filled with the non-zero rule.
- **SVG.** SVG path data uses the same sequence (`M`, `Q`, `Z`), with numbers in invariant culture and at most two
  decimals, so SVG and screen show the same shape.

## Rendering

`SignaturePadDrawable.Draw` draws, in this order:
1. The signature line, three quarters of the way down, 16 DIP from each side, 1 DIP thick.
2. The placeholder, centred above the line at font size 16, while the pad is empty and nothing is being drawn.
3. The committed strokes.
4. The stroke being drawn, rebuilt from the builder's points every frame.

The guides are drawn only here, so they never appear in an export. When `DisplayStrokeColor` is set, the committed
strokes and the stroke being drawn are filled with it instead of their own colors; the data does not change.

Committed strokes are drawn differently per platform, behind `ICommittedInkRenderer`:
- **iOS, Mac Catalyst and Windows.**
  - `PathCacheInkRenderer` caches one `PathF` per stroke, keyed by reference, because strokes are immutable.
  - The platform canvas caches its native path on the `PathF`.
  - Paths of strokes that left the signature are released.
- **Android.**
  - Its canvas converts a `PathF` to a native path on every fill, which would make each frame cost as much as all the
    ink so far. So `CommittedInkLayer` keeps the committed ink in a bitmap the size of the view, and a frame draws one
    bitmap.
  - A new stroke is added to the bitmap.
  - Undo, clear, loading, a new size or a new display color redraws it.

Invalidation:
- **Android and Windows.** The whole view is invalidated; the platforms draw at most once a frame.
- **iOS and Mac Catalyst.**
  - CoreGraphics draws on the CPU, so while a stroke is drawn only its changed tail is redrawn
    (`TakeActiveDirtyRect`).
  - The tail reaches back over the end noise and twice the widest ink.
  - The margin covers the ink, the round caps and the bulge of the spline between far apart points.

**Hot paths.** Input callbacks and `Draw` avoid allocating per sample: struct samples, reused buffers, indexed loops,
and no LINQ.

## Export

**Layout.** `ExportLayout.Compute` picks the area to export:
- **Area.** The bounds of the ink plus the padding when cropping, otherwise the whole pad.
- **Size.** The image is that area times the scale, rounded up and at least 1 × 1 pixel.
- **Limits.**
  - The image is at most 16,384 pixels per side and 64 megapixels in total.
  - The limits come from GPU texture sizes and a 256 MB memory budget.
  - SVG has no pixel limit.
- **Empty signatures.** An empty signature cannot be exported.

**Images.** `SignatureImageExporter` draws on a background thread with the same `InkRenderer` as the pad. The bitmap
canvases cannot be trusted to transform paths (Android's ignores its scale), so the geometry is moved and scaled
itself.

| Target | Bitmap | Encoder |
| --- | --- | --- |
| Android | `Bitmap` with a `Canvas` and the Maui.Graphics `PlatformCanvas` | `Bitmap.Compress` |
| iOS, Mac Catalyst | `CGBitmapContext`, flipped to a top-left origin | `UIImage.AsPNG` or `AsJPEG` |
| Windows | Win2D `CanvasRenderTarget` at 96 DPI | `CanvasRenderTarget.SaveAsync` |
| `net10.0` | none | throws `PlatformNotSupportedException` |

**SVG.** `SvgExporter` writes one filled `<path>` per stroke, with an optional background `<rect>`. The output is
deterministic.

### JSON format, version 1

```json
{"format":"plugin.maui.signaturepad","version":1,"width":320,"height":200,
 "strokes":[{"device":"pen","color":"#000000FF","minWidth":1,"maxWidth":3.5,
             "x":[10.5,11.25],"y":[20,21.5],"t":[0,4.17],"p":[0.5,0.52]}]}
```

- **Fields.**
  - `width` and `height` are the canvas size.
  - Each stroke has its `device` (`unknown`, `touch`, `pen` or `mouse`), its `color` as `#RRGGBBAA`, and its widths.
  - Each stroke has parallel arrays of points: `x`, `y`, `t` (ms), and `p` (pressure). `p` is left out when no point
    has pressure; `null` marks a single point without it.
- **Writing.**
  - `Utf8JsonWriter`, with no reflection.
  - Numbers in their shortest round-trip form, so reading the text gives back the exact values.
- **Reading.** `Utf8JsonReader` rejects the whole input with a `FormatException` that names the bad field when:
  - the `format` is wrong, or the `version` is not 1;
  - a field is missing or has the wrong type;
  - a number is not finite, or a pressure is outside 0 to 1;
  - arrays of one stroke differ in length, or a stroke has no points;
  - there are more than 10,000 strokes or 100,000 points.
- **Unknown properties** are ignored, so a later version can add fields.

## Tests

- **Unit tests** (`tests/Plugin.Maui.SignaturePad.Tests`) cover all the shared code: the ink formulas, the
  geometry, JSON, export layout and SVG, the session, the drawable through a recording canvas, and the view. The gate
  is 100% line and branch coverage.
- **UI tests** (`tests/Plugin.Maui.SignaturePad.UITests`) drive a host app with Appium on iOS, Android, Mac Catalyst and
  Windows. They check:
  - native input, scrolling, tabs, `BindableLayout`, `IsEnabled` and export;
  - screenshots of fixed signatures against baselines.
- **Manual checks** ([manual-testing.md](manual-testing.md)) cover what cannot be automated: pens, real devices and
  Windows.
