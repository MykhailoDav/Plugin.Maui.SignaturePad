namespace Plugin.Maui.SignaturePad.Tests.Handlers;

public class RegistrationTests
{
    [Fact]
    public void UseSignaturePad_RegistersTheHandler()
    {
        var app = MauiApp.CreateBuilder().UseSignaturePad().Build();

        var factory = app.Services.GetRequiredService<IMauiHandlersFactory>();

        Assert.Equal(typeof(SignaturePadHandler), factory.GetHandlerType(typeof(SignaturePadView)));
    }

    [Fact]
    public void UseSignaturePad_Twice_IsHarmless()
    {
        var builder = MauiApp.CreateBuilder();

        Assert.Same(builder, builder.UseSignaturePad().UseSignaturePad());
        var factory = builder.Build().Services.GetRequiredService<IMauiHandlersFactory>();
        Assert.Equal(typeof(SignaturePadHandler), factory.GetHandlerType(typeof(SignaturePadView)));
    }

    [Fact]
    public void UseSignaturePad_NullBuilder_Throws() =>
        Assert.Throws<ArgumentNullException>(() => SignaturePadAppBuilderExtensions.UseSignaturePad(null!));

    [Theory]
    [InlineData(nameof(SignaturePadView.Placeholder))]
    [InlineData(nameof(SignaturePadView.PlaceholderColor))]
    [InlineData(nameof(SignaturePadView.ShowSignatureLine))]
    [InlineData(nameof(SignaturePadView.SignatureLineColor))]
    [InlineData(nameof(SignaturePadView.DisplayStrokeColor))]
    public void Mapper_RedrawsOnVisualProperties(string property) =>
        Assert.Contains(property, SignaturePadHandler.Mapper.GetKeys());

    [Fact]
    public void Mapper_KeepsTheViewMappings() =>
        Assert.Contains(nameof(IView.IsEnabled), SignaturePadHandler.Mapper.GetKeys());

    [Fact]
    public void Handler_CanBeCreatedWithCustomMappers()
    {
        var mapper = new PropertyMapper<SignaturePadView, SignaturePadHandler>(SignaturePadHandler.Mapper);

        var commandMapper = new CommandMapper<SignaturePadView, SignaturePadHandler>(SignaturePadHandler.CommandMapper);

        Assert.NotNull(new SignaturePadHandler(mapper));
        Assert.NotNull(new SignaturePadHandler(mapper, commandMapper));
        Assert.NotNull(new SignaturePadHandler(null, null));
        Assert.NotNull(new SignaturePadHandler());
    }

    [Fact]
    public void MapInvalidate_WithoutAPlatformView_DoesNothing()
    {
        SignaturePadHandler.MapInvalidate(new SignaturePadHandler(), new SignaturePadView());

        Assert.Throws<ArgumentNullException>(() => SignaturePadHandler.MapInvalidate(null!, new SignaturePadView()));
    }

    [Fact]
    public void Handler_WithoutAPlatform_CannotCreateItsView()
    {
        var view = new SignaturePadView();
        var handler = new SignaturePadHandler();
        handler.SetMauiContext(new MauiContext(new ServiceCollection().BuildServiceProvider()));

        Assert.Throws<PlatformNotSupportedException>(() => handler.SetVirtualView(view));
    }
}
