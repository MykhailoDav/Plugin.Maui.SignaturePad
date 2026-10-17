using System.Diagnostics.CodeAnalysis;
using Microsoft.Maui.Graphics.Platform;
using Plugin.Maui.SignaturePad.Capture;
using UIKit;

namespace Plugin.Maui.SignaturePad;

/// <summary>
/// The iOS and Mac Catalyst view of the signature pad. It draws through Microsoft.Maui.Graphics (CoreGraphics) and
/// reads finger, Apple Pencil and mouse input with a gesture recognizer, including coalesced touches and pencil force.
/// </summary>
[SuppressMessage("Design", "CA1010", Justification = "UIView enumerates its subviews; the pad adds nothing to that.")]
public sealed class PlatformSignaturePadView : PlatformGraphicsView
{
    private readonly InkGestureRecognizer _recognizer = new();

    /// <summary>Creates the view.</summary>
    public PlatformSignaturePadView()
    {
        Opaque = false;
        BackgroundColor = UIColor.Clear;
        MultipleTouchEnabled = false;
        AddGestureRecognizer(_recognizer);
    }

    /// <summary>Where input goes; <see langword="null"/> while the handler is disconnected.</summary>
    internal IInkInputSink? Sink
    {
        get => _recognizer.Sink;
        set => _recognizer.Sink = value;
    }
}
