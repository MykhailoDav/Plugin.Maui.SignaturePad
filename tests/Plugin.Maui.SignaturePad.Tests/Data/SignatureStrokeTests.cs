namespace Plugin.Maui.SignaturePad.Tests.Data;

public class SignatureStrokeTests
{
    private static SignatureStroke Stroke(params SignaturePoint[] points) =>
        new(points, Colors.Black, 1f, 3.5f, SignatureInputDevice.Touch);

    [Fact]
    public void Stroke_CopiesPoints_AndIsImmutable()
    {
        var source = new List<SignaturePoint> { new(1, 2, 0), new(3, 4, 5) };

        var stroke = Stroke([.. source]);
        source[0] = new SignaturePoint(100, 100, 100);

        Assert.Equal(new SignaturePoint(1, 2, 0), stroke.Points[0]);
        Assert.False(stroke.Points is SignaturePoint[], "Points must not expose the backing array.");
    }

    [Fact]
    public void Stroke_KeepsItsProperties()
    {
        var stroke = new SignatureStroke([new(1, 2, 0, 0.5f)], Colors.Red, 0.5f, 4f, SignatureInputDevice.Pen);

        Assert.Equal(Colors.Red, stroke.Color);
        Assert.Equal(0.5f, stroke.MinWidth);
        Assert.Equal(4f, stroke.MaxWidth);
        Assert.Equal(SignatureInputDevice.Pen, stroke.Device);
        Assert.Equal(0.5f, stroke.Points[0].Pressure);
    }

    [Fact]
    public void Stroke_StartEndTime_FromFirstAndLastPoint()
    {
        var stroke = Stroke(new(0, 0, 12.5), new(1, 1, 20), new(2, 2, 47.25));

        Assert.Equal(12.5, stroke.StartTime);
        Assert.Equal(47.25, stroke.EndTime);
    }

    [Fact]
    public void Stroke_Rejects_NullOrEmptyPoints()
    {
        Assert.Throws<ArgumentNullException>(() => new SignatureStroke(null!, Colors.Black, 1, 2, SignatureInputDevice.Touch));
        Assert.Throws<ArgumentException>(() => Stroke());
    }

    [Fact]
    public void Stroke_Rejects_NullColor() =>
        Assert.Throws<ArgumentNullException>(() => new SignatureStroke([new(0, 0, 0)], null!, 1, 2, SignatureInputDevice.Touch));

    [Theory]
    [InlineData(float.NaN, 0f, 0d)]
    [InlineData(0f, float.PositiveInfinity, 0d)]
    [InlineData(0f, 0f, double.NaN)]
    [InlineData(0f, 0f, double.NegativeInfinity)]
    public void Stroke_Rejects_NonFiniteCoordinates(float x, float y, double time) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Stroke(new SignaturePoint(x, y, time)));

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(-0.01f)]
    [InlineData(1.01f)]
    public void Stroke_Rejects_PressureOutsideZeroToOne(float pressure) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Stroke(new SignaturePoint(0, 0, 0, pressure)));

    [Theory]
    [InlineData(0f, 1f)]
    [InlineData(-1f, 1f)]
    [InlineData(float.NaN, 1f)]
    [InlineData(2f, 1f)]
    [InlineData(1f, float.PositiveInfinity)]
    public void Stroke_Rejects_InvalidWidths(float minWidth, float maxWidth) =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SignatureStroke([new(0, 0, 0)], Colors.Black, minWidth, maxWidth, SignatureInputDevice.Touch));

    [Fact]
    public void Stroke_Rejects_UndefinedDevice() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SignatureStroke([new(0, 0, 0)], Colors.Black, 1, 2, (SignatureInputDevice)42));

    [Fact]
    public void Stroke_Equality_IsStructural()
    {
        SignaturePoint[] points = [new(1, 2, 0, 0.5f), new(3, 4, 5, 0.6f)];
        var a = new SignatureStroke(points, Colors.Black, 1, 3.5f, SignatureInputDevice.Pen);
        var b = new SignatureStroke(points, Color.FromRgba("#000000FF"), 1, 3.5f, SignatureInputDevice.Pen);

        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Stroke_Equality_DetectsEveryDifference()
    {
        SignaturePoint[] points = [new(1, 2, 0, 0.5f), new(3, 4, 5, 0.6f)];
        var a = new SignatureStroke(points, Colors.Black, 1, 3.5f, SignatureInputDevice.Pen);

        Assert.False(a.Equals(new SignatureStroke([new(1, 2, 0, 0.5f), new(3, 4, 5, 0.7f)], Colors.Black, 1, 3.5f, SignatureInputDevice.Pen)));
        Assert.False(a.Equals(new SignatureStroke([new(1, 2, 0, 0.5f)], Colors.Black, 1, 3.5f, SignatureInputDevice.Pen)));
        Assert.False(a.Equals(new SignatureStroke(points, Colors.Blue, 1, 3.5f, SignatureInputDevice.Pen)));
        Assert.False(a.Equals(new SignatureStroke(points, Colors.Black, 2, 3.5f, SignatureInputDevice.Pen)));
        Assert.False(a.Equals(new SignatureStroke(points, Colors.Black, 1, 3f, SignatureInputDevice.Pen)));
        Assert.False(a.Equals(new SignatureStroke(points, Colors.Black, 1, 3.5f, SignatureInputDevice.Mouse)));
        Assert.False(a.Equals(null));
        Assert.False(a.Equals("stroke"));
        Assert.True(a.Equals(a));
    }
}
