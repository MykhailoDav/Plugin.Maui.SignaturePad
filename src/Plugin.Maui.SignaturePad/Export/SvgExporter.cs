using System.Globalization;
using System.Text;
using Plugin.Maui.SignaturePad.Ink;

namespace Plugin.Maui.SignaturePad;

/// <summary>
/// Writes a signature as SVG: one filled <c>&lt;path&gt;</c> per stroke, with exactly the outline drawn on screen, so the
/// SVG scales to any size without losing the shape of the ink. The output is deterministic.
/// </summary>
internal static class SvgExporter
{
    public static string Export(SignatureData data, SignatureSvgExportOptions options)
    {
        var source = ExportLayout.SourceRect(data, options.CropToContent, options.Padding);

        var svg = new StringBuilder(256 + (data.PointCount * 40));
        svg.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"");
        StrokeGeometry.AppendNumber(svg, source.Width);
        svg.Append("\" height=\"");
        StrokeGeometry.AppendNumber(svg, source.Height);
        svg.Append("\" viewBox=\"0 0 ");
        StrokeGeometry.AppendNumber(svg, source.Width);
        svg.Append(' ');
        StrokeGeometry.AppendNumber(svg, source.Height);
        svg.Append("\">");

        if (options.BackgroundColor is { } background)
        {
            svg.Append("<rect width=\"");
            StrokeGeometry.AppendNumber(svg, source.Width);
            svg.Append("\" height=\"");
            StrokeGeometry.AppendNumber(svg, source.Height);
            svg.Append('"');
            AppendFill(svg, background);
            svg.Append("/>");
        }

        for (var i = 0; i < data.Strokes.Count; i++)
        {
            var stroke = data.Strokes[i];
            svg.Append("<path d=\"").Append(StrokeGeometry.ToSvgPathData(stroke.Outline, source.Location)).Append('"');
            AppendFill(svg, options.StrokeColor ?? stroke.Color);
            svg.Append("/>");
        }

        return svg.Append("</svg>").ToString();
    }

    private static void AppendFill(StringBuilder svg, Color color)
    {
        var rgba = ColorBytes.ToRgba(color);
        svg.Append(" fill=\"#").Append((rgba >> 8).ToString("X6", CultureInfo.InvariantCulture)).Append('"');

        // Opaque colors need no opacity attribute, which keeps the SVG small.
        var alpha = rgba & 0xFF;
        if (alpha < 0xFF)
        {
            svg.Append(" fill-opacity=\"");
            StrokeGeometry.AppendNumber(svg, alpha / 255f);
            svg.Append('"');
        }
    }
}
