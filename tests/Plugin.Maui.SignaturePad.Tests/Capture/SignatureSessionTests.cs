using Plugin.Maui.SignaturePad.Capture;
using Plugin.Maui.SignaturePad.Ink;

namespace Plugin.Maui.SignaturePad.Tests.Capture;

public class SignatureSessionTests
{
    private InkStyle _style = new(Colors.Black, 1f, 3.5f);

    private SignatureSession NewSession() => new(() => _style) { CanvasSize = new SizeF(300, 200) };

    private static InkSample Touch(float x, float y, double t) => new(x, y, t, float.NaN, SignatureInputDevice.Touch);

    /// <summary>Draws a straight horizontal stroke of <paramref name="points"/> points starting at platform time <paramref name="start"/>.</summary>
    private static void Draw(IInkInputSink sink, double start, float y = 10, int points = 5)
    {
        sink.BeginStroke(Touch(10, y, start));
        for (var i = 1; i < points - 1; i++)
            sink.ContinueStroke([Touch(10 + (i * 10), y, start + (i * 10))]);
        sink.EndStroke(Touch(10 + ((points - 1) * 10), y, start + ((points - 1) * 10)));
    }

    [Fact]
    public void NewSession_IsEmpty()
    {
        var session = NewSession();

        Assert.True(session.Current.IsEmpty);
        Assert.Null(session.Active);
        Assert.False(session.CanUndo);
        Assert.False(session.CanRedo);
        Assert.True(session.IsInputEnabled);
    }

    [Fact]
    public void Begin_Continue_End_CommitsOneStroke_AndRaisesEventsInOrder()
    {
        var session = NewSession();
        var events = new List<string>();
        session.StrokeStarted += () => events.Add("started");
        session.ActiveStrokeChanged += () => events.Add("active");
        session.StrokeCompleted += stroke => events.Add($"completed:{stroke.Points.Count}");
        session.Changed += () => events.Add("changed");

        Draw(session, start: 1000);

        Assert.Equal(["started", "active", "active", "active", "active", "changed", "completed:5"], events);
        var stroke = Assert.Single(session.Current.Strokes);
        Assert.Equal(new SizeF(300, 200), session.Current.CanvasSize);
        Assert.Null(session.Active);
        Assert.True(session.CanUndo);
        Assert.Equal(SignatureInputDevice.Touch, stroke.Device);
    }

    [Fact]
    public void Active_HoldsTheStrokeWhileDrawing()
    {
        var session = NewSession();

        session.BeginStroke(Touch(0, 0, 0));
        session.ContinueStroke([Touch(10, 0, 10), Touch(20, 0, 20)]);

        Assert.NotNull(session.Active);
        Assert.Equal(3, session.Active.Count);
        Assert.True(session.Current.IsEmpty);
    }

    [Fact]
    public void StyleIsCapturedAtBegin()
    {
        var session = NewSession();
        session.BeginStroke(Touch(0, 0, 0));

        _style = new InkStyle(Colors.Red, 2f, 6f);
        session.EndStroke(Touch(20, 0, 20));

        var stroke = Assert.Single(session.Current.Strokes);
        Assert.Equal(Colors.Black, stroke.Color);
        Assert.Equal(1f, stroke.MinWidth);
        Assert.Equal(3.5f, stroke.MaxWidth);
    }

    [Fact]
    public void Undo_RemovesTheLastStroke_AndRedoReappliesIt()
    {
        var session = NewSession();
        Draw(session, 0, y: 10);
        Draw(session, 100, y: 50);
        var second = session.Current.Strokes[1];

        Assert.True(session.Undo());
        Assert.Single(session.Current.Strokes);
        Assert.True(session.CanRedo);

        Assert.True(session.Redo());
        Assert.Equal(2, session.Current.Strokes.Count);
        Assert.Same(second, session.Current.Strokes[1]);
        Assert.False(session.CanRedo);
    }

    [Fact]
    public void Undo_Redo_ReturnFalse_WhenThereIsNothingToDo()
    {
        var session = NewSession();
        var changes = 0;
        session.Changed += () => changes++;

        Assert.False(session.Undo());
        Assert.False(session.Redo());
        Assert.Equal(0, changes);
    }

    [Fact]
    public void NewStroke_ClearsTheRedoStack()
    {
        var session = NewSession();
        Draw(session, 0);
        session.Undo();

        Draw(session, 100);

        Assert.False(session.CanRedo);
    }

