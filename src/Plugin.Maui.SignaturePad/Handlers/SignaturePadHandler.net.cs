using Microsoft.Maui.Handlers;

namespace Plugin.Maui.SignaturePad;

public partial class SignaturePadHandler : ViewHandler<SignaturePadView, object>
{
    /// <inheritdoc/>
    protected override object CreatePlatformView() =>
        throw new PlatformNotSupportedException("The signature pad needs a platform: Android, iOS, Mac Catalyst or Windows.");
}
