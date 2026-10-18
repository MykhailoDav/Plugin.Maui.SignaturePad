using Plugin.Maui.SignaturePad.Sample.Pages;
using Plugin.Maui.SignaturePad.Sample.ViewModels;

namespace Plugin.Maui.SignaturePad.Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseSignaturePad();

        // The view models use the platform services through their interfaces, so they do not depend on a device.
        builder.Services.AddSingleton(Clipboard.Default);
        builder.Services.AddSingleton(FileSystem.Current);
        builder.Services.AddSingleton(SemanticScreenReader.Default);
        builder.Services.AddSingleton(Share.Default);

        builder.Services.AddTransient<SignPage>().AddTransient<SignViewModel>();
        builder.Services.AddTransient<InkPage>().AddTransient<InkViewModel>();
        builder.Services.AddTransient<ExportPage>().AddTransient<ExportViewModel>();
        builder.Services.AddTransient<DataPage>().AddTransient<DataViewModel>();
        builder.Services.AddTransient<FormPage>().AddTransient<FormViewModel>();

        return builder.Build();
    }
}
