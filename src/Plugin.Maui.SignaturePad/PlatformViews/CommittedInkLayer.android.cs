using Android.Graphics;
using Microsoft.Maui.Graphics.Platform;
using Plugin.Maui.SignaturePad.Rendering;

namespace Plugin.Maui.SignaturePad;

/// <summary>
/// Keeps the committed strokes in a bitmap the size of the view, so a frame draws one bitmap however much ink there is.
/// Android's canvas turns a <see cref="PathF"/> into a native path on every fill, which would make each frame cost as
/// much as all the ink drawn so far. A new stroke is added to the bitmap; anything else (undo, clear, a new signature,
/// a new size) draws the bitmap again from the strokes.
/// </summary>
internal sealed class CommittedInkLayer(Func<(int Width, int Height, float Density)> pixelSize) : ICommittedInkRenderer
{
    private Bitmap? _bitmap;
    private PlatformImage? _image;
    private SignatureData? _drawn;

    public void Draw(ICanvas canvas, SignatureData signature, SizeF size)
    {
        var (width, height, density) = pixelSize();
        if (width <= 0 || height <= 0 || signature.IsEmpty)
            return;

        if (_bitmap is null || _bitmap.Width != width || _bitmap.Height != height)
        {
            Release();
            _bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888!)!;
            _image = new PlatformImage(_bitmap);
        }

        if (!ReferenceEquals(signature, _drawn))
            Update(signature, density);

        // The bitmap has exactly the view's pixels, so it is drawn at the view's size in DIP: one pixel per pixel.
        canvas.DrawImage(_image, 0, 0, width / density, height / density);
    }

    public void Dispose() => Release();

    private void Update(SignatureData signature, float density)
    {
        var strokes = signature.Strokes;
        var first = 0;
        if (_drawn is not null && StartsWith(strokes, _drawn.Strokes))
            first = _drawn.Strokes.Count;
        else
            _bitmap!.EraseColor(Android.Graphics.Color.Transparent);

        if (first < strokes.Count)
        {
            var added = new SignatureStroke[strokes.Count - first];
            for (var i = 0; i < added.Length; i++)
                added[i] = strokes[first + i];

            using var androidCanvas = new Canvas(_bitmap!);
            InkRenderer.DrawStrokes(new PlatformCanvas { Canvas = androidCanvas }, added, strokeColorOverride: null, Microsoft.Maui.Graphics.PointF.Zero, density);
        }

        _drawn = signature;
    }

    // Whether the new list keeps every stroke already drawn, in order, and only adds strokes after them.
    private static bool StartsWith(IReadOnlyList<SignatureStroke> strokes, IReadOnlyList<SignatureStroke> prefix)
    {
        if (strokes.Count < prefix.Count)
            return false;

        for (var i = 0; i < prefix.Count; i++)
        {
            if (!ReferenceEquals(strokes[i], prefix[i]))
                return false;
        }

        return true;
    }

    // The image owns the bitmap and disposes it. The bitmap is not recycled: the last frame drawn with it can still hold
    // it, and the garbage collector frees it once nothing does.
    private void Release()
    {
        _image?.Dispose();
        _image = null;
        _bitmap = null;
        _drawn = null;
    }
}
