using Android.Content;
using Android.Views;
using Microsoft.Maui.Graphics.Platform;
using Plugin.Maui.SignaturePad.Capture;
using Plugin.Maui.SignaturePad.Ink;

namespace Plugin.Maui.SignaturePad;

/// <summary>
/// The Android view of the signature pad. It draws through Microsoft.Maui.Graphics and reads touch, stylus and mouse
/// input from <see cref="MotionEvent"/>, including the historical samples Android batches into each move event.
/// </summary>
public sealed class PlatformSignaturePadView : PlatformGraphicsView
{
    private readonly PointerTracker _pointer = new();
    private InkSample[] _samples = new InkSample[16];
    private SignatureInputDevice _device;
    private float _density;

    /// <summary>Creates the view.</summary>
    /// <param name="context">The Android context.</param>
    public PlatformSignaturePadView(Context context)
        : base(context)
    {
        _density = DensityOf(context);
    }

    /// <summary>Where input goes; <see langword="null"/> while the handler is disconnected.</summary>
    internal IInkInputSink? Sink { get; set; }

    /// <inheritdoc/>
    public override bool OnTouchEvent(MotionEvent? e)
    {
        if (e is null || Sink is not { IsInputEnabled: true } sink)
            return false;

        switch (e.ActionMasked)
        {
            case MotionEventActions.Down:
                return Begin(e, sink);

            case MotionEventActions.Move:
                Continue(e, sink);
                return true;

            case MotionEventActions.Up:
            case MotionEventActions.PointerUp:
                End(e, sink);
                return true;

            case MotionEventActions.Cancel:
                _pointer.Reset();
                Parent?.RequestDisallowInterceptTouchEvent(false);
                sink.CancelStroke();
                return true;

            default:
                // Other fingers going down are ignored while one is drawing.
                return true;
        }
    }

    /// <inheritdoc/>
    protected override void OnSizeChanged(int width, int height, int oldWidth, int oldHeight)
    {
        base.OnSizeChanged(width, height, oldWidth, oldHeight);
        _density = DensityOf(Context);
    }

    private bool Begin(MotionEvent e, IInkInputSink sink)
    {
        var index = e.ActionIndex;
        if (InputMapping.AndroidDevice((int)e.GetToolType(index)) is not { } device)
            return false;

        // A mouse draws with its primary button only; the other buttons are for menus and the like.
        if (device == SignatureInputDevice.Mouse && (e.ButtonState & MotionEventButtonState.Primary) == 0)
            return false;

        // Down always starts a new gesture, so forget a pointer whose end was never delivered.
        _pointer.Reset();
        _pointer.TryBegin(e.GetPointerId(index));
        _device = device;

        // Keep a parent ScrollView (or pager, or drawer) from taking the gesture over while the user signs.
        Parent?.RequestDisallowInterceptTouchEvent(true);
        sink.BeginStroke(CurrentSample(e, index));
        return true;
    }

    private void Continue(MotionEvent e, IInkInputSink sink)
    {
        var index = PointerIndex(e);
        if (index < 0)
            return;

        // Android delivers moves at most once per frame and batches the samples in between into the event's
        // history; reading them all is what keeps fast strokes smooth.
        var history = e.HistorySize;
        if (_samples.Length < history + 1)
            Array.Resize(ref _samples, Math.Max(history + 1, _samples.Length * 2));

        var useNanos = OperatingSystem.IsAndroidVersionAtLeast(34);
        for (var h = 0; h < history; h++)
        {
            var nanos = useNanos ? e.GetHistoricalEventTimeNanos(h) : (long?)null;
            _samples[h] = new InkSample(
                e.GetHistoricalX(index, h) / _density,
                e.GetHistoricalY(index, h) / _density,
                InputMapping.AndroidTimestamp(e.GetHistoricalEventTime(h), nanos),
                InputMapping.PressureOrNaN(_device, e.GetHistoricalPressure(index, h)),
                _device);
        }

        _samples[history] = CurrentSample(e, index);
        sink.ContinueStroke(_samples.AsSpan(0, history + 1));
    }

    private void End(MotionEvent e, IInkInputSink sink)
    {
        var index = e.ActionIndex;
        if (!_pointer.End(e.GetPointerId(index)))
            return;

        Parent?.RequestDisallowInterceptTouchEvent(false);
        sink.EndStroke(CurrentSample(e, index));
    }

    private int PointerIndex(MotionEvent e)
    {
        for (var i = 0; i < e.PointerCount; i++)
        {
            if (_pointer.IsTracked(e.GetPointerId(i)))
                return i;
        }

        return -1;
    }

    private InkSample CurrentSample(MotionEvent e, int index)
    {
        var nanos = OperatingSystem.IsAndroidVersionAtLeast(34) ? e.EventTimeNanos : (long?)null;
        return new InkSample(
            e.GetX(index) / _density,
            e.GetY(index) / _density,
            InputMapping.AndroidTimestamp(e.EventTime, nanos),
            InputMapping.PressureOrNaN(_device, e.GetPressure(index)),
            _device);
    }

    private static float DensityOf(Context? context) => context?.Resources?.DisplayMetrics?.Density ?? 1f;
}
