namespace Plugin.Maui.SignaturePad;

/// <summary>
/// Options for exporting a signature as SVG.
/// </summary>
public sealed class SignatureSvgExportOptions
{
    private float _padding = 8f;

    /// <summary>
    /// Whether the SVG covers only the ink plus <see cref="Padding"/> (the default) instead of the whole pad.
    /// </summary>
    public bool CropToContent { get; set; } = true;

    /// <summary>The margin around the ink when cropping, in device-independent units. The default is 8.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or not finite.</exception>
    public float Padding
    {
        get => _padding;
        set => _padding = ExportValidation.Padding(value);
    }

    /// <summary>The background color, or <see langword="null"/> (the default) for a transparent background.</summary>
    public Color? BackgroundColor { get; set; }

    /// <summary>A color for all strokes instead of their own, or <see langword="null"/> (the default) to keep them.</summary>
    public Color? StrokeColor { get; set; }
}
