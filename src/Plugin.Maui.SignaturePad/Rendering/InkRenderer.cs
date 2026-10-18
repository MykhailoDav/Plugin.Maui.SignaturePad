using Plugin.Maui.SignaturePad.Ink;

namespace Plugin.Maui.SignaturePad.Rendering;

/// <summary>
/// Draws ink on any <see cref="ICanvas"/>: the pad on screen and every image export use this same code, so an exported
/// signature looks exactly like the one on the pad. Each stroke is a single filled outline.
/// </summary>
internal static class InkRenderer
{
    /// <summary>Fills the outline of every stroke, in drawing order, with paths from <paramref name="cache"/>.</summary>
    /// <param name="canvas">The canvas to draw on.</param>
    /// <param name="strokes">The strokes.</param>
    /// <param name="cache">The paths to reuse between frames.</param>
    public static void DrawStrokes(ICanvas canvas, IReadOnlyList<SignatureStroke> strokes, StrokePathCache cache)
    {
        canvas.Antialias = true;
        for (var i = 0; i < strokes.Count; i++)
        {
            var stroke = strokes[i];
            canvas.FillColor = stroke.Color;
            canvas.FillPath(cache.Get(stroke), WindingMode.NonZero);
        }
    }

    /// <summary>
    /// Fills the outline of every stroke, moved by <c>-offset</c> and scaled to pixels, building each path once. For
    /// drawing into bitmaps, where a canvas transform cannot be relied on.
    /// </summary>
    /// <param name="canvas">The canvas to draw on, in pixels.</param>
    /// <param name="strokes">The strokes.</param>
    /// <param name="strokeColorOverride">A color for all strokes instead of their own, or <see langword="null"/>.</param>
    /// <param name="offset">The point of the pad that becomes the origin.</param>
    /// <param name="scale">The pixels per DIP.</param>
    public static void DrawStrokes(ICanvas canvas, IReadOnlyList<SignatureStroke> strokes, Color? strokeColorOverride, PointF offset, float scale)
    {
        canvas.Antialias = true;
        for (var i = 0; i < strokes.Count; i++)
        {
            var stroke = strokes[i];
            canvas.FillColor = strokeColorOverride ?? stroke.Color;
            using var path = StrokeGeometry.ToPath(stroke.Outline, offset, scale);
            canvas.FillPath(path, WindingMode.NonZero);
        }
    }

    /// <summary>Fills the outline of the stroke being drawn, built from its current points.</summary>
    public static void DrawActive(ICanvas canvas, StrokeBuilder active, Color color)
    {
        using var path = StrokeGeometry.ToPath(StrokeOutline.Build(active.RenderPoints));
        canvas.Antialias = true;
        canvas.FillColor = color;
        canvas.FillPath(path, WindingMode.NonZero);
    }
}
