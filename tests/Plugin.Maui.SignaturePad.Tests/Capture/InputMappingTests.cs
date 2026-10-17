using Plugin.Maui.SignaturePad.Capture;

namespace Plugin.Maui.SignaturePad.Tests.Capture;

public class InputMappingTests
{
    [Fact]
    public void AndroidTimestamp_PrefersNanoseconds()
    {
        Assert.Equal(1234.5678, InputMapping.AndroidTimestamp(1234, 1_234_567_800));
        Assert.Equal(1234, InputMapping.AndroidTimestamp(1234, null));
    }

    [Fact]
    public void AppleTimestamp_ConvertsSeconds() =>
        Assert.Equal(12345.678, InputMapping.AppleTimestamp(12.345678), 9);

    [Fact]
    public void WindowsTimestamp_ConvertsMicroseconds() =>
        Assert.Equal(1234.567, InputMapping.WindowsTimestamp(1_234_567));

    [Fact]
    public void Pressure_IsKeptOnlyForPens()
    {
        Assert.Equal(0.4f, InputMapping.PressureOrNaN(SignatureInputDevice.Pen, 0.4f));
        Assert.True(float.IsNaN(InputMapping.PressureOrNaN(SignatureInputDevice.Touch, 0.4f)));
        Assert.True(float.IsNaN(InputMapping.PressureOrNaN(SignatureInputDevice.Mouse, 1f)));
    }

    // MotionEvent.TOOL_TYPE_* values.
    [Theory]
    [InlineData(0, SignatureInputDevice.Unknown)]
    [InlineData(1, SignatureInputDevice.Touch)]
    [InlineData(2, SignatureInputDevice.Pen)]
    [InlineData(3, SignatureInputDevice.Mouse)]
    [InlineData(99, SignatureInputDevice.Unknown)]
    public void AndroidDevice_MapsToolTypes(int toolType, SignatureInputDevice expected) =>
        Assert.Equal(expected, InputMapping.AndroidDevice(toolType));

    // TOOL_TYPE_ERASER and TOOL_TYPE_PALM (API 33) never draw.
    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    public void AndroidDevice_EraserAndPalm_DoNotDraw(int toolType) =>
        Assert.Null(InputMapping.AndroidDevice(toolType));

    // UITouch.TouchType values.
    [Theory]
    [InlineData(0, SignatureInputDevice.Touch)]
    [InlineData(1, SignatureInputDevice.Mouse)]
    [InlineData(2, SignatureInputDevice.Pen)]
    [InlineData(3, SignatureInputDevice.Mouse)]
    [InlineData(99, SignatureInputDevice.Unknown)]
    public void AppleDevice_MapsTouchTypes(int touchType, SignatureInputDevice expected) =>
        Assert.Equal(expected, InputMapping.AppleDevice(touchType));

    // Microsoft.UI.Input.PointerDeviceType values (3 is Touchpad).
    [Theory]
    [InlineData(0, SignatureInputDevice.Touch)]
    [InlineData(1, SignatureInputDevice.Pen)]
    [InlineData(2, SignatureInputDevice.Mouse)]
    [InlineData(3, SignatureInputDevice.Mouse)]
    [InlineData(99, SignatureInputDevice.Unknown)]
    public void WindowsDevice_MapsPointerDeviceTypes(int deviceType, SignatureInputDevice expected) =>
        Assert.Equal(expected, InputMapping.WindowsDevice(deviceType));
}
