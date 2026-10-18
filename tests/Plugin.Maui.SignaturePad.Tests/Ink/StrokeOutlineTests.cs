using Plugin.Maui.SignaturePad.Ink;

namespace Plugin.Maui.SignaturePad.Tests.Ink;

public class StrokeOutlineTests
{
    private static InkPoint[] Line(int count, float step, float radius, float y = 0)
    {
        var points = new InkPoint[count];
        for (var i = 0; i < count; i++)
            points[i] = new InkPoint(i * step, y, radius);
        return points;
    }

    private static void AssertFinite(PointF[] outline)
    {
        Assert.NotEmpty(outline);
        foreach (var point in outline)
            Assert.True(float.IsFinite(point.X) && float.IsFinite(point.Y), $"Not finite: {point}");
    }

    [Fact]
    public void Empty_GivesNoOutline() =>
        Assert.Empty(StrokeOutline.Build([]));

    [Fact]
    public void SinglePoint_IsDot_CircleOfRadius()
    {
        var outline = StrokeOutline.Build([new InkPoint(10, 20, 1.5f)]);

        Assert.Equal(StrokeOutline.DotSegments, outline.Length);
        foreach (var point in outline)
            Assert.Equal(1.5, Math.Sqrt(Math.Pow(point.X - 10, 2) + Math.Pow(point.Y - 20, 2)), 3);
    }

    [Fact]
    public void PointsWithinHalfDip_AreDot_WithTheLargestRadius()
    {
        var outline = StrokeOutline.Build(
            [new InkPoint(10, 20, 1f), new InkPoint(10.2f, 20.1f, 1.4f), new InkPoint(10.3f, 20, 1.2f)]);

        Assert.Equal(StrokeOutline.DotSegments, outline.Length);
        foreach (var point in outline)
            Assert.Equal(1.4, Math.Sqrt(Math.Pow(point.X - 10, 2) + Math.Pow(point.Y - 20, 2)), 3);
    }

    [Fact]
    public void StraightLine_HasWidthTwoRadius()
    {
        var outline = StrokeOutline.Build(Line(60, 1f, 1.5f));

        AssertFinite(outline);
        var middle = outline.Where(p => p.X is > 20 and < 40).ToArray();
        Assert.NotEmpty(middle);
        foreach (var point in middle)
            Assert.Equal(1.5, Math.Abs(point.Y), 1);
        Assert.Contains(middle, p => p.Y > 1.4f);
        Assert.Contains(middle, p => p.Y < -1.4f);
    }

    [Fact]
    public void StraightLine_HasRoundCapsBeyondItsEnds()
    {
        var outline = StrokeOutline.Build(Line(60, 1f, 1.5f));

        Assert.Equal(-1.5, outline.Min(p => p.X), 1);
        Assert.Equal(59 + 1.5, outline.Max(p => p.X), 1);
    }

    [Fact]
    public void StraightLine_HasNoCornerPoints()
    {
        const int count = 60;

        var outline = StrokeOutline.Build(Line(count, 1f, 1.5f));

        Assert.True(outline.Length <= (2 * count) + StrokeOutline.StartCapSegments + StrokeOutline.EndCapSegments);
    }

    [Fact]
    public void UTurn_SharpCorner_AddsCapPoints_AndStaysFinite()
    {
        var points = Line(30, 1f, 1.5f).Concat(Line(30, -1f, 1.5f).Select(p => p with { X = 29 + p.X, Y = 0.5f })).ToArray();

        var outline = StrokeOutline.Build(points);

        AssertFinite(outline);
        Assert.True(outline.Length > 2 * StrokeOutline.CornerCapSegments);
    }

    public static TheoryData<int> Seeds => [1, 2, 3, 4, 5, 6, 7, 8];

    [Theory]
    [MemberData(nameof(Seeds))]
    public void RandomWalks_GiveFiniteOutlines(int seed)
    {
        var random = new Random(seed);
        var points = new InkPoint[200];
        float x = 0, y = 0;
        for (var i = 0; i < points.Length; i++)
        {
            x += (float)((random.NextDouble() - 0.5) * 8);
            y += (float)((random.NextDouble() - 0.5) * 8);
            points[i] = new InkPoint(x, y, 0.5f + (float)random.NextDouble());
        }

        AssertFinite(StrokeOutline.Build(points));
    }

    [Fact]
    public void Zigzag_WithRepeatedPoints_GivesFiniteOutline()
    {
        var points = new List<InkPoint>();
        for (var i = 0; i < 40; i++)
        {
            var point = new InkPoint(i % 2 == 0 ? 0 : 10, i, 1f);
            points.Add(point);
            points.Add(point);
        }

        AssertFinite(StrokeOutline.Build([.. points]));
    }

    [Fact]
    public void StrokeReturningToItsStart_WithinItsWidth_IsDot()
    {
        var outline = StrokeOutline.Build([new InkPoint(0, 0, 1f), new InkPoint(1, 0, 1f), new InkPoint(0, 0, 1f)]);

        Assert.Equal(StrokeOutline.DotSegments, outline.Length);
    }

    [Fact]
    public void HookAtTheVeryEnd_GetsACornerCap_AndStaysFinite()
    {
        // The turn is within the end noise, so the point before it is skipped and the corner is found from the
        // previous direction at the last point.
        var points = Line(31, 1f, 1.5f).Append(new InkPoint(28.5f, 0.5f, 1.5f)).ToArray();

        var outline = StrokeOutline.Build(points);

        AssertFinite(outline);
        Assert.True(outline.Length > 2 * StrokeOutline.CornerCapSegments);
    }

    [Fact]
    public void TinyRadius_IsRaisedToTheMinimum()
    {
        var outline = StrokeOutline.Build(Line(20, 1f, 0f));

        AssertFinite(outline);
        Assert.True(outline.Max(p => p.Y) > 0);
    }

    [Fact]
    public void ShortStroke_BelowItsWidth_KeepsBothEnds()
    {
        var outline = StrokeOutline.Build([new InkPoint(0, 0, 1.5f), new InkPoint(1, 0, 1.5f), new InkPoint(2, 0, 1.5f)]);

        AssertFinite(outline);
        Assert.Equal(2 + 1.5, outline.Max(p => p.X), 1);
    }

    [Fact]
    public void SparseCurve_IsDrawnRound_NotAsStraightSegments()
    {
        // Seven samples over three quarters of a circle of radius 40, as a fast stroke gives. Straight segments
        // between them would cut 3 DIP into the circle.
        const float radius = 40, ink = 1.5f;
        var points = Enumerable.Range(0, 7)
            .Select(i => new InkPoint(radius * MathF.Cos(i * MathF.PI / 4), radius * MathF.Sin(i * MathF.PI / 4), ink))
            .ToArray();

        var outline = StrokeOutline.Build(points);

        AssertFinite(outline);
        foreach (var point in outline)
        {
            // Away from the first and the last segment, whose open ends have no neighbour to follow, both sides of the
            // ink stay one ink radius from the circle: from 50° to 220°.
            var angle = MathF.Atan2(point.Y, point.X);
            if (angle is > 0.88f or < -2.45f)
                Assert.InRange(MathF.Sqrt((point.X * point.X) + (point.Y * point.Y)), radius - ink - 0.6f, radius + ink + 0.6f);
        }
    }
}