    [Fact]
    public void Clear_IsUndoable_AndRestoresEveryStroke()
    {
        var session = NewSession();
        Draw(session, 0, y: 10);
        Draw(session, 100, y: 50);
        var before = session.Current;

        session.Clear();
        Assert.True(session.Current.IsEmpty);

        Assert.True(session.Undo());
        Assert.Equal(before.Strokes, session.Current.Strokes);

        Assert.True(session.Redo());
        Assert.True(session.Current.IsEmpty);
    }

    [Fact]
    public void ClearUndoRedoAndLoad_EachRaiseChangedOnce()
    {
        var session = NewSession();
        Draw(session, 0);
        var changes = 0;
        session.Changed += () => changes++;

        session.Clear();
        Assert.Equal(1, changes);
        session.Undo();
        Assert.Equal(2, changes);
        session.Redo();
        Assert.Equal(3, changes);
        session.Load(SignatureData.Empty);
        Assert.Equal(4, changes);
    }

    [Fact]
    public void Clear_OnAnEmptyPad_IsNoOp_WithoutHistory()
    {
        var session = NewSession();
        var changes = 0;
        session.Changed += () => changes++;

        session.Clear();

        Assert.False(session.CanUndo);
        Assert.Equal(0, changes);
    }

    [Fact]
    public void Load_ReplacesEverything_AndResetsHistory()
    {
        var session = NewSession();
        Draw(session, 0);
        var loaded = new SignatureData(
            [new SignatureStroke([new(1, 1, 0), new(5, 5, 400)], Colors.Blue, 1, 2, SignatureInputDevice.Pen)], new SizeF(50, 50));

        session.Load(loaded);

        Assert.Same(loaded, session.Current);
        Assert.False(session.CanUndo);
        Assert.False(session.CanRedo);
    }

