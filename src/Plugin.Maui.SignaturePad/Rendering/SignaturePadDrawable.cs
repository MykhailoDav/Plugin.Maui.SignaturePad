using Plugin.Maui.SignaturePad.Ink;

namespace Plugin.Maui.SignaturePad.Rendering;

/// <summary>
/// Draws a pad: the signature line, the placeholder while the pad is empty, the committed strokes and the stroke being
/// drawn. Guides are drawn only here, so they never appear in an export.
/// </summary>
/// <param name="view">The pad.</param>
/// <param name="committedInk">How the committed strokes are drawn; the path cache when <see langword="null"/>.</param>
internal sealed class SignaturePadDrawable(SignaturePadView view, ICommittedInkRenderer? committedInk = null) : IDrawable, IDisposable
{
    internal const float LineInset = 16f;
    internal const float LinePosition = 0.75f;
    internal const float PlaceholderFontSize = 16f;

    private readonly ICommittedInkRenderer _committedInk = committedInk ?? new PathCacheInkRenderer();
    private StrokeBuilder? _trackedActive;
    private int _trackedActiveCount;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var session = view.Session;
        var signature = session.Current;
        var size = new SizeF((float)view.Width, (float)view.Height);

        // The guides are laid out on the whole pad; dirtyRect can be just the part being redrawn.
        DrawGuides(canvas, size.Width, size.Height, showPlaceholder: signature.IsEmpty && session.Active is null);
        var displayColor = view.DisplayStrokeColor;
        _committedInk.Draw(canvas, signature, size, displayColor);
        if (session.Active is { } active)
            InkRenderer.DrawActive(canvas, active, displayColor ?? session.ActiveColor);
    }

    /// <summary>
    /// The area that changed since the last call because the stroke being drawn got new samples, for platforms that
    /// can redraw part of the view. Besides the new samples, the end of the outline drawn before can change: the end
    /// cap moves, and the outline ignores the last few DIP of a stroke as noise until more samples arrive.
    /// </summary>
    public RectF TakeActiveDirtyRect()
    {
        var active = view.Session.Active;
        if (active is null)
        {
            _trackedActive = null;
            return RectF.Zero;
        }

        if (!ReferenceEquals(active, _trackedActive))
        {
            _trackedActive = active;
            _trackedActiveCount = 0;
        }

        var points = active.RenderPoints;
        var start = StartOfChangedTail(points, _trackedActiveCount, StrokeOutline.EndNoiseThreshold + (2 * active.MaxWidth));
        _trackedActiveCount = points.Length;

        float left = float.MaxValue, top = float.MaxValue, right = float.MinValue, bottom = float.MinValue, longestStep = 0;
        for (var i = start; i < points.Length; i++)
        {
            left = MathF.Min(left, points[i].X);
            top = MathF.Min(top, points[i].Y);
            right = MathF.Max(right, points[i].X);
            bottom = MathF.Max(bottom, points[i].Y);
            if (i > start)
                longestStep = MathF.Max(longestStep, MathF.Sqrt(Square(points[i].X - points[i - 1].X) + Square(points[i].Y - points[i - 1].Y)));
        }

        // The outline and its round caps reach at most half the width beyond the centre line, and the curve between
        // far apart points bulges out from the straight line between them by much less than half their distance. The
        // margin is generous because a dirty rect that is too small leaves stale ink, while one too large only costs a
        // little drawing.
        var margin = (2 * active.MaxWidth) + 2 + (longestStep / 2);
        return new RectF(left - margin, top - margin, right - left + (2 * margin), bottom - top + (2 * margin));
    }

    public void Dispose() => _committedInk.Dispose();

    private void DrawGuides(ICanvas canvas, float width, float height, bool showPlaceholder)
    {
        if (width <= 0 || height <= 0)
            return;

        canvas.SaveState();
        var lineY = height * LinePosition;
        var showLine = view.ShowSignatureLine && width > 2 * LineInset;
        if (showLine)
        {
            canvas.StrokeColor = view.SignatureLineColor;
            canvas.StrokeSize = 1;
            canvas.DrawLine(LineInset, lineY, width - LineInset, lineY);
        }

        if (showPlaceholder && !string.IsNullOrEmpty(view.Placeholder))
        {
            canvas.FontColor = view.PlaceholderColor;
            canvas.FontSize = PlaceholderFontSize;
            canvas.DrawString(view.Placeholder, 0, 0, width, showLine ? lineY : height,
                HorizontalAlignment.Center, VerticalAlignment.Center);
        }

        canvas.RestoreState();
    }

    private static float Square(float value) => value * value;

    // Walks back from the last point drawn before until the outline can no longer have changed.
    private static int StartOfChangedTail(ReadOnlySpan<InkPoint> points, int drawnCount, float reach)
    {
        if (drawnCount == 0)
            return 0;

        var start = drawnCount - 1;
        var distance = 0f;
        while (start > 0 && distance <= reach)
        {
            var dx = points[start].X - points[start - 1].X;
            var dy = points[start].Y - points[start - 1].Y;
            distance += MathF.Sqrt((dx * dx) + (dy * dy));
            start--;
        }

        return start;
    }
}
