using Plugin.Maui.SignaturePad.Ink;

namespace Plugin.Maui.SignaturePad.Tests.Ink;

public class StrokeSplineTests
{
    private const float Tolerance = 1e-3f;

    private static float Distance(InkPoint a, InkPoint b) => MathF.Sqrt(((b.X - a.X) * (b.X - a.X)) + ((b.Y - a.Y) * (b.Y - a.Y)));

    private static InkPoint OnCircle(float radius, float angle) => new(radius * MathF.Cos(angle), radius * MathF.Sin(angle), 1f);

    [Fact]
    public void NoPoints_OrOnePoint_AreKept()
    {
        Assert.Equal(0, StrokeSpline.Densify([]).Length);

        InkPoint[] single = [new(1, 2, 3)];
        Assert.Equal(single, StrokeSpline.Densify(single).ToArray());
    }

    [Fact]
    public void SegmentsUpToMaxStep_AreKeptAsTheyAre()
    {
        InkPoint[] points = [new(0, 0, 1), new(StrokeSpline.MaxStep, 0, 1), new(3, 1, 1), new(3, 1, 2)];

        Assert.Equal(points, StrokeSpline.Densify(points).ToArray());
    }

    [Fact]
    public void LongSegment_IsSplitIntoEqualSteps_OfAtMostMaxStep()
    {
        var dense = StrokeSpline.Densify([new(0, 0, 1), new(10, 0, 1)]).ToArray();

        Assert.Equal(6, dense.Length);
        for (var i = 0; i < dense.Length; i++)
        {
            Assert.Equal(2f * i, dense[i].X, Tolerance);
            Assert.Equal(0f, dense[i].Y, Tolerance);
        }
    }

    [Fact]
    public void VeryLongSegment_GetsAtMostMaxStepsPerSegment()
    {
        var dense = StrokeSpline.Densify([new(0, 0, 1), new(10_000, 0, 1)]);

        Assert.Equal(StrokeSpline.MaxStepsPerSegment + 1, dense.Length);
    }

    [Fact]
    public void TheCurve_PassesThroughEveryPoint_InOrder()
    {
        InkPoint[] points = [new(0, 0, 1), new(20, 5, 1.5f), new(30, 30, 2), new(5, 40, 1), new(6, 41, 1)];

        var dense = StrokeSpline.Densify(points).ToArray();

        var index = 0;
        foreach (var point in points)
        {
            index = Array.IndexOf(dense, point, index);
            Assert.True(index >= 0, $"{point} is not on the curve in order.");
        }

        Assert.Equal(points[^1], dense[^1]);
        for (var i = 1; i < dense.Length; i++)
            Assert.True(Distance(dense[i - 1], dense[i]) <= StrokeSpline.MaxStep * 1.5f, $"Step {i} is {Distance(dense[i - 1], dense[i])}.");
    }

    [Fact]
    public void PointsOnALine_StayOnTheLine()
    {
        InkPoint[] points = [new(0, 0, 1), new(10, 10, 1), new(30, 30, 1), new(35, 35, 1)];

        Assert.All(StrokeSpline.Densify(points).ToArray(), point => Assert.Equal(point.X, point.Y, Tolerance));
    }

    [Fact]
    public void Radius_ChangesLinearlyAlongASegment()
    {
        var dense = StrokeSpline.Densify([new(0, 0, 1), new(8, 0, 3)]).ToArray();

        Assert.Equal(5, dense.Length);
        for (var i = 0; i < dense.Length; i++)
            Assert.Equal(1f + (0.5f * i), dense[i].Radius, Tolerance);
    }

    [Fact]
    public void SparseCircle_BecomesRound()
    {
        // Eight samples around a circle of radius 40, as a fast stroke gives: straight lines between them would cut
        // 3 DIP into the circle.
        const float radius = 40;
        var points = Enumerable.Range(0, 9).Select(i => OnCircle(radius, i * MathF.PI / 4)).ToArray();

        var dense = StrokeSpline.Densify(points).ToArray();

        // Away from the open ends, which have no neighbour to follow, the curve stays within half a DIP of the circle.
        var first = Array.IndexOf(dense, points[1]);
        var last = Array.IndexOf(dense, points[^2]);
        Assert.True(last - first > 50);
        for (var i = first; i <= last; i++)
            Assert.Equal(radius, MathF.Sqrt((dense[i].X * dense[i].X) + (dense[i].Y * dense[i].Y)), 0.5f);
    }

    [Fact]
    public void RepeatedNeighbours_DoNotBendTheCurve()
    {
        // A pen that presses harder without moving repeats its point; the curve treats it as a straight continuation.
        InkPoint[] points = [new(0, 0, 1), new(0, 0, 2), new(10, 0, 1), new(10, 0, 2)];

        var dense = StrokeSpline.Densify(points).ToArray();

        Assert.True(dense.Length > points.Length);
        Assert.All(dense, point => Assert.True(float.IsFinite(point.X) && point.Y == 0, $"{point}"));
    }
}
