namespace Plugin.Maui.SignaturePad.Sample.WinUI;

/// <summary>The WinUI application that hosts the MAUI app.</summary>
public partial class App : MauiWinUIApplication
{
    public App() => InitializeComponent();

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
