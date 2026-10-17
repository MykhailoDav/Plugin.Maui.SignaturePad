namespace Plugin.Maui.SignaturePad.Capture;

/// <summary>
/// Follows the one pointer that is drawing. Other pointers (a second finger, the palm resting on the screen) are ignored
/// until it lifts, so they can neither start a stroke nor end the current one.
/// </summary>
internal sealed class PointerTracker
{
    private long? _pointerId;

    public bool IsTracking => _pointerId.HasValue;

    /// <summary>Starts following <paramref name="pointerId"/> unless another pointer is drawing.</summary>
    public bool TryBegin(long pointerId)
    {
        if (_pointerId.HasValue)
            return false;

        _pointerId = pointerId;
        return true;
    }

    public bool IsTracked(long pointerId) => _pointerId == pointerId;

    /// <summary>Stops following <paramref name="pointerId"/>; returns whether it was the one drawing.</summary>
    public bool End(long pointerId)
    {
        if (_pointerId != pointerId)
            return false;

        _pointerId = null;
        return true;
    }

    public void Reset() => _pointerId = null;
}
