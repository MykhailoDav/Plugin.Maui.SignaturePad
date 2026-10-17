using Foundation;
using Plugin.Maui.SignaturePad.Capture;
using Plugin.Maui.SignaturePad.Ink;
using UIKit;

namespace Plugin.Maui.SignaturePad;

/// <summary>
/// Reads the input of the pad on iOS and Mac Catalyst. A gesture recognizer, rather than the view's own touch methods,
/// takes part in UIKit's gesture arbitration: it begins as soon as a finger, pen or mouse touches the pad, and makes the
/// pan gestures of enclosing views (scroll views, the Shell flyout, the navigation back swipe) wait for it, so they
/// cannot steal the stroke. Touches outside the pad never reach it, so scrolling there works as usual.
/// </summary>
internal sealed class InkGestureRecognizer : UIGestureRecognizer
{
    // Weak, because the session leads back to the native view through the pad and its handler, and a gesture
    // recognizer is retained natively by its view: a strong reference would keep the whole pad alive forever.
    private WeakReference<IInkInputSink>? _sink;
    private UITouch? _touch;
    private SignatureInputDevice _device;
    private InkSample[] _samples = new InkSample[16];

    public InkGestureRecognizer()
    {
        CancelsTouchesInView = true;
        DelaysTouchesBegan = false;
        DelaysTouchesEnded = false;
    }

    public IInkInputSink? Sink
    {
        get => _sink is not null && _sink.TryGetTarget(out var sink) ? sink : null;
        set => _sink = value is null ? null : new WeakReference<IInkInputSink>(value);
    }

    public override void TouchesBegan(NSSet touches, UIEvent evt)
    {
        base.TouchesBegan(touches, evt);

        if (_touch is not null || touches.AnyObject is not UITouch touch || View is null || Sink is not { IsInputEnabled: true } sink)
        {
            IgnoreAll(touches, evt);
            return;
        }

        var device = InputMapping.AppleDevice((int)touch.Type);

        // A mouse draws with its primary button only; a secondary click is for context menus.
        if (device == SignatureInputDevice.Mouse && IsSecondaryClick(evt))
        {
            IgnoreAll(touches, evt);
            return;
        }

        _touch = touch;
        _device = device;
        foreach (var other in touches)
        {
            if (other is UITouch otherTouch && otherTouch != touch)
                IgnoreTouch(otherTouch, evt);
        }

        sink.BeginStroke(Sample(touch));
        State = UIGestureRecognizerState.Began;
    }

    public override void TouchesMoved(NSSet touches, UIEvent evt)
    {
        base.TouchesMoved(touches, evt);

        if (_touch is not { } touch || !touches.Contains(touch) || Sink is not { } sink)
            return;

        // UIKit reports about 60 (or 120) moves a second, but the screen samples up to 240 times a second for a pencil
        // and more than once a frame for a finger; the samples in between are only available as coalesced touches.
        var coalesced = evt.GetCoalescedTouches(touch);
        var count = coalesced?.Length ?? 0;
        if (count == 0)
        {
            _samples[0] = Sample(touch);
            count = 1;
        }
        else
        {
            if (_samples.Length < count)
                Array.Resize(ref _samples, Math.Max(count, _samples.Length * 2));
            for (var i = 0; i < count; i++)
                _samples[i] = Sample(coalesced![i]);
        }

        sink.ContinueStroke(_samples.AsSpan(0, count));
        State = UIGestureRecognizerState.Changed;
    }

    public override void TouchesEnded(NSSet touches, UIEvent evt)
    {
        base.TouchesEnded(touches, evt);

        if (_touch is not { } touch || !touches.Contains(touch))
            return;

        _touch = null;
        Sink?.EndStroke(Sample(touch));
        State = UIGestureRecognizerState.Ended;
    }

    public override void TouchesCancelled(NSSet touches, UIEvent evt)
    {
        base.TouchesCancelled(touches, evt);

        if (_touch is null)
            return;

        _touch = null;
        Sink?.CancelStroke();
        State = UIGestureRecognizerState.Cancelled;
    }

    public override void Reset()
    {
        base.Reset();

        // UIKit resets a recognizer after it ends or fails; a stroke still open here was interrupted.
        if (_touch is not null)
        {
            _touch = null;
            Sink?.CancelStroke();
        }
    }

    public override bool ShouldBeRequiredToFailByGestureRecognizer(UIGestureRecognizer otherGestureRecognizer) =>
        otherGestureRecognizer is UIPanGestureRecognizer
        && otherGestureRecognizer.View is { } otherView
        && View is { } view
        && view.IsDescendantOfView(otherView);

    private InkSample Sample(UITouch touch)
    {
        var location = touch.GetPreciseLocation(View);
        var pressure = touch.MaximumPossibleForce > 0 ? (float)(touch.Force / touch.MaximumPossibleForce) : float.NaN;
        return new InkSample(
            (float)location.X,
            (float)location.Y,
            InputMapping.AppleTimestamp(touch.Timestamp),
            InputMapping.PressureOrNaN(_device, pressure),
            _device);
    }

    private static bool IsSecondaryClick(UIEvent evt) =>
        (OperatingSystem.IsIOSVersionAtLeast(13, 4) || OperatingSystem.IsMacCatalystVersionAtLeast(13, 4))
        && (evt.ButtonMask & UIEventButtonMask.Primary) == 0;

    private void IgnoreAll(NSSet touches, UIEvent evt)
    {
        foreach (var item in touches)
        {
            if (item is UITouch touch)
                IgnoreTouch(touch, evt);
        }

        if (_touch is null)
            State = UIGestureRecognizerState.Failed;
    }
}
