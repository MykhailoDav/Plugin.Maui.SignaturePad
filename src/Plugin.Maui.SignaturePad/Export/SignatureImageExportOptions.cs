namespace Plugin.Maui.SignaturePad;

/// <summary>
/// Options for exporting a signature as a PNG or JPEG image.
/// </summary>
public sealed class SignatureImageExportOptions
{
    private SignatureImageFormat _format = SignatureImageFormat.Png;
    private float _scale = 2f;
    private float _padding = 8f;
    private int _jpegQuality = 90;

    /// <summary>The image format. The default is <see cref="SignatureImageFormat.Png"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a defined format.</exception>
    public SignatureImageFormat Format
    {
        get => _format;
        set => _format = Enum.IsDefined(value) ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "The format is not a defined value.");
    }

    /// <summary>
    /// The number of image pixels per device-independent unit of the pad, from just above 0 to 16. The default is 2;
    /// use the display density for an image as sharp as the screen, or more for print.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not greater than 0 and at most 16.</exception>
    public float Scale
    {
        get => _scale;
        set => _scale = value > 0 && value <= 16 ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "The scale must be greater than 0 and at most 16.");
    }

    /// <summary>
    /// Whether the image covers only the ink plus <see cref="Padding"/> (the default) instead of the whole pad.
    /// </summary>
    public bool CropToContent { get; set; } = true;

    /// <summary>The margin around the ink when cropping, in device-independent units. The default is 8.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or not finite.</exception>
    public float Padding
    {
        get => _padding;
        set => _padding = ExportValidation.Padding(value);
    }

    /// <summary>
    /// The background color, or <see langword="null"/> (the default) for a transparent PNG or a white JPEG.
    /// </summary>
    public Color? BackgroundColor { get; set; }

    /// <summary>A color for all strokes instead of their own, or <see langword="null"/> (the default) to keep them.</summary>
    public Color? StrokeColor { get; set; }

    /// <summary>The JPEG quality from 1 to 100. The default is 90. PNG ignores it.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside 1 to 100.</exception>
    public int JpegQuality
    {
        get => _jpegQuality;
        set => _jpegQuality = value is >= 1 and <= 100 ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "The JPEG quality must be from 1 to 100.");
    }
}
