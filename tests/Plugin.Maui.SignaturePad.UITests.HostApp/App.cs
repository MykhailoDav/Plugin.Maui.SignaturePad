namespace Plugin.Maui.SignaturePad.UITests.HostApp;

public class App : Application
{
    // The same window size on every run on the desktop, so pads are always laid out the same.
    private const double DesktopWindowWidth = 480;
    private const double DesktopWindowHeight = 900;

    public App()
    {
        // Screenshots compare against light-theme baselines.
        UserAppTheme = AppTheme.Light;
    }

    /// <summary>Shows <paramref name="page"/> in the app's window, without a navigation animation.</summary>
    public static void Show(Page page) => Current!.Windows[0].Page = page;

    protected override Window CreateWindow(IActivationState? activationState) => new(new GalleryPage())
    {
        Title = "SignaturePad UI Tests",
        Width = DesktopWindowWidth,
        Height = DesktopWindowHeight,
    };
}
