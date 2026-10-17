using Plugin.Maui.SignaturePad.Ink;

namespace Plugin.Maui.SignaturePad.Tests.Ink;

public class QuantizeTests
{
    [Theory]
    [InlineData(1.234f, 1.23f)]
    [InlineData(1.236f, 1.24f)]
    [InlineData(-7.891f, -7.89f)]
    [InlineData(250f, 250f)]
    public void Coordinate_RoundsToHundredths(float value, float expected) =>
        Assert.Equal(expected, Quantize.Coordinate(value));

    [Theory]
    [InlineData(4.16666, 4.17)]
    [InlineData(1000.004, 1000)]
    public void Time_RoundsToHundredthsOfAMillisecond(double value, double expected) =>
        Assert.Equal(expected, Quantize.Time(value));

    [Theory]
    [InlineData(0.12345f, 0.123f)]
    [InlineData(0.9996f, 1f)]
    public void Pressure_RoundsToThousandths(float value, float expected) =>
        Assert.Equal(expected, Quantize.Pressure(value));
}
