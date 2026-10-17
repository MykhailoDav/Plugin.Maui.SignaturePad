using Plugin.Maui.SignaturePad.Ink;

namespace Plugin.Maui.SignaturePad.Rendering;

/// <summary>
/// Draws ink on any <see cref="ICanvas"/>: the pad on screen and every image export use this same code, so an exported
/// signature looks exactly like the one on the pad. Each stroke is a single filled outline.
/// </summary>
internal static class InkRenderer
{
    /// <summary>Fills the outline of every stroke, in drawing order.</summary>
    /// <param name="canvas">The canvas to draw on.</param>
    /// <param name="strokes">The strokes.</param>
    /// <param name="strokeColorOverride">A color for all strokes instead of their own, or <see langword="null"/>.</param>
    /// <param name="cache">Paths to reuse between frames, or <see langword="null"/> to build each path once and release it.</param>
    public static void DrawStrokes(ICanvas canvas, IReadOnlyList<SignatureStroke> strokes, Color? strokeColorOverride, StrokePathCache? cache)
    {
        canvas.Antialias = true;
        for (var i = 0; i < strokes.Count; i++)
        {
            var stroke = strokes[i];
            canvas.FillColor = strokeColorOverride ?? stroke.Color;
            if (cache is not null)
            {
                canvas.FillPath(cache.Get(stroke), WindingMode.NonZero);
            }
            else
            {
                using var path = StrokeGeometry.ToPath(stroke.Outline);
                canvas.FillPath(path, WindingMode.NonZero);
            }
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
