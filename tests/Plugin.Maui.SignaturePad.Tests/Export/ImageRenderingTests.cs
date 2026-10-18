using Plugin.Maui.SignaturePad.Tests.Rendering;

namespace Plugin.Maui.SignaturePad.Tests.Export;

public class ImageRenderingTests
{
    private static SignatureData Data() => new(
    [
        new SignatureStroke([new(10, 20, 0), new(60, 20, 50)], Colors.Black, 2, 2, SignatureInputDevice.Touch),
        new SignatureStroke([new(80, 40, 60), new(90, 50, 80)], Colors.Blue, 2, 2, SignatureInputDevice.Touch),
    ], new SizeF(200, 100));

    private static RecordingCanvas Render(SignatureImageExportOptions options)
    {
        var data = Data();
        var layout = ExportLayout.Compute(data, options.CropToContent, options.Padding, options.Scale);
        var canvas = new RecordingCanvas();
        SignatureImageExporter.Render(canvas, data, layout, options);
        return canvas;
    }

    [Fact]
    public void Png_HasNoBackground_AndScalesThenMovesTheSourceToTheOrigin()
    {
        var options = new SignatureImageExportOptions { Scale = 3 };
        var source = ExportLayout.Compute(Data(), true, 8, 3).Source;

        var canvas = Render(options);

        Assert.Empty(canvas.FilledRectangles);
        Assert.Equal([(3f, 3f)], canvas.Scales);
        Assert.Equal([(-source.X, -source.Y)], canvas.Translations);
        Assert.Equal(["Scale", "Translate", "FillPath", "FillPath"], canvas.Calls);
    }

    [Fact]
    public void Jpeg_GetsAWhiteBackground_ByDefault()
    {
        var canvas = Render(new SignatureImageExportOptions { Format = SignatureImageFormat.Jpeg, CropToContent = false, Scale = 1 });

        var (rect, color) = Assert.Single(canvas.FilledRectangles);
        Assert.Equal(new RectF(0, 0, 200, 100), rect);
        Assert.Equal(Colors.White, color);
        Assert.Equal("FillRectangle", canvas.Calls[0]);
    }

    [Fact]
    public void Background_IsUsedWhenSet()
    {
        var canvas = Render(new SignatureImageExportOptions { BackgroundColor = Colors.Yellow });

        Assert.Equal(Colors.Yellow, Assert.Single(canvas.FilledRectangles).Color);
    }

    [Fact]
    public void Strokes_KeepTheirColors_UnlessOverridden()
    {
        Assert.Equal([Colors.Black, Colors.Blue], Render(new SignatureImageExportOptions()).Fills.Select(f => f.Color));
        Assert.All(Render(new SignatureImageExportOptions { StrokeColor = Colors.Red }).Fills, fill => Assert.Equal(Colors.Red, fill.Color));
    }
}
