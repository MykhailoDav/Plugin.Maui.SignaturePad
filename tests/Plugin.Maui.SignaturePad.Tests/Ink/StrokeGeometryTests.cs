using System.Globalization;
using Plugin.Maui.SignaturePad.Ink;

namespace Plugin.Maui.SignaturePad.Tests.Ink;

public class StrokeGeometryTests
{
    private static readonly PointF[] Square = [new(0, 0), new(10, 0), new(10, 10), new(0, 10)];

    [Fact]
    public void ToSvgPathData_MatchesGolden() =>
        Assert.Equal("M5 0 Q10 0 10 5 Q10 10 5 10 Q0 10 0 5 Q0 0 5 0 Z", StrokeGeometry.ToSvgPathData(Square));

    [Fact]
    public void ToSvgPathData_WritesAtMostTwoDecimals_WithoutNegativeZero()
    {
        PointF[] outline = [new(0.004f, -0.004f), new(1.239f, 0), new(1.239f, 2.5f)];

        Assert.Equal("M0.62 0 Q1.24 0 1.24 1.25 Q1.24 2.5 0.62 1.25 Q0 0 0.62 0 Z", StrokeGeometry.ToSvgPathData(outline));
    }

    [Fact]
    public void ToSvgPathData_IsCultureInvariant()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            Assert.Equal("M5 0 Q10 0 10 5 Q10 10 5 10 Q0 10 0 5 Q0 0 5 0 Z", StrokeGeometry.ToSvgPathData(Square));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void ToSvgPathData_CanOffsetTheOutline() =>
        Assert.Equal("M3 -2 Q8 -2 8 3 Q8 8 3 8 Q-2 8 -2 3 Q-2 -2 3 -2 Z", StrokeGeometry.ToSvgPathData(Square, new PointF(2, 2)));

    [Fact]
    public void ToSvgPathData_Empty_IsEmpty() =>
        Assert.Equal("", StrokeGeometry.ToSvgPathData([]));

    [Fact]
    public void ToPath_HasTheSameSegmentsAsTheSvg()
    {
        using var path = StrokeGeometry.ToPath(Square);

        Assert.Equal(new PointF(5, 0), path[0]);
        Assert.Equal(1 + (2 * Square.Length), path.Count);
        Assert.Equal(new PointF(10, 0), path[1]);
        Assert.Equal(new PointF(10, 5), path[2]);
        Assert.Equal(new PointF(0, 0), path[7]);
        Assert.Equal(new PointF(5, 0), path[8]);
        Assert.True(path.Closed);
        Assert.Equal(Square.Length, path.SegmentCountExcludingOpenAndClose);
    }

    [Fact]
    public void ToPath_Empty_IsEmptyPath()
    {
        using var path = StrokeGeometry.ToPath([]);

        Assert.Equal(0, path.Count);
    }

    [Fact]
    public void Bounds_ContainsAllOutlinePoints() =>
        Assert.Equal(new RectF(-1, 2, 11, 8), StrokeGeometry.Bounds([new(0, 2), new(10, 5), new(-1, 10)]));

    [Fact]
    public void Bounds_Empty_IsZero() =>
        Assert.Equal(RectF.Zero, StrokeGeometry.Bounds([]));

    [Fact]
    public void Stroke_Bounds_IsTheOutlineBounds_AndCached()
    {
        var stroke = new SignatureStroke([new(10, 20, 0), new(60, 20, 50)], Colors.Black, 2, 2, SignatureInputDevice.Touch);

        var bounds = stroke.Bounds;

        Assert.Equal(9, bounds.Left, 1);
        Assert.Equal(61, bounds.Right, 1);
        Assert.Equal(19, bounds.Top, 1);
        Assert.Equal(21, bounds.Bottom, 1);
        Assert.Equal(bounds, stroke.Bounds);
        Assert.Same(stroke.Outline, stroke.Outline);
    }

    [Fact]
    public void Data_Bounds_IsTheUnionOfStrokes()
    {
        var a = new SignatureStroke([new(10, 20, 0), new(60, 20, 50)], Colors.Black, 2, 2, SignatureInputDevice.Touch);
        var b = new SignatureStroke([new(100, 100, 60)], Colors.Black, 2, 2, SignatureInputDevice.Touch);

        var data = new SignatureData([a, b], new SizeF(200, 200));

        Assert.Equal(RectF.Union(a.Bounds, b.Bounds), data.Bounds);
        Assert.Equal(data.Bounds, data.Bounds);
    }

    [Fact]
    public void Data_Bounds_Empty_IsZero() =>
        Assert.Equal(RectF.Zero, SignatureData.Empty.Bounds);
}
