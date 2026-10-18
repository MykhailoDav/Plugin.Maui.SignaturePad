namespace Plugin.Maui.SignaturePad.Tests.Data;

public class SignatureDataTests
{
    private static SignatureStroke Stroke(double start, double end, int pointCount = 2)
    {
        var points = new SignaturePoint[pointCount];
        for (var i = 0; i < pointCount; i++)
        {
            var t = pointCount == 1 ? start : start + ((end - start) * i / (pointCount - 1));
            points[i] = new SignaturePoint(i, i, t);
        }

        return new SignatureStroke(points, Colors.Black, 1, 3.5f, SignatureInputDevice.Touch);
    }

    [Fact]
    public void Data_Empty_IsEmptyWithZeroCounts()
    {
        var empty = SignatureData.Empty;

        Assert.True(empty.IsEmpty);
        Assert.Empty(empty.Strokes);
        Assert.Equal(0, empty.PointCount);
        Assert.Equal(0, empty.Duration);
        Assert.Equal(SizeF.Zero, empty.CanvasSize);
    }

    [Fact]
    public void Data_KeepsStrokesAndCanvasSize()
    {
        var stroke = Stroke(0, 10);

        var data = new SignatureData([stroke], new SizeF(320, 180));

        Assert.False(data.IsEmpty);
        Assert.Same(stroke, Assert.Single(data.Strokes));
        Assert.Equal(new SizeF(320, 180), data.CanvasSize);
    }

    [Fact]
    public void Data_CopiesStrokes_AndIsImmutable()
    {
        var source = new List<SignatureStroke> { Stroke(0, 10) };

        var data = new SignatureData(source, new SizeF(10, 10));
        source.Add(Stroke(20, 30));

        Assert.Single(data.Strokes);
        Assert.False(data.Strokes is SignatureStroke[], "Strokes must not expose the backing array.");
    }

    [Fact]
    public void Data_PointCount_SumsStrokes() =>
        Assert.Equal(7, new SignatureData([Stroke(0, 10, 3), Stroke(20, 30, 4)], SizeF.Zero).PointCount);

    [Fact]
    public void Data_Duration_IsLastEndMinusFirstStart() =>
        Assert.Equal(160, new SignatureData([Stroke(0, 40), Stroke(100, 160)], SizeF.Zero).Duration);

    [Fact]
    public void Data_Duration_DoesNotDependOnStrokeOrder() =>
        Assert.Equal(160, new SignatureData([Stroke(100, 160), Stroke(0, 40)], SizeF.Zero).Duration);

    [Fact]
    public void Data_Rejects_NullStrokesOrNullStroke()
    {
        Assert.Throws<ArgumentNullException>(() => new SignatureData(null!, SizeF.Zero));
        Assert.Throws<ArgumentException>(() => new SignatureData([Stroke(0, 1), null!], SizeF.Zero));
    }

    [Theory]
    [InlineData(-1f, 0f)]
    [InlineData(0f, -1f)]
    [InlineData(float.NaN, 0f)]
    [InlineData(0f, float.PositiveInfinity)]
    public void Data_Rejects_InvalidCanvasSize(float width, float height) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new SignatureData([], new SizeF(width, height)));

    [Fact]
    public void Data_Equality_IsStructural()
    {
        var a = new SignatureData([Stroke(0, 10), Stroke(20, 30)], new SizeF(320, 180));
        var b = new SignatureData([Stroke(0, 10), Stroke(20, 30)], new SizeF(320, 180));

        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.True(a.Equals(a));
    }

    [Fact]
    public void Data_Equality_DetectsEveryDifference()
    {
        var a = new SignatureData([Stroke(0, 10), Stroke(20, 30)], new SizeF(320, 180));

        Assert.False(a.Equals(new SignatureData([Stroke(0, 10)], new SizeF(320, 180))));
        Assert.False(a.Equals(new SignatureData([Stroke(0, 10), Stroke(20, 31)], new SizeF(320, 180))));
        Assert.False(a.Equals(new SignatureData([Stroke(0, 10), Stroke(20, 30)], new SizeF(320, 181))));
        Assert.False(a.Equals(null));
        Assert.False(a.Equals(new object()));
    }

    [Fact]
    public void Data_EmptyInstances_AreEqual()
    {
        Assert.Equal(SignatureData.Empty, new SignatureData([], SizeF.Zero));
        Assert.Equal(SignatureData.Empty.GetHashCode(), new SignatureData([], SizeF.Zero).GetHashCode());
    }

    [Fact]
    public void StrokeEventArgs_CarryTheStroke()
    {
        var stroke = Stroke(0, 1);

        Assert.Same(stroke, new SignatureStrokeEventArgs(stroke).Stroke);
        Assert.Throws<ArgumentNullException>(() => new SignatureStrokeEventArgs(null!));
    }
}
