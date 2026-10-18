# Manual testing

The unit tests cover the shared code and the UI tests cover input, layouts and rendering on simulators and emulators.
What remains needs a person with real hardware:
- pen pressure;
- the feel of the ink at real sampling rates;
- Windows, which has no automated run on a device yet.

Use the sample app (`samples/Plugin.Maui.SignaturePad.Sample`) in a Release build, because Debug builds draw more
slowly.

## Devices

| Device | Input | Why |
| --- | --- | --- |
| iPhone | Finger | Coalesced touches at 120 Hz or more, and ProMotion displays |
| iPad | Apple Pencil, finger | Pencil pressure at 240 Hz, and a palm on the screen (see the known limitations) |
| Mac (Mac Catalyst) | Trackpad, mouse | Indirect pointer input, secondary clicks |
| Android phone | Finger | Batched historical samples, different touch rates per vendor |
| Android tablet or phone with a stylus (Samsung S Pen and similar) | Stylus, finger | Stylus pressure and the eraser end |
| Windows PC with touch and a pen (Surface or similar) | Pen, touch, mouse | Pen pressure, intermediate points, pointer capture |

## Checks

Do these on each device, with each of its inputs.

**Ink**

- [ ] A slow stroke is noticeably thicker than a fast one.
- [ ] Fast loops, such as a quick "e" or "l", stay round and do not turn into spikes.
- [ ] With a pen, pressing harder makes the ink thicker, and the Ink tab shows a pressure range for the last stroke.
- [ ] With a finger or a mouse, the Ink tab says there is no pressure.
- [ ] A tap draws a round dot.
- [ ] The ink follows the pointer without visible lag, also with 30 or more strokes on the pad.
- [ ] Rotating the device or resizing the window keeps the ink where it was drawn.

**Input**

- [ ] A second finger during a stroke neither draws nor breaks the stroke.
- [ ] Android: the stylus eraser does not draw.
- [ ] Windows: the pen's eraser end and its barrel button do not draw.
- [ ] Mac Catalyst and Windows: a right click does not draw.
- [ ] Windows: a mouse stroke that leaves the pad and is released outside it ends cleanly.

**Layouts (Form tab)**

- [ ] Drawing on the pad does not scroll the page; swiping outside the pad does.
- [ ] iOS: the back swipe and the Shell flyout still work outside the pad.
- [ ] After Submit, the pad does not take input; New delivery makes it work again.
- [ ] The ink is still there after switching tabs and coming back.

**Export (Export and Sign tabs)**

- [ ] PNG, JPEG and SVG look exactly like the pad, at every scale.
- [ ] A transparent PNG shared to another app keeps its transparency.
- [ ] Share opens the system share sheet with the file.

**Data (Data tab)**

- [ ] Save, then restart the app, then Restore: the signature comes back exactly, and Undo is disabled.
- [ ] On a pen device, the restored data keeps the pressure.

## Known limitations

- **Palm rejection.** The first touch draws. A palm that touches an iPad before the Pencil draws, and the iPadOS setting
  "Only Draw with Apple Pencil" is not followed yet.

## Reporting

Record the device, OS version, input, package version and what you saw. For an ink problem, the JSON from the Data tab's
Copy button reproduces it exactly. Attach it to the issue.
