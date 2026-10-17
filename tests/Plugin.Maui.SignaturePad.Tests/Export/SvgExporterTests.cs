using System.Globalization;
using System.Xml.Linq;

namespace Plugin.Maui.SignaturePad.Tests.Export;

public class SvgExporterTests
{
    private static SignatureData Fixture() => new(
    [
        new SignatureStroke([new(10, 20, 0), new(60, 20, 50)], Colors.Black, 2, 2, SignatureInputDevice.Touch),
        new SignatureStroke([new(100, 60, 80)], Color.FromRgba(0x1E, 0x88, 0xE5, 0x80), 2, 2, SignatureInputDevice.Pen),
    ], new SizeF(200, 100));

    private static XElement Parse(string svg) => XElement.Parse(svg);

    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    [Fact]
    public void Svg_IsWellFormed_WithOnePathPerStroke()
    {
        var svg = Parse(Fixture().ToSvg());

        Assert.Equal(Svg + "svg", svg.Name);
        Assert.Equal(2, svg.Elements(Svg + "path").Count());
    }

    [Fact]
    public void Svg_Cropped_SizeIsTheInkBoundsPlusPadding()
    {
        var data = Fixture();
        var svg = Parse(data.ToSvg());

        var width = data.Bounds.Width + 16;
        var height = data.Bounds.Height + 16;
        Assert.Equal(width, float.Parse(svg.Attribute("width")!.Value, CultureInfo.InvariantCulture), 2);
        Assert.Equal(height, float.Parse(svg.Attribute("height")!.Value, CultureInfo.InvariantCulture), 2);
        Assert.StartsWith("0 0 ", svg.Attribute("viewBox")!.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void Svg_Uncropped_UsesTheCanvas()
    {
        var svg = Parse(Fixture().ToSvg(new SignatureSvgExportOptions { CropToContent = false }));

        Assert.Equal("200", svg.Attribute("width")!.Value);
        Assert.Equal("100", svg.Attribute("height")!.Value);
        Assert.Equal("0 0 200 100", svg.Attribute("viewBox")!.Value);
    }

    [Fact]
    public void Svg_PathData_IsTheStrokeOutline_MovedIntoTheViewBox()
    {
        var data = Fixture();
        var source = ExportLayout.Compute(data, cropToContent: true, padding: 8, scale: 1).Source;

        var path = Parse(data.ToSvg()).Elements(Svg + "path").First();

        Assert.Equal(
            Plugin.Maui.SignaturePad.Ink.StrokeGeometry.ToSvgPathData(data.Strokes[0].Outline, source.Location),
            path.Attribute("d")!.Value);
    }

    [Fact]
    public void Svg_WritesFillColor_AndOpacityOnlyWhenTranslucent()
    {
        var paths = Parse(Fixture().ToSvg()).Elements(Svg + "path").ToArray();

        Assert.Equal("#000000", paths[0].Attribute("fill")!.Value);
        Assert.Null(paths[0].Attribute("fill-opacity"));
        Assert.Equal("#1E88E5", paths[1].Attribute("fill")!.Value);
        Assert.Equal("0.5", paths[1].Attribute("fill-opacity")!.Value);
    }

    [Fact]
    public void Svg_BackgroundRect_WhenColorSet()
    {
        var svg = Parse(Fixture().ToSvg(new SignatureSvgExportOptions { BackgroundColor = Colors.White, CropToContent = false }));

        var rect = Assert.Single(svg.Elements(Svg + "rect"));
        Assert.Equal("#FFFFFF", rect.Attribute("fill")!.Value);
        Assert.Equal("200", rect.Attribute("width")!.Value);
        Assert.Same(rect, svg.Elements().First());
    }

    [Fact]
    public void Svg_TranslucentBackground_WritesFillOpacity()
    {
        var svg = Parse(Fixture().ToSvg(new SignatureSvgExportOptions { BackgroundColor = Color.FromRgba(255, 255, 255, 64) }));

        Assert.Equal("0.25", svg.Element(Svg + "rect")!.Attribute("fill-opacity")!.Value);
    }

    [Fact]
    public void Svg_NoBackground_ByDefault() =>
        Assert.Empty(Parse(Fixture().ToSvg()).Elements(Svg + "rect"));

    [Fact]
    public void Svg_StrokeColorOverride_AppliesToEveryStroke()
    {
        var paths = Parse(Fixture().ToSvg(new SignatureSvgExportOptions { StrokeColor = Colors.Red })).Elements(Svg + "path");

        Assert.All(paths, path => Assert.Equal("#FF0000", path.Attribute("fill")!.Value));
        Assert.All(paths, path => Assert.Null(path.Attribute("fill-opacity")));
    }

    [Fact]
    public void Svg_IsDeterministic_AndCultureInvariant()
    {
        var expected = Fixture().ToSvg();
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            Assert.Equal(expected, Fixture().ToSvg());
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Svg_StartsWithTheSvgElement_WithoutAnXmlDeclaration() =>
        Assert.StartsWith("<svg xmlns=\"http://www.w3.org/2000/svg\" ", Fixture().ToSvg(), StringComparison.Ordinal);

    [Fact]
    public void Svg_Empty_Throws() =>
        Assert.Throws<InvalidOperationException>(() => SignatureData.Empty.ToSvg());
}
