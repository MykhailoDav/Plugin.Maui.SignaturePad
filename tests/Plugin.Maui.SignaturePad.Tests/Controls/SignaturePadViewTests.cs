using System.Windows.Input;
using Plugin.Maui.SignaturePad.Capture;
using Plugin.Maui.SignaturePad.Ink;

namespace Plugin.Maui.SignaturePad.Tests.Controls;

public class SignaturePadViewTests
{
    private static InkSample Touch(float x, float y, double t) => new(x, y, t, float.NaN, SignatureInputDevice.Touch);

    private static void Draw(SignaturePadView view, double start, float y = 10)
    {
        IInkInputSink sink = view.Session;
        sink.BeginStroke(Touch(10, y, start));
        sink.ContinueStroke([Touch(30, y, start + 10), Touch(50, y, start + 20)]);
        sink.EndStroke(Touch(70, y, start + 30));
    }

    private static SignatureData OneStroke() => new(
        [new SignatureStroke([new(1, 1, 0), new(40, 5, 100)], Colors.Blue, 1, 2, SignatureInputDevice.Pen)], new SizeF(100, 50));

    [Fact]
    public void Defaults_MatchTheSpec()
    {
        var view = new SignaturePadView();

        Assert.Equal(Colors.Black, view.StrokeColor);
        Assert.Equal(1.0, view.MinStrokeWidth);
        Assert.Equal(3.5, view.MaxStrokeWidth);
        Assert.Null(view.Placeholder);
        Assert.Equal(Color.FromArgb("#9E9E9E"), view.PlaceholderColor);
        Assert.True(view.ShowSignatureLine);
        Assert.Equal(Color.FromArgb("#BDBDBD"), view.SignatureLineColor);
        Assert.Same(SignatureData.Empty, view.Signature);
        Assert.Null(view.StrokeCompletedCommand);
        Assert.True(view.IsEmpty);
        Assert.False(view.CanUndo);
        Assert.False(view.CanRedo);
        Assert.False(view.UndoCommand.CanExecute(null));
        Assert.False(view.RedoCommand.CanExecute(null));
        Assert.False(view.ClearCommand.CanExecute(null));
    }

    [Fact]
    public void VisualProperties_KeepTheirValues()
    {
        var view = new SignaturePadView
        {
            Placeholder = "Sign here",
            PlaceholderColor = Colors.Gray,
            ShowSignatureLine = false,
            SignatureLineColor = Colors.Silver,
        };

        Assert.Equal("Sign here", view.Placeholder);
        Assert.Equal(Colors.Gray, view.PlaceholderColor);
        Assert.False(view.ShowSignatureLine);
        Assert.Equal(Colors.Silver, view.SignatureLineColor);
    }

    [Fact]
    public void Drawing_UpdatesTheStateAndCommands()
    {
        var view = new SignaturePadView();
        var changed = new List<string?>();
        view.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        Draw(view, 0);

        Assert.Single(view.Signature.Strokes);
        Assert.False(view.IsEmpty);
        Assert.True(view.CanUndo);
        Assert.True(view.UndoCommand.CanExecute(null));
        Assert.True(view.ClearCommand.CanExecute(null));
        Assert.False(view.RedoCommand.CanExecute(null));
        Assert.Contains(nameof(SignaturePadView.Signature), changed);
        Assert.Contains(nameof(SignaturePadView.IsEmpty), changed);
        Assert.Contains(nameof(SignaturePadView.CanUndo), changed);
    }

    [Fact]
    public void Commands_RaiseCanExecuteChanged_WhenTheStateChanges()
    {
        var view = new SignaturePadView();
        var raised = 0;
        view.UndoCommand.CanExecuteChanged += (_, _) => raised++;

        Draw(view, 0);

        Assert.True(raised > 0);
    }

    [Fact]
    public void UndoRedoClear_WorkThroughMethodsAndCommands()
    {
        var view = new SignaturePadView();
        Draw(view, 0, y: 10);
        Draw(view, 100, y: 50);

        Assert.True(view.Undo());
        Assert.Single(view.Signature.Strokes);
        Assert.True(view.CanRedo);

        view.RedoCommand.Execute(null);
        Assert.Equal(2, view.Signature.Strokes.Count);

        view.ClearCommand.Execute(null);
        Assert.True(view.IsEmpty);

        view.UndoCommand.Execute(null);
        Assert.Equal(2, view.Signature.Strokes.Count);

        view.Clear();
        Assert.True(view.IsEmpty);
        Assert.True(view.Redo() is false);
        Assert.True(view.Undo());
    }

