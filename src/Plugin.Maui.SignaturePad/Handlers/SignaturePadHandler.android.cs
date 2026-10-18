using Microsoft.Maui.Handlers;
using Plugin.Maui.SignaturePad.Rendering;

namespace Plugin.Maui.SignaturePad;

public partial class SignaturePadHandler : ViewHandler<SignaturePadView, PlatformSignaturePadView>
{
    private SignaturePadDrawable? _drawable;

    /// <inheritdoc/>
    protected override PlatformSignaturePadView CreatePlatformView() => new(Context);

    /// <inheritdoc/>
    protected override void ConnectHandler(PlatformSignaturePadView platformView)
    {
        base.ConnectHandler(platformView);

        _drawable = new SignaturePadDrawable(VirtualView, new CommittedInkLayer(platformView.PixelSize));
        platformView.Drawable = _drawable;
        platformView.Sink = VirtualView.Session;
        VirtualView.InvalidateRequested += InvalidatePlatformView;
        VirtualView.ActiveStrokeUpdated += InvalidatePlatformView;
    }

    /// <inheritdoc/>
    protected override void DisconnectHandler(PlatformSignaturePadView platformView)
    {
        VirtualView.InvalidateRequested -= InvalidatePlatformView;
        VirtualView.ActiveStrokeUpdated -= InvalidatePlatformView;
        platformView.Sink = null;
        platformView.Drawable = null;
        _drawable?.Dispose();
        _drawable = null;

        base.DisconnectHandler(platformView);
    }

    // Android hardware-accelerated views redraw whole anyway (dirty rectangles are ignored), and invalidations are
    // coalesced to one draw per frame.
    partial void InvalidatePlatformView() => PlatformView.Invalidate();
}
