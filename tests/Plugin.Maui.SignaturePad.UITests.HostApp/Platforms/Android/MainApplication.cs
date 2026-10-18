using Android.App;
using Android.Runtime;

namespace Plugin.Maui.SignaturePad.UITests.HostApp;

[Application]
public class MainApplication(IntPtr handle, JniHandleOwnership ownership) : MauiApplication(handle, ownership)
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
