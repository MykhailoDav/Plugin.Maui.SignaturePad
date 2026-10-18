using Microsoft.Maui.Graphics.Platform;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Plugin.Maui.SignaturePad.Capture;
using Plugin.Maui.SignaturePad.Ink;

namespace Plugin.Maui.SignaturePad;

/// <summary>
/// The Windows view of the signature pad. It draws through Microsoft.Maui.Graphics (Win2D) and reads touch, pen and
/// mouse input from pointer events, including the intermediate points between events and pen pressure.
/// </summary>
public sealed partial class PlatformSignaturePadView : PlatformGraphicsView
{
    private readonly PointerTracker _pointer = new();
    private InkSample[] _samples = new InkSample[16];
    private SignatureInputDevice _device;

    /// <summary>Creates the view.</summary>
    public PlatformSignaturePadView()
    {
        // Touch in a ScrollViewer becomes direct manipulation, which takes the pointer away from the view; any mode
        // other than System keeps it here. The manipulation events themselves are not used.
        ManipulationMode = ManipulationModes.All;
    }

    /// <summary>Where input goes; <see langword="null"/> while the handler is disconnected.</summary>
    internal IInkInputSink? Sink { get; set; }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (Sink is not { IsInputEnabled: true } sink)
            return;

        var point = e.GetCurrentPoint(this);
        var properties = point.Properties;

        // Touch, a pen tip and the left mouse button report a left-button press; the eraser end, the barrel button and
        // the other mouse buttons do not draw.
        if (!properties.IsLeftButtonPressed || properties.IsEraser || properties.IsBarrelButtonPressed)
            return;
        if (!_pointer.TryBegin(e.Pointer.PointerId))
            return;

        _device = InputMapping.WindowsDevice((int)point.PointerDeviceType);

        // With the pointer captured, a mouse released outside the pad still ends the stroke.
        CapturePointer(e.Pointer);
        e.Handled = true;
        sink.BeginStroke(Sample(point));
    }

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerRoutedEventArgs e)
    {
        base.OnPointerMoved(e);

        if (Sink is not { } sink || !_pointer.IsTracked(e.Pointer.PointerId))
            return;

        // The points between this event and the last one, which a pen reports far more often than events arrive. The
        // documentation is not consistent about their order, so it is taken from their timestamps.
        var points = e.GetIntermediatePoints(this);
        var count = points.Count;
        if (count == 0)
            return;
        if (_samples.Length < count)
            Array.Resize(ref _samples, Math.Max(count, _samples.Length * 2));

        var newestFirst = count > 1 && points[0].Timestamp > points[count - 1].Timestamp;
        for (var i = 0; i < count; i++)
            _samples[i] = Sample(points[newestFirst ? count - 1 - i : i]);

        e.Handled = true;
        sink.ContinueStroke(_samples.AsSpan(0, count));
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (!_pointer.End(e.Pointer.PointerId))
            return;

        e.Handled = true;
        ReleasePointerCapture(e.Pointer);
        Sink?.EndStroke(Sample(e.GetCurrentPoint(this)));
    }

    /// <inheritdoc/>
    protected override void OnPointerCanceled(PointerRoutedEventArgs e)
    {
        base.OnPointerCanceled(e);
        Cancel(e.Pointer.PointerId);
    }

    /// <inheritdoc/>
    protected override void OnPointerCaptureLost(PointerRoutedEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        Cancel(e.Pointer.PointerId);
    }

    private void Cancel(uint pointerId)
    {
        if (_pointer.End(pointerId))
            Sink?.CancelStroke();
    }

    private InkSample Sample(PointerPoint point) => new(
        (float)point.Position.X,
        (float)point.Position.Y,
        InputMapping.WindowsTimestamp(point.Timestamp),
        InputMapping.PressureOrNaN(_device, point.Properties.Pressure),
        _device);
}
