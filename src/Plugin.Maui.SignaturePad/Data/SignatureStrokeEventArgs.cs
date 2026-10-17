namespace Plugin.Maui.SignaturePad;

/// <summary>
/// Data for an event about a stroke, such as <see cref="SignaturePadView.StrokeCompleted"/>.
/// </summary>
public sealed class SignatureStrokeEventArgs : EventArgs
{
    /// <summary>
    /// Creates the event data.
    /// </summary>
    /// <param name="stroke">The stroke the event is about.</param>
    /// <exception cref="ArgumentNullException"><paramref name="stroke"/> is <see langword="null"/>.</exception>
    public SignatureStrokeEventArgs(SignatureStroke stroke)
    {
        ArgumentNullException.ThrowIfNull(stroke);
        Stroke = stroke;
    }

    /// <summary>The stroke the event is about.</summary>
    public SignatureStroke Stroke { get; }
}