    [Fact]
    public void Signature_SetToTheSameReference_IsNoOp()
    {
        var view = new SignaturePadView();
        Draw(view, 0);
        var current = view.Signature;

        view.Signature = current;

        Assert.True(view.CanUndo);
    }

    [Fact]
    public void Signature_SetToOtherData_LoadsIt_AndResetsHistory()
    {
        var view = new SignaturePadView();
        Draw(view, 0);
        var loaded = OneStroke();

        view.Signature = loaded;

        Assert.Same(loaded, view.Signature);
        Assert.Same(loaded, view.Session.Current);
        Assert.False(view.CanUndo);
        Assert.False(view.IsEmpty);
    }

    [Fact]
    public void Signature_SetToNull_BecomesEmpty()
    {
        var view = new SignaturePadView { Signature = OneStroke() };

        view.Signature = null!;

        Assert.Same(SignatureData.Empty, view.Signature);
        Assert.True(view.IsEmpty);
    }

    [Fact]
    public void Signature_TwoWayBinding_FlowsBothWays()
    {
        var source = new SignatureSource();
        var view = new SignaturePadView { BindingContext = source };
        view.SetBinding(SignaturePadView.SignatureProperty, static (SignatureSource s) => s.Signature, BindingMode.Default);

        Draw(view, 0);
        Assert.Same(view.Signature, source.Signature);

        source.Signature = OneStroke();
        Assert.Same(source.Signature, view.Signature);
    }

    [Fact]
    public void Signature_OneWayBinding_SurvivesDrawing()
    {
        var source = new SignatureSource();
        var view = new SignaturePadView { BindingContext = source };
        view.SetBinding(SignaturePadView.SignatureProperty, static (SignatureSource s) => s.Signature, BindingMode.OneWay);

        Draw(view, 0);
        Assert.Same(SignatureData.Empty, source.Signature);

        var loaded = OneStroke();
        source.Signature = loaded;
        Assert.Same(loaded, view.Signature);
    }

    [Fact]
    public void InvalidJson_LeavesTheStateUnchanged()
    {
        var view = new SignaturePadView();
        Draw(view, 0);
        var before = view.Signature;

        Assert.Throws<FormatException>(() => view.Signature = SignatureData.FromJson("{\"format\":\"nope\"}"));

        Assert.Same(before, view.Signature);
        Assert.True(view.CanUndo);
    }

    [Fact]
    public void StrokeEvents_AreRaised_AfterTheSignatureIsUpdated()
    {
        var view = new SignaturePadView();
        var started = 0;
        SignatureStroke? completed = null;
        var strokesSeenByHandler = -1;
        view.StrokeStarted += (sender, _) => { Assert.Same(view, sender); started++; };
        view.StrokeCompleted += (sender, e) =>
        {
            Assert.Same(view, sender);
            completed = e.Stroke;
            strokesSeenByHandler = view.Signature.Strokes.Count;
        };

        Draw(view, 0);

        Assert.Equal(1, started);
        Assert.Same(view.Signature.Strokes[0], completed);
        Assert.Equal(1, strokesSeenByHandler);
    }

    [Fact]
    public void StrokeCompletedCommand_ReceivesTheStroke_WhenItCanExecute()
    {
        var command = new RecordingCommand();
        var view = new SignaturePadView { StrokeCompletedCommand = command };

        Draw(view, 0);
        command.CanRun = false;
        Draw(view, 100, y: 50);

        Assert.Same(view.Signature.Strokes[0], Assert.Single(command.Executed));
    }

    [Fact]
    public void IsEnabledFalse_IgnoresInput_UntilEnabledAgain()
    {
        var view = new SignaturePadView { IsEnabled = false };

        Draw(view, 0);
        Assert.True(view.IsEmpty);

        view.IsEnabled = true;
        Draw(view, 100);
        Assert.False(view.IsEmpty);
    }

