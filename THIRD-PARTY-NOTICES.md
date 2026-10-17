# Third-party notices

Plugin.Maui.SignaturePad includes code adapted from the projects below. Their licenses apply to that code.

## perfect-freehand

The stroke outline in `src/Plugin.Maui.SignaturePad/Ink/StrokeOutline.cs` is a C# port of `getStrokePoints` and
`getStrokeOutlinePoints` from [perfect-freehand](https://github.com/steveruizok/perfect-freehand) 1.2 (commit
`176e00f`), adapted to take an explicit ink width per point.

```
MIT License

Copyright (c) 2021 Stephen Ruiz Ltd

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## signature_pad

The velocity-based ink width follows the approach of [signature_pad](https://github.com/szimek/signature_pad) by Szymon
Nowak (MIT): the stroke gets thinner the faster it is drawn, with the velocity smoothed by an exponential filter. No
code was copied.
