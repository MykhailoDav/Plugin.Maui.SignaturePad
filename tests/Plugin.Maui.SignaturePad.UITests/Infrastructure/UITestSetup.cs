using NUnit.Framework;
using Plugin.Maui.SignaturePad.UITests.Infrastructure;

// In the root namespace, so it applies to the tests of every namespace below it.
namespace Plugin.Maui.SignaturePad.UITests;

/// <summary>Starts Appium and the app once for the whole run; every test then opens its own scenario.</summary>
[SetUpFixture]
public sealed class UITestSetup
{
    private static AppiumServer? s_server;
    private static SignaturePadApp? s_app;

    public static SignaturePadApp App => s_app ?? throw new InvalidOperationException("The app was not started.");

    [OneTimeSetUp]
    public void StartApp()
    {
        var url = UITestSettings.AppiumUrl;
        if (url is null)
        {
            s_server = AppiumServer.Start();
            url = s_server.Url;
        }

        s_app = SignaturePadApp.Launch(url);
    }

    [OneTimeTearDown]
    public void StopApp()
    {
        s_app?.Dispose();
        s_server?.Dispose();
    }
}