    [Fact]
    public void StrokeWidths_AreCoercedToAtLeastATenth()
    {
        var view = new SignaturePadView { MinStrokeWidth = 0.01, MaxStrokeWidth = -3 };

        Assert.Equal(0.1, view.MinStrokeWidth);
        Assert.Equal(0.1, view.MaxStrokeWidth);
    }

    [Fact]
    public void MaxBelowMin_DrawsWithTheMinimumWidth_WhateverTheOrder()
    {
        var view = new SignaturePadView { MaxStrokeWidth = 3, MinStrokeWidth = 5 };

        Draw(view, 0);

        Assert.Equal(3, view.MaxStrokeWidth);
        Assert.Equal(5f, view.Signature.Strokes[0].MinWidth);
        Assert.Equal(5f, view.Signature.Strokes[0].MaxWidth);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void StrokeWidths_IgnoreNonFiniteValues(double value)
    {
        var view = new SignaturePadView { MinStrokeWidth = 2, MaxStrokeWidth = 4 };

        view.MinStrokeWidth = value;
        view.MaxStrokeWidth = value;

        Assert.Equal(2, view.MinStrokeWidth);
        Assert.Equal(4, view.MaxStrokeWidth);
    }

    [Fact]
    public void Ink_AppliesToNewStrokesOnly()
    {
        var view = new SignaturePadView();
        Draw(view, 0);

        view.StrokeColor = Colors.Red;
        view.MinStrokeWidth = 2;
        view.MaxStrokeWidth = 6;
        Draw(view, 100, y: 50);

        Assert.Equal(Colors.Black, view.Signature.Strokes[0].Color);
        Assert.Equal(Colors.Red, view.Signature.Strokes[1].Color);
        Assert.Equal(2f, view.Signature.Strokes[1].MinWidth);
        Assert.Equal(6f, view.Signature.Strokes[1].MaxWidth);
    }

    [Fact]
    public void SessionChanges_RequestARedraw()
    {
        var view = new SignaturePadView();
        var full = 0;
        var active = 0;
        view.InvalidateRequested += () => full++;
        view.ActiveStrokeUpdated += () => active++;

        Draw(view, 0);

        Assert.Equal(2, active);
        Assert.Equal(1, full);
    }

    [Fact]
    public void Session_IsCreatedOnce_AndSurvivesStateChanges()
    {
        var view = new SignaturePadView();
        var session = view.Session;

        Draw(view, 0);
        view.Signature = OneStroke();
        view.IsEnabled = false;

        Assert.Same(session, view.Session);
    }

    [Fact]
    public void CanvasSize_FollowsTheViewSize()
    {
        var view = new SignaturePadView();

        view.Frame = new Rect(0, 0, 320, 180);
        Draw(view, 0);

        Assert.Equal(new SizeF(320, 180), view.Signature.CanvasSize);
    }

    [Fact]
    public void ExportSvg_DelegatesToTheSignature()
    {
        var view = new SignaturePadView { Signature = OneStroke() };

        Assert.Equal(OneStroke().ToSvg(), view.ExportSvg());
    }

    [Fact]
    public async Task ExportImageAsync_WithoutAPlatform_ThrowsPlatformNotSupported()
    {
        var view = new SignaturePadView { Signature = OneStroke() };

        await Assert.ThrowsAsync<PlatformNotSupportedException>(() => view.ExportImageAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ExportImageAsync_Empty_ThrowsInvalidOperation()
    {
        var view = new SignaturePadView();

        await Assert.ThrowsAsync<InvalidOperationException>(() => view.ExportImageAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ExportImageAsync_Cancelled_Throws()
    {
        var view = new SignaturePadView { Signature = OneStroke() };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => view.ExportImageAsync(new SignatureImageExportOptions(), new CancellationToken(canceled: true)));
    }

    internal sealed class SignatureSource : BindableObject
    {
        public static readonly BindableProperty SignatureProperty =
            BindableProperty.Create(nameof(Signature), typeof(SignatureData), typeof(SignatureSource), SignatureData.Empty);

        public SignatureData Signature
        {
            get => (SignatureData)GetValue(SignatureProperty);
            set => SetValue(SignatureProperty, value);
        }
    }

    private sealed class RecordingCommand : ICommand
    {
        public bool CanRun { get; set; } = true;

        public List<object?> Executed { get; } = [];

        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => CanRun;

        public void Execute(object? parameter) => Executed.Add(parameter);
    }
}
