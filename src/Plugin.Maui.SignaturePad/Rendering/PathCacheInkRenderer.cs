namespace Plugin.Maui.SignaturePad.Rendering;

/// <summary>
/// Fills the cached path of every committed stroke. On iOS, Mac Catalyst and Windows the platform canvas keeps the
/// native path on the <see cref="PathF"/>, so a frame costs one fill per stroke and no geometry work.
/// </summary>
internal sealed class PathCacheInkRenderer : ICommittedInkRenderer
{
    private readonly StrokePathCache _cache = new();
    private SignatureData? _cachedFor;

    public int CachedPathCount => _cache.Count;

    public void Draw(ICanvas canvas, SignatureData signature, SizeF size, Color? strokeColorOverride)
    {
        if (!ReferenceEquals(signature, _cachedFor))
        {
            _cache.Retain(signature.Strokes);
            _cachedFor = signature;
        }

        InkRenderer.DrawStrokes(canvas, signature.Strokes, _cache, strokeColorOverride);
    }

    public void Dispose()
    {
        _cache.Dispose();
        _cachedFor = null;
    }
}
