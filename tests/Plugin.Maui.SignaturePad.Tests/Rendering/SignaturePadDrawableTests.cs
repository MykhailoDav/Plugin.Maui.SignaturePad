using Plugin.Maui.SignaturePad.Capture;
using Plugin.Maui.SignaturePad.Ink;
using Plugin.Maui.SignaturePad.Rendering;

namespace Plugin.Maui.SignaturePad.Tests.Rendering;

public class SignaturePadDrawableTests
{
    private static InkSample Touch(float x, float y, double t) => new(x, y, t, float.NaN, SignatureInputDevice.Touch);

    private static SignaturePadView Pad(float width = 300, float height = 200)
    {
        var view = new SignaturePadView { Frame = new Rect(0, 0, width, height) };
        return view;
    }

    private static void Draw(SignaturePadView view, double start, float y = 10)
    {
        IInkInputSink sink = view.Session;
        sink.BeginStroke(Touch(10, y, start));
        sink.ContinueStroke([Touch(40, y, start + 10), Touch(70, y + 5, start + 20)]);
        sink.EndStroke(Touch(100, y, start + 30));
    }

    private static RecordingCanvas Render(SignaturePadDrawable drawable)
    {
        var canvas = new RecordingCanvas();
        drawable.Draw(canvas, new RectF(0, 0, 300, 200));
        return canvas;
    }

    [Fact]
    public void Empty_DrawsTheLineAndThePlaceholder()
    {
        var view = Pad();
        view.Placeholder = "Sign here";
        var drawable = new SignaturePadDrawable(view);

        var canvas = Render(drawable);

        var line = Assert.Single(canvas.Lines);
        Assert.Equal(new RecordingCanvas.Line(16, 150, 284, 150, view.SignatureLineColor, 1), line);
        var text = Assert.Single(canvas.Texts);
        Assert.Equal(new RecordingCanvas.Text("Sign here", new RectF(0, 0, 300, 150), view.PlaceholderColor, 16,
            HorizontalAlignment.Center, VerticalAlignment.Center), text);
        Assert.Empty(canvas.Fills);
    }

    [Fact]
    public void DisplayStrokeColor_DrawsEveryStrokeInIt_WithoutChangingTheSignature()
    {
        var view = Pad();
        view.StrokeColor = Colors.Black;
        Draw(view, 0);
        view.StrokeColor = Colors.Red;
        Draw(view, 100, y: 60);
        view.DisplayStrokeColor = Colors.White;

        // A stroke being drawn is shown in the display color too.
        IInkInputSink sink = view.Session;
        sink.BeginStroke(Touch(10, 120, 200));
        sink.ContinueStroke([Touch(60, 120, 210)]);
        var canvas = Render(new SignaturePadDrawable(view));

        Assert.Equal(3, canvas.Fills.Count);
        Assert.All(canvas.Fills, fill => Assert.Equal(Colors.White, fill.Color));
        Assert.Equal([Colors.Black, Colors.Red], view.Signature.Strokes.Select(stroke => stroke.Color));
    }

    [Fact]
    public void WithoutDisplayStrokeColor_EachStrokeHasItsOwnColor()
    {
        var view = Pad();
        view.StrokeColor = Colors.Black;
        Draw(view, 0);
        view.StrokeColor = Colors.Red;
        Draw(view, 100, y: 60);

        var canvas = Render(new SignaturePadDrawable(view));

        Assert.Equal([Colors.Black, Colors.Red], canvas.Fills.Select(fill => fill.Color));
    }

    [Fact]
    public void Empty_WithoutPlaceholder_DrawsNoText()
    {
        var canvas = Render(new SignaturePadDrawable(Pad()));

        Assert.Empty(canvas.Texts);
        Assert.Single(canvas.Lines);
    }

    [Fact]
    public void WithoutTheLine_ThePlaceholderIsCentredInThePad()
    {
        var view = Pad();
        view.ShowSignatureLine = false;
        view.Placeholder = "Sign here";

        var canvas = Render(new SignaturePadDrawable(view));

        Assert.Empty(canvas.Lines);
        Assert.Equal(new RectF(0, 0, 300, 200), Assert.Single(canvas.Texts).Rect);
    }

    [Fact]
    public void NarrowPad_HasNoLine()
    {
        var canvas = Render(new SignaturePadDrawable(Pad(width: 30)));

        Assert.Empty(canvas.Lines);
    }

