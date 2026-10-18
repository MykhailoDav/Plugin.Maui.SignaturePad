using Microsoft.Maui.Handlers;
using Plugin.Maui.SignaturePad.Rendering;

namespace Plugin.Maui.SignaturePad;

public partial class SignaturePadHandler : ViewHandler<SignaturePadView, PlatformSignaturePadView>
{
    private SignaturePadDrawable? _drawable;

    /// <inheritdoc/>
    protected override PlatformSignaturePadView CreatePlatformView() => new();

    /// <inheritdoc/>
    protected override void ConnectHandler(PlatformSignaturePadView platformView)
    {
        base.ConnectHandler(platformView);

        _drawable = new SignaturePadDrawable(VirtualView);
        platformView.Drawable = _drawable;
        platformView.Sink = VirtualView.Session;
        VirtualView.InvalidateRequested += InvalidatePlatformView;
        VirtualView.ActiveStrokeUpdated += InvalidateActiveStroke;
    }

    /// <inheritdoc/>
    protected override void DisconnectHandler(PlatformSignaturePadView platformView)
    {
        VirtualView.InvalidateRequested -= InvalidatePlatformView;
        VirtualView.ActiveStrokeUpdated -= InvalidateActiveStroke;
        platformView.Sink = null;
        platformView.Drawable = null;
        _drawable?.Dispose();
        _drawable = null;

        base.DisconnectHandler(platformView);
    }

    partial void InvalidatePlatformView() => PlatformView.InvalidateDrawable();

    // CoreGraphics draws on the CPU, so while a stroke is drawn only the area it changed is redrawn; the committed
    // paths outside it are clipped away cheaply.
    private void InvalidateActiveStroke()
    {
        if (_drawable?.TakeActiveDirtyRect() is { Width: > 0, Height: > 0 } rect)
            PlatformView.InvalidateDrawable(rect.X, rect.Y, rect.Width, rect.Height);
    }
}
