namespace Plugin.Maui.SignaturePad;

/// <summary>
/// The part of the pad an export covers and the size of the image in pixels.
/// </summary>
/// <param name="Source">The exported area of the pad, in DIP.</param>
/// <param name="PixelWidth">The image width in pixels.</param>
/// <param name="PixelHeight">The image height in pixels.</param>
internal readonly record struct ExportLayout(RectF Source, int PixelWidth, int PixelHeight)
{
    /// <summary>The largest side of a bitmap that every platform can create (Win2D's and most GPUs' texture limit).</summary>
    internal const int MaxPixelDimension = 16384;

    /// <summary>The most pixels in an image (256 MB at 4 bytes per pixel), so a large scale cannot exhaust memory.</summary>
    internal const long MaxPixelCount = 64L * 1024 * 1024;

    /// <summary>The exported area: the ink bounds plus the padding when cropping, otherwise the whole pad.</summary>
    /// <exception cref="InvalidOperationException">The signature is empty.</exception>
    public static RectF SourceRect(SignatureData data, bool cropToContent, float padding)
    {
        if (data.IsEmpty)
            throw new InvalidOperationException("The signature is empty, so there is nothing to export.");

        if (!cropToContent)
            return new RectF(0, 0, data.CanvasSize.Width, data.CanvasSize.Height);

        var bounds = data.Bounds;
        return new RectF(bounds.X - padding, bounds.Y - padding, bounds.Width + (2 * padding), bounds.Height + (2 * padding));
    }

    /// <summary>Works out the exported area and the image size at <paramref name="scale"/> pixels per DIP.</summary>
    /// <exception cref="InvalidOperationException">The signature is empty, or the image would be too large.</exception>
    public static ExportLayout Compute(SignatureData data, bool cropToContent, float padding, float scale)
    {
        var source = SourceRect(data, cropToContent, padding);
        var width = Math.Max(1, Math.Ceiling(source.Width * (double)scale));
        var height = Math.Max(1, Math.Ceiling(source.Height * (double)scale));
        if (width > MaxPixelDimension || height > MaxPixelDimension || width * height > MaxPixelCount)
        {
            throw new InvalidOperationException(
                $"The image would be {width} × {height} pixels; the largest supported size is {MaxPixelDimension} pixels per side " +
                $"and {MaxPixelCount} pixels in total. Use a smaller scale or crop to the content.");
        }

        return new ExportLayout(source, (int)width, (int)height);
    }
}