    [Fact]
    public void Load_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => NewSession().Load(null!));

    [Fact]
    public void Load_DuringAStroke_DiscardsIt()
    {
        var session = NewSession();
        session.BeginStroke(Touch(0, 0, 0));

        session.Load(SignatureData.Empty);

        Assert.Null(session.Active);
        Assert.True(session.Current.IsEmpty);
    }

    [Fact]
    public void Timeline_FirstStroke_StartsAtZero()
    {
        var session = NewSession();

        Draw(session, start: 123456);

        Assert.Equal(0, session.Current.Strokes[0].StartTime);
        Assert.Equal(40, session.Current.Strokes[0].EndTime);
    }

    [Fact]
    public void Timeline_SecondStroke_KeepsTheRealGap()
    {
        var session = NewSession();
        Draw(session, start: 1000);

        Draw(session, start: 1500);

        Assert.Equal(500, session.Current.Strokes[1].StartTime);
    }

    [Fact]
    public void Timeline_AfterLoad_NextStrokeStartsAtTheLastLoadedTime()
    {
        var session = NewSession();
        session.Load(new SignatureData(
            [new SignatureStroke([new(1, 1, 0), new(5, 5, 400)], Colors.Blue, 1, 2, SignatureInputDevice.Pen)], new SizeF(50, 50)));

        Draw(session, start: 99999);

        Assert.Equal(400, session.Current.Strokes[1].StartTime);
    }

    [Fact]
    public void Timeline_AfterLoadingEmptyData_StartsAtZero()
    {
        var session = NewSession();
        session.Load(SignatureData.Empty);

        Draw(session, start: 777);

        Assert.Equal(0, session.Current.Strokes[0].StartTime);
    }

    [Fact]
    public void Timeline_ResetsAfterClearingToEmpty()
    {
        var session = NewSession();
        Draw(session, start: 1000);
        session.Clear();

        Draw(session, start: 5000);

        Assert.Equal(0, session.Current.Strokes[0].StartTime);
    }

    [Fact]
    public void Timeline_NeverGoesBackwards_AfterUndoingAClear()
    {
        var session = NewSession();
        Draw(session, start: 1000);
        Draw(session, start: 3000);
        session.Clear();
        Draw(session, start: 5000);
        session.Undo();
        session.Undo();

        Draw(session, start: 5100);

        var strokes = session.Current.Strokes;
        Assert.True(strokes[2].StartTime >= strokes[1].EndTime);
    }

    [Fact]
    public void Cancel_CommitsThePartialStroke_Once()
    {
        var session = NewSession();
        var completed = 0;
        session.StrokeCompleted += _ => completed++;
        session.BeginStroke(Touch(0, 0, 0));
        session.ContinueStroke([Touch(10, 0, 10)]);

        session.CancelStroke();
        session.CancelStroke();

        Assert.Equal(1, completed);
        Assert.Equal(2, Assert.Single(session.Current.Strokes).Points.Count);
    }

    [Fact]
    public void End_And_Continue_WithoutBegin_AreIgnored()
    {
        var session = NewSession();
        var changes = 0;
        session.Changed += () => changes++;
        session.ActiveStrokeChanged += () => changes++;

        session.ContinueStroke([Touch(10, 0, 10)]);
        session.EndStroke(Touch(20, 0, 20));

        Assert.True(session.Current.IsEmpty);
        Assert.Equal(0, changes);
    }

    [Fact]
    public void SecondBegin_CommitsTheFirstStroke()
    {
        var session = NewSession();
        session.BeginStroke(Touch(0, 0, 0));
        session.ContinueStroke([Touch(10, 0, 10)]);

        session.BeginStroke(Touch(50, 50, 100));
        session.EndStroke(Touch(60, 50, 110));

        Assert.Equal(2, session.Current.Strokes.Count);
    }

    [Fact]
    public void Disable_DuringAStroke_CommitsIt_AndIgnoresFurtherInput()
    {
        var session = NewSession();
        session.BeginStroke(Touch(0, 0, 0));
        session.ContinueStroke([Touch(10, 0, 10)]);

        session.IsInputEnabled = false;
        session.ContinueStroke([Touch(20, 0, 20)]);
        session.BeginStroke(Touch(30, 0, 30));

        Assert.False(session.IsInputEnabled);
        Assert.Equal(2, Assert.Single(session.Current.Strokes).Points.Count);
        Assert.Null(session.Active);
    }

    [Fact]
    public void Disable_WhenIdle_JustDisables()
    {
        var session = NewSession();

        session.IsInputEnabled = false;
        session.IsInputEnabled = true;
        Draw(session, 0);

        Assert.Single(session.Current.Strokes);
    }

    [Fact]
    public void NonFiniteSamples_AreDropped()
    {
        var session = NewSession();

        session.BeginStroke(Touch(float.NaN, 0, 0));
        Assert.Null(session.Active);

        session.BeginStroke(Touch(0, 0, 0));
        session.ContinueStroke([Touch(float.PositiveInfinity, 0, 5), Touch(10, 0, 10), Touch(20, 0, double.NaN)]);
        session.EndStroke(Touch(30, float.NaN, 30));

        Assert.Equal(2, Assert.Single(session.Current.Strokes).Points.Count);
    }

    [Fact]
    public void ContinueWithOnlyDroppedSamples_RaisesNothing()
    {
        var session = NewSession();
        session.BeginStroke(Touch(0, 0, 0));
        var changes = 0;
        session.ActiveStrokeChanged += () => changes++;

        session.ContinueStroke([Touch(float.NaN, 0, 5)]);
        session.ContinueStroke([]);

        Assert.Equal(0, changes);
    }

    [Fact]
    public void UndoClearOrRedo_DuringAStroke_CommitItFirst()
    {
        var session = NewSession();
        Draw(session, 0, y: 10);
        session.BeginStroke(Touch(0, 50, 100));
        session.ContinueStroke([Touch(10, 50, 110)]);

        Assert.True(session.Undo());
        Assert.Single(session.Current.Strokes);
        Assert.Null(session.Active);

        session.BeginStroke(Touch(0, 80, 200));
        session.Clear();
        Assert.True(session.Current.IsEmpty);
        Assert.True(session.Undo());
        Assert.Equal(2, session.Current.Strokes.Count);

        session.BeginStroke(Touch(0, 90, 300));
        Assert.False(session.Redo());
        Assert.Equal(3, session.Current.Strokes.Count);
    }

    [Fact]
    public void CanvasSize_IsUsedForTheNextChange_WithoutChangingTheCurrentData()
    {
        var session = NewSession();
        Draw(session, 0);
        var before = session.Current;

        session.CanvasSize = new SizeF(400, 300);

        Assert.Same(before, session.Current);
        Draw(session, 100);
        Assert.Equal(new SizeF(400, 300), session.Current.CanvasSize);
    }
}
