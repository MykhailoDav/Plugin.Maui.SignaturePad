using Plugin.Maui.SignaturePad.Ink;

namespace Plugin.Maui.SignaturePad.Tests.Ink;

public class StrokeBuilderTests
{
    private const float Min = 1f;
    private const float Max = 3.5f;
    private const float Tolerance = 1e-4f;

    private static InkSample Touch(float x, float y, double t) => new(x, y, t, float.NaN, SignatureInputDevice.Touch);

    private static InkSample Pen(float x, float y, double t, float pressure) => new(x, y, t, pressure, SignatureInputDevice.Pen);

    private static StrokeBuilder Start(in InkSample first, double origin = 0) => new(first, Min, Max, origin);

    private static float Radius(float k) => (Min + ((Max - Min) * k)) / 2f;

    [Fact]
    public void FirstSample_IsAccepted_AtTimeZero()
    {
        var builder = Start(Touch(10, 20, 1000), origin: 1000);

        Assert.Equal(1, builder.Count);
        Assert.Equal(new SignaturePoint(10, 20, 0), builder.Points[0]);
        Assert.Equal(SignatureInputDevice.Touch, builder.Device);
        Assert.False(builder.IsCompleted);
    }

    [Fact]
    public void Sample_CloserThanMinDistance_IsDropped()
    {
        var builder = Start(Touch(10, 20, 0));

        builder.Add(Touch(10.2f, 20, 10));
        Assert.Equal(1, builder.Count);

        builder.Add(Touch(10.3f, 20, 20));
        Assert.Equal(2, builder.Count);
        Assert.Equal(10.3f, builder.Points[1].X);
    }

    [Fact]
    public void PenSample_WithPressureChange_IsKeptEvenIfClose()
    {
        var builder = Start(Pen(10, 20, 0, 0.5f));

        builder.Add(Pen(10, 20, 4, 0.53f));
        builder.Add(Pen(10, 20, 8, 0.54f));

        Assert.Equal(2, builder.Count);
        Assert.Equal(0.53f, builder.Points[1].Pressure);
    }

    [Fact]
    public void Pen_SampleWithoutPressure_StoresNull()
    {
        var builder = Start(Pen(0, 0, 0, 0.5f));

        builder.Add(Pen(5, 0, 4, float.NaN));

        Assert.Null(builder.Points[1].Pressure);
    }

    [Fact]
    public void Pen_PressureAppearsLater_CloseSampleIsDropped()
    {
        var builder = Start(Pen(0, 0, 0, float.NaN));

        builder.Add(Pen(0.1f, 0, 4, 0.9f));

        Assert.Equal(1, builder.Count);
    }

    [Fact]
    public void LastSample_IsKept_WhenNotIdentical_EvenIfClose()
    {
        var builder = Start(Touch(10, 20, 0));

        builder.Add(Touch(10.1f, 20, 10), isLast: true);

        Assert.Equal(2, builder.Count);
        Assert.True(builder.IsCompleted);
    }

    [Fact]
    public void LastSample_Identical_IsNotAddedTwice_ButCompletes()
    {
        var builder = Start(Touch(10, 20, 0));
        builder.Add(Touch(20, 20, 10));

        builder.Add(Touch(20, 20, 10), isLast: true);

        Assert.Equal(2, builder.Count);
        Assert.True(builder.IsCompleted);
    }

    [Fact]
    public void Coordinates_Times_AndPressure_AreQuantized()
    {
        var builder = Start(Pen(1.234f, 5.678f, 1000.123456, 0.12345f), origin: 1000);

        Assert.Equal(new SignaturePoint(1.23f, 5.68f, 0.12, 0.123f), builder.Points[0]);
    }

    [Fact]
    public void Touch_DropsPressure()
    {
        var builder = Start(new InkSample(0, 0, 0, 0.7f, SignatureInputDevice.Touch));

        Assert.Null(builder.Points[0].Pressure);
    }

    [Theory]
    [InlineData(1.4f, 1f)]
    [InlineData(-0.2f, 0f)]
    public void Pen_ClampsPressure(float raw, float stored) =>
        Assert.Equal(stored, Start(Pen(0, 0, 0, raw)).Points[0].Pressure);

    [Fact]
    public void Device_IsTakenFromTheFirstSample()
    {
        var builder = Start(Pen(0, 0, 0, 0.5f));

        builder.Add(new InkSample(5, 0, 4, 0.5f, SignatureInputDevice.Mouse));

        Assert.Equal(SignatureInputDevice.Pen, builder.Device);
        Assert.Equal(0.5f, builder.Points[1].Pressure);
    }

    [Fact]
    public void Width_AtRestStart_IsMid() =>
        Assert.Equal(Radius(0.5f), Start(Touch(0, 0, 0)).RenderPoints[0].Radius, Tolerance);

    [Fact]
    public void Width_FollowsTheFilteredVelocityFormula()
    {
        // vf₁ = 0.7·(5 DIP / 10 ms) + 0.3·V0 = 0.65; kv = 1 / (1 + 0.65) .
        var builder = Start(Touch(0, 0, 0));

        builder.Add(Touch(3, 4, 10));

        Assert.Equal(Radius(1f / 1.65f), builder.RenderPoints[1].Radius, Tolerance);
    }

