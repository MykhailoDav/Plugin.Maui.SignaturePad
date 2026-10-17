# Plugin.Maui.SignaturePad

A free, MIT-licensed signature pad for .NET MAUI with natural, pen-like ink.

> **Status:** early planning. No package has been published yet.

## Goals

- **Natural ink.** Stroke width follows velocity and pen pressure (Apple Pencil, Windows pen, Android stylus), with smooth Bézier curves.
- **Every MAUI platform.** Android, iOS, Mac Catalyst and Windows with touch, mouse and pen.
- **Useful output.** PNG/JPEG, SVG, and raw stroke data with timestamps and pressure that can be saved and restored.
- **Signature essentials.** Undo and redo, empty-signature validation, and a "Sign here" placeholder with a baseline.
- **PDF add-on.** `Plugin.Maui.SignaturePad.Pdf` places a captured signature into a PDF document.
- **Pure managed code.** No native binaries to maintain.

## License

[MIT](LICENSE)
