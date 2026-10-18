using System.Diagnostics.CodeAnalysis;

namespace Plugin.Maui.SignaturePad;

/// <summary>
/// The handler of <see cref="SignaturePadView"/>. The native view draws through Microsoft.Maui.Graphics and reads input
/// natively on each platform, with every sample the platform has (stylus history, coalesced touches, intermediate pen
/// points), real timestamps and pen pressure.
/// </summary>
/// <remarks>Customize it like any MAUI handler, for example with <c>SignaturePadHandler.Mapper.AppendToMapping</c>.</remarks>
[SuppressMessage("Design", "CA1001", Justification = "Handlers release what they own in DisconnectHandler, as every MAUI handler does.")]
public partial class SignaturePadHandler
{
    /// <summary>The property mapper. The visual properties of the pad redraw it.</summary>
    public static readonly IPropertyMapper<SignaturePadView, SignaturePadHandler> Mapper =
        new PropertyMapper<SignaturePadView, SignaturePadHandler>(ViewMapper)
        {
            [nameof(SignaturePadView.Placeholder)] = MapInvalidate,
            [nameof(SignaturePadView.PlaceholderColor)] = MapInvalidate,
            [nameof(SignaturePadView.ShowSignatureLine)] = MapInvalidate,
            [nameof(SignaturePadView.SignatureLineColor)] = MapInvalidate,
            [nameof(SignaturePadView.DisplayStrokeColor)] = MapInvalidate,
        };

    /// <summary>The command mapper.</summary>
    public static readonly CommandMapper<SignaturePadView, SignaturePadHandler> CommandMapper = new(ViewCommandMapper);

    /// <summary>Creates the handler with the default mappers.</summary>
    public SignaturePadHandler()
        : base(Mapper, CommandMapper)
    {
    }

    /// <summary>Creates the handler with custom mappers.</summary>
    /// <param name="mapper">The property mapper, or <see langword="null"/> for <see cref="Mapper"/>.</param>
    /// <param name="commandMapper">The command mapper, or <see langword="null"/> for <see cref="CommandMapper"/>.</param>
    public SignaturePadHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper)
    {
    }

    /// <summary>Redraws the pad.</summary>
    /// <param name="handler">The handler.</param>
    /// <param name="view">The pad.</param>
    public static void MapInvalidate(SignaturePadHandler handler, SignaturePadView view)
    {
        ArgumentNullException.ThrowIfNull(handler);
        handler.InvalidatePlatformView();
    }

    partial void InvalidatePlatformView();
}
