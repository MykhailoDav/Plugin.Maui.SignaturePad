using System.Globalization;

namespace Plugin.Maui.SignaturePad.Sample.ViewModels;

/// <summary>Helpers for showing exported signatures on the pages.</summary>
internal static class Exports
{
    /// <summary>Reads an exported image into memory and disposes the export's stream.</summary>
    public static async Task<byte[]> ReadAllAsync(Task<Stream> export, CancellationToken cancellationToken)
    {
        await using var stream = await export;
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }

    /// <summary>An image source that can read the image as often as the page needs.</summary>
    public static ImageSource ToImageSource(byte[] image) => ImageSource.FromStream(() => new MemoryStream(image));

    public static string FormatSize(int bytes) =>
        bytes < 1024
            ? string.Format(CultureInfo.CurrentCulture, "{0} B", bytes)
            : string.Format(CultureInfo.CurrentCulture, "{0:0.0} KB", bytes / 1024.0);
}
