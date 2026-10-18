using System.Numerics;
using Microsoft.Maui.Graphics.Text;

namespace Plugin.Maui.SignaturePad.Tests.Rendering;

/// <summary>An <see cref="ICanvas"/> that records what is drawn, with the state at the time of each call.</summary>
internal sealed class RecordingCanvas : ICanvas
{
    public sealed record Fill(PathF Path, Color Color, WindingMode WindingMode, bool Antialias);

    public sealed record Line(float X1, float Y1, float X2, float Y2, Color Color, float Size);

    public sealed record Text(string Value, RectF Rect, Color Color, float FontSize, HorizontalAlignment Horizontal, VerticalAlignment Vertical);

    private Color _fillColor = Colors.White;
    private Color _strokeColor = Colors.Black;
    private Color _fontColor = Colors.Black;
    private float _strokeSize = 1;
    private float _fontSize = 12;
    private bool _antialias;

    public List<Fill> Fills { get; } = [];

    public List<Line> Lines { get; } = [];

    public List<Text> Texts { get; } = [];

    public List<string> Calls { get; } = [];

    public List<(RectF Rect, Color Color)> FilledRectangles { get; } = [];

    public List<(float X, float Y)> Scales { get; } = [];

    public List<(float X, float Y)> Translations { get; } = [];

    public int SavedStates { get; private set; }

    public int RestoredStates { get; private set; }

    public float DisplayScale { get; set; } = 1;

    public float StrokeSize { set => _strokeSize = value; }

    public float MiterLimit { set { } }

    public Color StrokeColor { set => _strokeColor = value; }

    public LineCap StrokeLineCap { set { } }

    public LineJoin StrokeLineJoin { set { } }

    public float[] StrokeDashPattern { set { } }

    public float StrokeDashOffset { set { } }

    public Color FillColor { set => _fillColor = value; }

    public Color FontColor { set => _fontColor = value; }

    public IFont Font { set { } }

    public float FontSize { set => _fontSize = value; }

    public float Alpha { set { } }

    public bool Antialias { set => _antialias = value; }

    public BlendMode BlendMode { set { } }

    public void FillPath(PathF path, WindingMode windingMode)
    {
        Calls.Add(nameof(FillPath));
        Fills.Add(new Fill(path, _fillColor, windingMode, _antialias));
    }

    public void DrawLine(float x1, float y1, float x2, float y2)
    {
        Calls.Add(nameof(DrawLine));
        Lines.Add(new Line(x1, y1, x2, y2, _strokeColor, _strokeSize));
    }

    public void DrawString(string value, float x, float y, float width, float height, HorizontalAlignment horizontalAlignment,
        VerticalAlignment verticalAlignment, TextFlow textFlow = TextFlow.ClipBounds, float lineSpacingAdjustment = 0)
    {
        Calls.Add(nameof(DrawString));
        Texts.Add(new Text(value, new RectF(x, y, width, height), _fontColor, _fontSize, horizontalAlignment, verticalAlignment));
    }

    public void SaveState() => SavedStates++;

    public bool RestoreState()
    {
        RestoredStates++;
        return true;
    }

    public void DrawPath(PathF path) => Calls.Add(nameof(DrawPath));

    public void SubtractFromClip(float x, float y, float width, float height) { }

    public void ClipPath(PathF path, WindingMode windingMode = WindingMode.NonZero) { }

    public void ClipRectangle(float x, float y, float width, float height) { }

    public void DrawArc(float x, float y, float width, float height, float startAngle, float endAngle, bool clockwise, bool closed) { }

    public void FillArc(float x, float y, float width, float height, float startAngle, float endAngle, bool clockwise) { }

    public void DrawRectangle(float x, float y, float width, float height) { }

    public void FillRectangle(float x, float y, float width, float height)
    {
        Calls.Add(nameof(FillRectangle));
        FilledRectangles.Add((new RectF(x, y, width, height), _fillColor));
    }

    public void DrawRoundedRectangle(float x, float y, float width, float height, float cornerRadius) { }

    public void FillRoundedRectangle(float x, float y, float width, float height, float cornerRadius) { }

    public void DrawEllipse(float x, float y, float width, float height) { }

    public void FillEllipse(float x, float y, float width, float height) { }

    public void DrawString(string value, float x, float y, HorizontalAlignment horizontalAlignment) { }

    public void DrawText(IAttributedText value, float x, float y, float width, float height) { }

    public void Rotate(float degrees, float x, float y) { }

    public void Rotate(float degrees) { }

    public void Scale(float sx, float sy)
    {
        Calls.Add(nameof(Scale));
        Scales.Add((sx, sy));
    }

    public void Translate(float tx, float ty)
    {
        Calls.Add(nameof(Translate));
        Translations.Add((tx, ty));
    }

    public void ConcatenateTransform(Matrix3x2 transform) { }

    public void ResetState() { }

    public void SetShadow(SizeF offset, float blur, Color color) { }

    public void SetFillPaint(Paint paint, RectF rectangle) { }

    public void DrawImage(Microsoft.Maui.Graphics.IImage image, float x, float y, float width, float height) { }

    public SizeF GetStringSize(string value, IFont font, float fontSize) => SizeF.Zero;

    public SizeF GetStringSize(string value, IFont font, float fontSize, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment) => SizeF.Zero;
}
