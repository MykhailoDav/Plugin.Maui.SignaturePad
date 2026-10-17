namespace Plugin.Maui.SignaturePad;

/// <summary>
/// Registers the signature pad with a MAUI app.
/// </summary>
public static class SignaturePadAppBuilderExtensions
{
    /// <summary>
    /// Registers <see cref="SignaturePadView"/> and its handler. Call it once in <c>MauiProgram.CreateMauiApp</c>;
    /// calling it again does nothing.
    /// </summary>
    /// <param name="builder">The app builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static MauiAppBuilder UseSignaturePad(this MauiAppBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureMauiHandlers(static handlers => handlers.TryAddHandler<SignaturePadView, SignaturePadHandler>());
        return builder;
    }
}
