namespace Plugin.Maui.SignaturePad.Capture;

/// <summary>
/// Converts native input values into the shared model. The platform enums are passed as their numeric values, which
/// the platforms define as stable constants, so the mapping can live (and be tested) in shared code.
/// </summary>
internal static class InputMapping
{
    /// <summary>Android event time in milliseconds, using the nanosecond time when the OS provides it (API 34+).</summary>
    public static double AndroidTimestamp(long eventTimeMs, long? eventTimeNanos) =>
        eventTimeNanos is { } nanos ? nanos / 1_000_000.0 : eventTimeMs;

    /// <summary><c>UITouch.Timestamp</c> (seconds since boot) in milliseconds.</summary>
    public static double AppleTimestamp(double seconds) => seconds * 1000.0;

    /// <summary><c>PointerPoint.Timestamp</c> (microseconds) in milliseconds.</summary>
    public static double WindowsTimestamp(ulong microseconds) => microseconds / 1000.0;

    /// <summary>
    /// The pressure to record. A finger's "pressure" is really its contact area, and a mouse's is a constant, so only
    /// pens report it.
    /// </summary>
    public static float PressureOrNaN(SignatureInputDevice device, float pressure) =>
        device == SignatureInputDevice.Pen ? pressure : float.NaN;

    /// <summary>
    /// Maps <c>MotionEvent.TOOL_TYPE_*</c>. Returns <see langword="null"/> for the eraser end of a stylus and for a palm
    /// (API 33+), which must not draw.
    /// </summary>
    public static SignatureInputDevice? AndroidDevice(int toolType) => toolType switch
    {
        1 => SignatureInputDevice.Touch,
        2 => SignatureInputDevice.Pen,
        3 => SignatureInputDevice.Mouse,
        4 or 5 => null,
        _ => SignatureInputDevice.Unknown,
    };

    /// <summary>Maps <c>UITouch.TouchType</c>: direct, indirect, pencil and indirect pointer (mouse or trackpad).</summary>
    public static SignatureInputDevice AppleDevice(int touchType) => touchType switch
    {
        0 => SignatureInputDevice.Touch,
        1 or 3 => SignatureInputDevice.Mouse,
        2 => SignatureInputDevice.Pen,
        _ => SignatureInputDevice.Unknown,
    };

    /// <summary>Maps <c>Microsoft.UI.Input.PointerDeviceType</c>: touch, pen, mouse and touchpad.</summary>
    public static SignatureInputDevice WindowsDevice(int pointerDeviceType) => pointerDeviceType switch
    {
        0 => SignatureInputDevice.Touch,
        1 => SignatureInputDevice.Pen,
        2 or 3 => SignatureInputDevice.Mouse,
        _ => SignatureInputDevice.Unknown,
    };
}
