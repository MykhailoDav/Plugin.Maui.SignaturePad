namespace Plugin.Maui.SignaturePad.Rendering;

/// <summary>
/// Keeps the path of every committed stroke, so a frame only fills cached paths. On iOS and Windows the platform
/// canvas also caches its native path on the <see cref="PathF"/>, so reusing the same instance avoids converting it again.
/// </summary>
internal sealed class StrokePathCache : IDisposable
{
    // Strokes are immutable, so a stroke's path never changes; keying by reference is both correct and fast.
    private readonly Dictionary<SignatureStroke, PathF> _paths = new(ReferenceEqualityComparer.Instance);

    public int Count => _paths.Count;

    public PathF Get(SignatureStroke stroke)
    {
        if (!_paths.TryGetValue(stroke, out var path))
        {
            path = Ink.StrokeGeometry.ToPath(stroke.Outline);
            _paths.Add(stroke, path);
        }

        return path;
    }

    /// <summary>Releases the paths of strokes that are no longer in <paramref name="alive"/>.</summary>
    public void Retain(IReadOnlyList<SignatureStroke> alive)
    {
        if (_paths.Count == 0)
            return;

        var keep = new HashSet<SignatureStroke>(alive.Count, ReferenceEqualityComparer.Instance);
        for (var i = 0; i < alive.Count; i++)
            keep.Add(alive[i]);

        List<SignatureStroke>? removed = null;
        foreach (var stroke in _paths.Keys)
        {
            if (!keep.Contains(stroke))
                (removed ??= []).Add(stroke);
        }

        if (removed is null)
            return;

        foreach (var stroke in removed)
        {
            _paths[stroke].Dispose();
            _paths.Remove(stroke);
        }
    }

    public void Dispose()
    {
        foreach (var path in _paths.Values)
            path.Dispose();
        _paths.Clear();
    }
}
