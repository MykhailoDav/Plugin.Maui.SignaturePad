namespace Plugin.Maui.SignaturePad.Rendering;

/// <summary>
/// Draws the committed strokes of the pad on each frame. Platforms whose canvas re-creates native paths on every fill
/// (Android) use a raster layer instead of the default path cache.
/// </summary>
internal interface ICommittedInkRenderer : IDisposable
{
    /// <summary>Draws the strokes of <paramref name="signature"/> on a pad of <paramref name="size"/> DIP.</summary>
    void Draw(ICanvas canvas, SignatureData signature, SizeF size);
}
