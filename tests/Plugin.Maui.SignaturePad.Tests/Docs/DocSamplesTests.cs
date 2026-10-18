using System.Diagnostics;

namespace Plugin.Maui.SignaturePad.Tests.Docs;

/// <summary>
/// The C# samples of README.md and llms-full.txt, kept here so they keep compiling against the API. Samples that need a
/// platform (images, files, preferences, sharing) are compiled but not run. When a sample changes in the docs, change
/// it here too.
/// </summary>
public class DocSamplesTests
{
    private static SignatureData Signature() => new(
        [new SignatureStroke([new(10, 20, 0), new(60, 40, 120, 0.5f)], Colors.Black, 1, 3.5f, SignatureInputDevice.Pen)],
        new SizeF(320, 200));

    [Fact]
    public void SvgAndJsonSamples_Run()
    {
        var pad = new SignaturePadView { Signature = Signature() };

        // README: SVG.
        string svg = pad.ExportSvg(new SignatureSvgExportOptions { BackgroundColor = Colors.White });

        // README: JSON.
        string json = pad.Signature.ToJson();
        pad.Signature = SignatureData.FromJson(json);

        Assert.StartsWith("<svg", svg, StringComparison.Ordinal);
        Assert.Equal(Signature(), pad.Signature);
    }

    [Fact]
    public void CreateInCSharpSample_Runs()
    {
        // llms-full.txt: Create in C#.
        var pad = new SignaturePadView { HeightRequest = 200, Placeholder = "Sign here", StrokeColor = Colors.Navy };
        pad.StrokeCompleted += (_, e) => Debug.WriteLine($"{e.Stroke.Points.Count} points, {e.Stroke.Device}");

        Assert.Equal("Sign here", pad.Placeholder);
    }

    // README: Quick start, saving a PNG image.
    internal static async Task SavePng(SignaturePadView Pad)
    {
        if (!Pad.IsEmpty)
        {
            await using var png = await Pad.ExportImageAsync();
            await using var file = File.Create(Path.Combine(FileSystem.CacheDirectory, "signature.png"));
            await png.CopyToAsync(file);
        }
    }

    // README: Export, PNG and JPEG.
    internal static async Task ExportJpeg(SignaturePadView pad)
    {
        await using var jpeg = await pad.Signature.ToImageAsync(new SignatureImageExportOptions
        {
            Format = SignatureImageFormat.Jpeg,
            Scale = 3,
            BackgroundColor = Colors.White,
            StrokeColor = Colors.Navy,
        });
    }

    // llms-full.txt: Save the image to a file and share it.
    internal static async Task SaveAndShare(SignaturePadView pad)
    {
        await using var png = await pad.ExportImageAsync(new SignatureImageExportOptions { BackgroundColor = Colors.White });
        var path = Path.Combine(FileSystem.CacheDirectory, "signature.png");
        await using (var file = File.Create(path))
            await png.CopyToAsync(file);
        await Share.Default.RequestAsync(new ShareFileRequest("Signature", new ShareFile(path, "image/png")));
    }

    // llms-full.txt: Show the exported image in the UI.
    internal static async Task ShowImage(SignatureData signature, Image PreviewImage)
    {
        await using var stream = await signature.ToImageAsync();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        var bytes = buffer.ToArray();
        PreviewImage.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
    }

    // llms-full.txt: Persist and restore the strokes.
    internal static void PersistAndRestore(SignaturePadView pad)
    {
        Preferences.Default.Set("signature", pad.Signature.ToJson());
        var json = Preferences.Default.Get("signature", "");
        if (json.Length > 0)
        {
            try { pad.Signature = SignatureData.FromJson(json); }
            catch (FormatException) { /* corrupted or not a signature */ }
        }
    }
}
