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

    // Win2D draws on the GPU and its control has no partial invalidation, so the whole pad is redrawn; invalidations
    // are coalesced to one draw per frame.
    partial void InvalidatePlatformView() => PlatformView.Invalidate();
}