    [Fact]
    public void UnmeasuredPad_DrawsInkButNoGuides()
    {
        var view = new SignaturePadView { Placeholder = "Sign here" };
        Draw(view, 0);

        var canvas = Render(new SignaturePadDrawable(view));

        Assert.Empty(canvas.Lines);
        Assert.Empty(canvas.Texts);
        Assert.Single(canvas.Fills);
    }

    [Fact]
    public void WithStrokes_ThePlaceholderIsHidden_AndTheLineStays()
    {
        var view = Pad();
        view.Placeholder = "Sign here";
        Draw(view, 0);

        var canvas = Render(new SignaturePadDrawable(view));

        Assert.Empty(canvas.Texts);
        Assert.Single(canvas.Lines);
    }

    [Fact]
    public void WhileTheFirstStrokeIsDrawn_ThePlaceholderIsHidden()
    {
        var view = Pad();
        view.Placeholder = "Sign here";
        ((IInkInputSink)view.Session).BeginStroke(Touch(10, 10, 0));

        var canvas = Render(new SignaturePadDrawable(view));

        Assert.Empty(canvas.Texts);
    }

    [Fact]
    public void EachCommittedStroke_IsOneAntialiasedNonZeroFill_InItsColor()
    {
        var view = Pad();
        Draw(view, 0, y: 10);
        view.StrokeColor = Colors.Red;
        Draw(view, 100, y: 60);

        var canvas = Render(new SignaturePadDrawable(view));

        Assert.Equal(2, canvas.Fills.Count);
        Assert.Equal(Colors.Black, canvas.Fills[0].Color);
        Assert.Equal(Colors.Red, canvas.Fills[1].Color);
        Assert.All(canvas.Fills, fill => Assert.Equal(WindingMode.NonZero, fill.WindingMode));
        Assert.All(canvas.Fills, fill => Assert.True(fill.Antialias));
    }

    [Fact]
    public void TheActiveStroke_IsDrawnLast_InItsOwnColor()
    {
        var view = Pad();
        Draw(view, 0);
        view.StrokeColor = Colors.Blue;
        IInkInputSink sink = view.Session;
        sink.BeginStroke(Touch(10, 100, 100));
        sink.ContinueStroke([Touch(60, 100, 110)]);
        view.StrokeColor = Colors.Green;

        var canvas = Render(new SignaturePadDrawable(view));

        Assert.Equal(2, canvas.Fills.Count);
        Assert.Equal(Colors.Blue, canvas.Fills[^1].Color);
        Assert.Equal("FillPath", canvas.Calls[^1]);
    }

    [Fact]
    public void CommittedPaths_AreReusedAcrossFrames()
    {
        var view = Pad();
        Draw(view, 0);
        var drawable = new SignaturePadDrawable(view);

        var first = Render(drawable).Fills[0].Path;
        var second = Render(drawable).Fills[0].Path;

        Assert.Same(first, second);
    }

    [Fact]
    public void PathsOfRemovedStrokes_AreDropped()
    {
        var view = Pad();
        Draw(view, 0, y: 10);
        Draw(view, 100, y: 60);
        var ink = new PathCacheInkRenderer();
        var drawable = new SignaturePadDrawable(view, ink);
        Render(drawable);
        Assert.Equal(2, ink.CachedPathCount);

        view.Undo();
        Render(drawable);

        Assert.Equal(1, ink.CachedPathCount);
    }

    [Fact]
    public void AddingAStroke_KeepsTheCachedPaths()
    {
        var view = Pad();
        Draw(view, 0, y: 10);
        var ink = new PathCacheInkRenderer();
        var drawable = new SignaturePadDrawable(view, ink);
        var first = Render(drawable).Fills[0].Path;

        Draw(view, 100, y: 60);
        var canvas = Render(drawable);

        Assert.Same(first, canvas.Fills[0].Path);
        Assert.Equal(2, ink.CachedPathCount);
    }

    [Fact]
    public void CanvasState_IsBalanced()
    {
        var view = Pad();
        view.Placeholder = "Sign here";
        var canvas = Render(new SignaturePadDrawable(view));

        Assert.True(canvas.SavedStates > 0);
        Assert.Equal(canvas.SavedStates, canvas.RestoredStates);
    }

    [Fact]
    public void Dispose_ReleasesTheCache()
    {
        var view = Pad();
        Draw(view, 0);
        var ink = new PathCacheInkRenderer();
        var drawable = new SignaturePadDrawable(view, ink);
        Render(drawable);

        drawable.Dispose();

        Assert.Equal(0, ink.CachedPathCount);
    }

    [Fact]
    public void ActiveDirtyRect_IsEmpty_WithoutAnActiveStroke() =>
        Assert.Equal(RectF.Zero, new SignaturePadDrawable(Pad()).TakeActiveDirtyRect());

