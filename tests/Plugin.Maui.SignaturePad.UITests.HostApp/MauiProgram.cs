namespace Plugin.Maui.SignaturePad.UITests.HostApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseSignaturePad();
        return builder.Build();
    }
}
