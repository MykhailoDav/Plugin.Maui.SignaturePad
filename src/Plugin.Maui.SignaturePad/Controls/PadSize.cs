using Microsoft.Maui.Primitives;

namespace Plugin.Maui.SignaturePad;

/// <summary>
/// The size a pad asks for. It has no content to measure, so it fills the space its layout gives it. Where a layout
/// leaves a length open, as a vertical stack, a scroll view or an Auto row leaves the height, it takes its natural size,
/// rather than none at all and disappearing.
/// </summary>
internal static class PadSize
{
    /// <summary>The width, in DIP, where a layout leaves the width open.</summary>
    public const double NaturalWidth = 300;

    /// <summary>The height, in DIP, where a layout leaves the height open.</summary>
    public const double NaturalHeight = 200;

    /// <summary>The size of <paramref name="view"/> within the given space, both without the view's margin.</summary>
    public static Size Desired(IView view, double widthConstraint, double heightConstraint) => new(
        Length(widthConstraint, view.Width, view.MinimumWidth, view.MaximumWidth, NaturalWidth),
        Length(heightConstraint, view.Height, view.MinimumHeight, view.MaximumHeight, NaturalHeight));

    // As MAUI resolves the size of a view: a requested length wins, the minimum and maximum bound it, and the result
    // never exceeds the space.
    private static double Length(double constraint, double requested, double minimum, double maximum, double natural)
    {
        var length = Dimension.IsExplicitSet(requested) ? requested
            : double.IsPositiveInfinity(constraint) ? natural
            : constraint;
        length = Math.Min(length, maximum);
        if (Dimension.IsMinimumSet(minimum))
            length = Math.Max(length, minimum);

        return Math.Max(0, Math.Min(length, constraint));
    }
}
