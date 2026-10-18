using System.Globalization;
using System.Text;

namespace Plugin.Maui.SignaturePad.Ink;

/// <summary>
/// Turns a stroke outline into a drawable path and into SVG path data. Both describe exactly the same curve, so a
/// signature exported as SVG looks like the one on screen.
/// </summary>
/// <remarks>
/// The curve runs through the midpoints of the outline's edges, with each outline point as the control point of a
/// quadratic Bézier between them. That smooths the polygon into a round shape without adding points.
/// </remarks>
internal static class StrokeGeometry
{
    public static PathF ToPath(ReadOnlySpan<PointF> outline) => ToPath(outline, default, 1f);

    /// <summary>
    /// Builds the path of the outline moved by <c>-offset</c> and then scaled. Platform canvases do not apply their own
    /// transforms to paths alike (Android's ignores its scale for paths), so drawing into a bitmap transforms the
    /// geometry itself.
    /// </summary>
    public static PathF ToPath(ReadOnlySpan<PointF> outline, PointF offset, float scale)
    {
        var path = new PathF();
        if (outline.Length == 0)
            return path;

        var count = outline.Length;
        path.MoveTo(Transform(Mid(outline[0], outline[1 % count]), offset, scale));
        for (var i = 1; i <= count; i++)
        {
            var control = outline[i % count];
            path.QuadTo(Transform(control, offset, scale), Transform(Mid(control, outline[(i + 1) % count]), offset, scale));
        }

        path.Close();
        return path;
    }

    /// <summary>Writes the SVG path data of the outline, moved by <c>-offset</c>.</summary>
    public static string ToSvgPathData(ReadOnlySpan<PointF> outline, PointF offset = default)
    {
        if (outline.Length == 0)
            return "";

        var count = outline.Length;
        var data = new StringBuilder(count * 26);
        data.Append('M');
        AppendPoint(data, Mid(outline[0], outline[1 % count]), offset);
        for (var i = 1; i <= count; i++)
        {
            var control = outline[i % count];
            data.Append(" Q");
            AppendPoint(data, control, offset);
            data.Append(' ');
            AppendPoint(data, Mid(control, outline[(i + 1) % count]), offset);
        }

        return data.Append(" Z").ToString();
    }

    public static RectF Bounds(ReadOnlySpan<PointF> outline)
    {
        if (outline.Length == 0)
            return RectF.Zero;

        float left = float.MaxValue, top = float.MaxValue, right = float.MinValue, bottom = float.MinValue;
        foreach (var point in outline)
        {
            left = MathF.Min(left, point.X);
            top = MathF.Min(top, point.Y);
            right = MathF.Max(right, point.X);
            bottom = MathF.Max(bottom, point.Y);
        }

        return new RectF(left, top, right - left, bottom - top);
    }

    /// <summary>Writes a number with at most two decimals, in invariant culture, and never as <c>-0</c>.</summary>
    internal static void AppendNumber(StringBuilder text, float value)
    {
        var rounded = MathF.Round(value, 2);
        if (rounded == 0)
            rounded = 0;
        text.Append(rounded.ToString("0.##", CultureInfo.InvariantCulture));
    }

    private static void AppendPoint(StringBuilder text, PointF point, PointF offset)
    {
        AppendNumber(text, point.X - offset.X);
        text.Append(' ');
        AppendNumber(text, point.Y - offset.Y);
    }

    private static PointF Mid(PointF a, PointF b) => new((a.X + b.X) / 2f, (a.Y + b.Y) / 2f);

    private static PointF Transform(PointF point, PointF offset, float scale) =>
        new((point.X - offset.X) * scale, (point.Y - offset.Y) * scale);
}
