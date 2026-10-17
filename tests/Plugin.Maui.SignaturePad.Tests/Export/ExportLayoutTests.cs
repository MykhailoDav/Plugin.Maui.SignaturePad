namespace Plugin.Maui.SignaturePad.Tests.Export;

public class ExportLayoutTests
{
    private static SignatureData Data(SizeF canvas, params SignatureStroke[] strokes) => new(strokes, canvas);

    private static SignatureStroke Line(float x1, float y1, float x2, float y2) =>
        new([new(x1, y1, 0), new(x2, y2, 50)], Colors.Black, 2, 2, SignatureInputDevice.Touch);

    [Fact]
    public void Compute_Crop_InflatesTheInkBoundsByThePadding()
    {
        var data = Data(new SizeF(300, 200), Line(10, 20, 60, 20));

        var layout = ExportLayout.Compute(data, cropToContent: true, padding: 8, scale: 1);

        var bounds = data.Bounds;
        Assert.Equal(new RectF(bounds.X - 8, bounds.Y - 8, bounds.Width + 16, bounds.Height + 16), layout.Source);
    }

    [Fact]
    public void Compute_NoCrop_UsesTheCanvas()
    {
        var layout = ExportLayout.Compute(Data(new SizeF(300, 200), Line(10, 20, 60, 20)), cropToContent: false, padding: 8, scale: 1);

        Assert.Equal(new RectF(0, 0, 300, 200), layout.Source);
        Assert.Equal(300, layout.PixelWidth);
        Assert.Equal(200, layout.PixelHeight);
    }

    [Fact]
    public void Compute_PixelSize_IsTheCeilingOfTheScaledSize()
    {
        var layout = ExportLayout.Compute(Data(new SizeF(100.2f, 50.5f), Line(10, 20, 60, 20)), cropToContent: false, padding: 0, scale: 2);

        Assert.Equal(201, layout.PixelWidth);
        Assert.Equal(101, layout.PixelHeight);
    }

    [Fact]
    public void Compute_IsAtLeastOnePixel()
    {
        var layout = ExportLayout.Compute(Data(SizeF.Zero, Line(10, 20, 60, 20)), cropToContent: false, padding: 0, scale: 1);

        Assert.Equal(1, layout.PixelWidth);
        Assert.Equal(1, layout.PixelHeight);
    }

    [Fact]
    public void Compute_Empty_Throws() =>
        Assert.Throws<InvalidOperationException>(() => ExportLayout.Compute(SignatureData.Empty, true, 8, 1));

    [Theory]
    [InlineData(20000f, 10f)]
    [InlineData(10f, 20000f)]
    [InlineData(10000f, 10000f)]
    public void Compute_TooManyPixels_Throws(float width, float height)
    {
        var data = Data(new SizeF(width, height), Line(10, 20, 60, 20));

        var exception = Assert.Throws<InvalidOperationException>(() => ExportLayout.Compute(data, false, 0, 1));
        Assert.Contains("pixels", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Compute_AtTheLimits_Succeeds()
    {
        var layout = ExportLayout.Compute(Data(new SizeF(16384, 4096), Line(10, 20, 60, 20)), false, 0, 1);

        Assert.Equal(16384, layout.PixelWidth);
        Assert.Equal(4096, layout.PixelHeight);
    }

    [Fact]
    public void SourceRect_HasNoPixelLimit() =>
        Assert.Equal(new RectF(0, 0, 100000, 100000), ExportLayout.SourceRect(Data(new SizeF(100000, 100000), Line(10, 20, 60, 20)), false, 0));

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(16.01f)]
    [InlineData(float.NaN)]
    public void ImageOptions_RejectScaleOutOfRange(float scale) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new SignatureImageExportOptions { Scale = scale });

    [Theory]
    [InlineData(-1f)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NaN)]
    public void Options_RejectNegativeOrNonFinitePadding(float padding)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SignatureImageExportOptions { Padding = padding });
        Assert.Throws<ArgumentOutOfRangeException>(() => new SignatureSvgExportOptions { Padding = padding });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void ImageOptions_RejectJpegQualityOutOfRange(int quality) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new SignatureImageExportOptions { JpegQuality = quality });

    [Fact]
    public void ImageOptions_HaveTheDocumentedDefaults()
    {
        var options = new SignatureImageExportOptions();

        Assert.Equal(SignatureImageFormat.Png, options.Format);
        Assert.Equal(2f, options.Scale);
        Assert.True(options.CropToContent);
        Assert.Equal(8f, options.Padding);
        Assert.Null(options.BackgroundColor);
        Assert.Null(options.StrokeColor);
        Assert.Equal(90, options.JpegQuality);
    }

    [Fact]
    public void ImageOptions_KeepValidValues()
    {
        var options = new SignatureImageExportOptions
        {
            Format = SignatureImageFormat.Jpeg,
            Scale = 16,
            CropToContent = false,
            Padding = 0,
            BackgroundColor = Colors.White,
            StrokeColor = Colors.Blue,
            JpegQuality = 1,
        };

        Assert.Equal(SignatureImageFormat.Jpeg, options.Format);
        Assert.Equal(16f, options.Scale);
        Assert.False(options.CropToContent);
        Assert.Equal(0f, options.Padding);
        Assert.Equal(Colors.White, options.BackgroundColor);
        Assert.Equal(Colors.Blue, options.StrokeColor);
        Assert.Equal(1, options.JpegQuality);
    }

    [Fact]
    public void ImageOptions_RejectUndefinedFormat() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new SignatureImageExportOptions { Format = (SignatureImageFormat)7 });

    [Fact]
    public void SvgOptions_HaveTheDocumentedDefaults()
    {
        var options = new SignatureSvgExportOptions();

        Assert.True(options.CropToContent);
        Assert.Equal(8f, options.Padding);
        Assert.Null(options.BackgroundColor);
        Assert.Null(options.StrokeColor);
    }
}