    [Fact]
    public void ActiveDirtyRect_CoversTheWholeNewStroke_ThenOnlyItsTail()
    {
        var view = Pad();
        var drawable = new SignaturePadDrawable(view);
        IInkInputSink sink = view.Session;
        sink.BeginStroke(Touch(10, 100, 0));
        for (var i = 1; i <= 40; i++)
            sink.ContinueStroke([Touch(10 + (i * 5), 100, i * 10)]);

        var whole = drawable.TakeActiveDirtyRect();
        sink.ContinueStroke([Touch(215, 100, 410)]);
        var tail = drawable.TakeActiveDirtyRect();

        var margin = (2 * 3.5f) + 2;
        Assert.True(whole.Left <= 10 - margin + 0.01f);
        Assert.True(whole.Right >= 210);
        Assert.True(tail.Left > 150, $"The tail starts at {tail.Left}.");
        Assert.True(tail.Right >= 215);
        Assert.True(tail.Top <= 100 - margin + 0.01f && tail.Bottom >= 100 + margin - 0.01f);
    }

    [Fact]
    public void ActiveDirtyRect_CoversTheCurveBetweenSparseSamples()
    {
        // Far apart samples are joined by a curve, which bulges beyond the box of the samples at a turn.
        var view = Pad();
        var drawable = new SignaturePadDrawable(view);
        IInkInputSink sink = view.Session;
        sink.BeginStroke(Touch(10, 10, 0));
        sink.ContinueStroke([Touch(210, 10, 100)]);
        drawable.TakeActiveDirtyRect();

        sink.ContinueStroke([Touch(210, 210, 200)]);
        var rect = drawable.TakeActiveDirtyRect();

        var outline = StrokeOutline.Build(view.Session.Active!.RenderPoints);
        Assert.True(outline.Max(point => point.X) > 210 + 9, "The curve does not bulge out; the test needs a sharper turn.");
        Assert.All(outline, point => Assert.True(rect.Contains(point), $"{point} is outside {rect}."));
    }

    [Fact]
    public void ActiveDirtyRect_StartsOverForANewStroke()
    {
        var view = Pad();
        var drawable = new SignaturePadDrawable(view);
        IInkInputSink sink = view.Session;
        sink.BeginStroke(Touch(10, 10, 0));
        sink.ContinueStroke([Touch(100, 10, 10)]);
        drawable.TakeActiveDirtyRect();
        sink.EndStroke(Touch(120, 10, 20));

        sink.BeginStroke(Touch(10, 150, 100));
        sink.ContinueStroke([Touch(20, 150, 110)]);
        var rect = drawable.TakeActiveDirtyRect();

        Assert.True(rect.Top > 100);
        Assert.True(rect.Left <= 10);
    }

    [Fact]
    public void ActiveDirtyRect_ForAShortStroke_ReachesBackToItsStart()
    {
        var view = Pad();
        var drawable = new SignaturePadDrawable(view);
        IInkInputSink sink = view.Session;
        sink.BeginStroke(Touch(10, 10, 0));
        sink.ContinueStroke([Touch(12, 10, 10)]);
        drawable.TakeActiveDirtyRect();

        sink.ContinueStroke([Touch(14, 10, 20)]);
        var rect = drawable.TakeActiveDirtyRect();

        Assert.True(rect.Left <= 10 - 9 + 0.01f);
    }

    [Fact]
    public void InkRenderer_ForBitmaps_MovesAndScalesTheGeometry_AndUsesTheOverrideColor()
    {
        var view = Pad();
        Draw(view, 0);
        var stroke = view.Signature.Strokes[0];
        var canvas = new RecordingCanvas();

        InkRenderer.DrawStrokes(canvas, view.Signature.Strokes, Colors.Magenta, new PointF(5, 3), 2.5f);

        var fill = Assert.Single(canvas.Fills);
        Assert.Equal(Colors.Magenta, fill.Color);
        using var untransformed = StrokeGeometry.ToPath(stroke.Outline);
        Assert.Equal(untransformed.Count, fill.Path.Count);
        for (var i = 0; i < untransformed.Count; i++)
        {
            Assert.Equal((untransformed[i].X - 5) * 2.5f, fill.Path[i].X, 0.001f);
            Assert.Equal((untransformed[i].Y - 3) * 2.5f, fill.Path[i].Y, 0.001f);
        }

        Assert.Empty(canvas.Scales);
        Assert.Empty(canvas.Translations);
    }
}