    [Fact]
    public void Width_FastMotion_ApproachesMin()
    {
        var builder = Start(Touch(0, 0, 0));
        for (var i = 1; i <= 20; i++)
            builder.Add(Touch(i * 50, 0, i));

        Assert.True(builder.RenderPoints[^1].Radius < 0.55f);
    }

    [Fact]
    public void Width_SlowMotion_ApproachesMax()
    {
        var builder = Start(Touch(0, 0, 0));
        for (var i = 1; i <= 30; i++)
            builder.Add(Touch(i * 0.3f, 0, i * 10));

        Assert.True(builder.RenderPoints[^1].Radius > 1.7f);
    }

    [Fact]
    public void Width_PenFullPressure_IsNearMaxAtRest_AndReachesMaxWhenSlow()
    {
        var builder = Start(Pen(0, 0, 0, 1f));
        Assert.Equal(Radius(0.85f), builder.RenderPoints[0].Radius, Tolerance);

        for (var i = 1; i <= 30; i++)
            builder.Add(Pen(i * 0.3f, 0, i * 10, 1f));

        Assert.True(builder.RenderPoints[^1].Radius > 1.73f);
    }

    [Fact]
    public void Width_PenZeroPressure_IsMin() =>
        Assert.Equal(Min / 2f, Start(Pen(0, 0, 0, 0f)).RenderPoints[0].Radius, Tolerance);

    [Fact]
    public void EqualTimestamps_UseMinDeltaTime()
    {
        // dt is raised to 1 ms: vf = 0.7·5 + 0.3·1 = 3.8.
        var builder = Start(Touch(0, 0, 0));

        builder.Add(Touch(5, 0, 0));

        Assert.Equal(Radius(1f / 4.8f), builder.RenderPoints[1].Radius, Tolerance);
    }

    [Fact]
    public void BackwardsTimestamps_StayFinite()
    {
        var builder = Start(Touch(0, 0, 100));
        builder.Add(Touch(5, 0, 50));
        builder.Add(Touch(10, 0, 0));

        foreach (var point in builder.RenderPoints)
            Assert.True(float.IsFinite(point.Radius) && point.Radius > 0);
    }

    [Fact]
    public void Streamline_LagsThenSnapsToTheEnd()
    {
        var builder = Start(Touch(0, 0, 0));

        builder.Add(Touch(10, 0, 10));
        Assert.Equal(5.75f, builder.RenderPoints[1].X, Tolerance);

        builder.Complete();
        Assert.Equal(10f, builder.RenderPoints[1].X, Tolerance);
        Assert.True(builder.IsCompleted);
    }

    [Fact]
    public void Complete_Twice_IsHarmless()
    {
        var builder = Start(Touch(0, 0, 0));
        builder.Add(Touch(10, 0, 10));

        builder.Complete();
        builder.Complete();

        Assert.Equal(10f, builder.RenderPoints[1].X, Tolerance);
    }

    [Fact]
    public void Add_AfterComplete_Throws()
    {
        var builder = Start(Touch(0, 0, 0));
        builder.Complete();

        Assert.Throws<InvalidOperationException>(() => builder.Add(Touch(10, 0, 10)));
    }

    [Fact]
    public void ToStroke_CarriesPointsStyleAndDevice()
    {
        var builder = Start(Pen(0, 0, 0, 0.5f));
        builder.Add(Pen(10, 0, 10, 0.6f), isLast: true);

        var stroke = builder.ToStroke(Colors.Red);

        Assert.Equal([new(0, 0, 0, 0.5f), new(10, 0, 10, 0.6f)], stroke.Points);
        Assert.Equal(Colors.Red, stroke.Color);
        Assert.Equal(Min, stroke.MinWidth);
        Assert.Equal(Max, stroke.MaxWidth);
        Assert.Equal(SignatureInputDevice.Pen, stroke.Device);
    }

    [Fact]
    public void ReplayFromStroke_MatchesLiveRenderPoints_Exactly()
    {
        var random = new Random(7);
        var x = 100f;
        var y = 100f;
        var t = 5000.0;
        var builder = Start(Pen(x, y, t, 0.4f), origin: t);
        for (var i = 0; i < 300; i++)
        {
            x += (float)((random.NextDouble() - 0.5) * 12);
            y += (float)((random.NextDouble() - 0.5) * 12);
            t += random.NextDouble() * 9;
            builder.Add(Pen(x, y, t, (float)random.NextDouble()), isLast: i == 299);
        }

        var replayed = StrokeBuilder.ComputeRenderPoints(builder.ToStroke(Colors.Black));

        Assert.Equal(builder.RenderPoints.ToArray(), replayed);
    }

    [Fact]
    public void ReplayOfACompletedStroke_SnapsTheEnd_LikeLiveCompletion()
    {
        var builder = Start(Touch(0, 0, 0));
        builder.Add(Touch(10, 0, 10));
        builder.Complete();

        var replayed = StrokeBuilder.ComputeRenderPoints(builder.ToStroke(Colors.Black));

        Assert.Equal(builder.RenderPoints.ToArray(), replayed);
    }

    [Fact]
    public void TenThousandSamples_AllocateLittle()
    {
        var builder = Start(Touch(0, 0, 0));
        for (var i = 1; i <= 100; i++)
            builder.Add(Touch(i, 0, i));

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 101; i <= 10_100; i++)
            builder.Add(Touch(i, i % 7, i));
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(10_101, builder.Count);
        Assert.True(allocated < 2_000_000, $"Allocated {allocated} bytes.");
    }
}
