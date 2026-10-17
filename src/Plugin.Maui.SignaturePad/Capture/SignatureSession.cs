using Plugin.Maui.SignaturePad.Ink;

namespace Plugin.Maui.SignaturePad.Capture;

/// <summary>The ink a new stroke is drawn with.</summary>
internal readonly record struct InkStyle(Color Color, float MinWidth, float MaxWidth);

/// <summary>
/// The state of a pad: the committed signature, the stroke being drawn, the undo history and the signature's clock.
/// It lives as long as the <c>SignaturePadView</c>, not its handler, so the ink survives the native view being
/// recreated by navigation, tabs or recycling.
/// </summary>
internal sealed class SignatureSession : IInkInputSink
{
    private readonly Func<InkStyle> _currentStyle;
    private readonly Stack<HistoryEntry> _undo = new();
    private readonly Stack<HistoryEntry> _redo = new();
    private Color _activeColor = Colors.Black;
    private double? _timeOrigin;
    private bool _isInputEnabled = true;

    /// <param name="currentStyle">Gives the ink to use when a stroke starts.</param>
    public SignatureSession(Func<InkStyle> currentStyle) => _currentStyle = currentStyle;

    /// <summary>Raised when the committed signature or the history changes.</summary>
    public event Action? Changed;

    /// <summary>Raised when the stroke being drawn starts or gets new samples.</summary>
    public event Action? ActiveStrokeChanged;

    public event Action? StrokeStarted;

    public event Action<SignatureStroke>? StrokeCompleted;

    /// <summary>The committed signature, without the stroke being drawn.</summary>
    public SignatureData Current { get; private set; } = SignatureData.Empty;

    /// <summary>The stroke being drawn, if any.</summary>
    public StrokeBuilder? Active { get; private set; }

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    /// <summary>
    /// The size of the pad. It is recorded with the next change of the signature rather than creating a new signature
    /// by itself, so a layout pass does not ripple through bindings.
    /// </summary>
    public SizeF CanvasSize { get; set; }

    /// <summary>Whether input is accepted. Turning it off ends the stroke being drawn, keeping its ink.</summary>
    public bool IsInputEnabled
    {
        get => _isInputEnabled;
        set
        {
            if (!value)
                CancelStroke();
            _isInputEnabled = value;
        }
    }

    public void BeginStroke(in InkSample sample)
    {
        if (!_isInputEnabled || !IsFinite(sample))
            return;

        // A second begin without an end means the platform lost an event; keep what was drawn.
        CancelStroke();

        var style = _currentStyle();
        _activeColor = style.Color;
        Active = new StrokeBuilder(sample, style.MinWidth, style.MaxWidth, TimeOriginFor(sample.TimestampMs));
        StrokeStarted?.Invoke();
        ActiveStrokeChanged?.Invoke();
    }

    public void ContinueStroke(ReadOnlySpan<InkSample> samples)
    {
        if (Active is null)
            return;

        var added = false;
        foreach (ref readonly var sample in samples)
        {
            if (IsFinite(sample))
            {
                Active.Add(sample);
                added = true;
            }
        }

        if (added)
            ActiveStrokeChanged?.Invoke();
    }

    public void EndStroke(in InkSample sample)
    {
        if (Active is null)
            return;

        if (IsFinite(sample))
            Active.Add(sample, isLast: true);
        Commit();
    }

    /// <summary>Ends the stroke being drawn without a final sample. Its ink is kept: the user saw it being drawn.</summary>
    public void CancelStroke()
    {
        if (Active is not null)
            Commit();
    }

    /// <summary>Removes the last stroke, or brings back what <see cref="Clear"/> removed.</summary>
    public bool Undo() => Step(_undo, _redo, undo: true);

    /// <summary>Applies again what <see cref="Undo"/> took back.</summary>
    public bool Redo() => Step(_redo, _undo, undo: false);

    /// <summary>Removes every stroke. Undo brings them back.</summary>
    public void Clear()
    {
        CancelStroke();
        if (Current.IsEmpty)
            return;

        _undo.Push(new Cleared(Current.Strokes));
        _redo.Clear();
        Current = new SignatureData([], CanvasSize);
        Changed?.Invoke();
    }

    /// <summary>Replaces the signature, forgetting the history and any stroke being drawn.</summary>
    public void Load(SignatureData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        Active = null;
        _undo.Clear();
        _redo.Clear();
        _timeOrigin = null;
        Current = data;
        Changed?.Invoke();
    }

    private bool Step(Stack<HistoryEntry> from, Stack<HistoryEntry> to, bool undo)
    {
        CancelStroke();
        if (from.Count == 0)
            return false;

        var entry = from.Pop();
        Current = undo ? entry.Undo(Current, CanvasSize) : entry.Redo(Current, CanvasSize);
        to.Push(entry);
        Changed?.Invoke();
        return true;
    }

    private void Commit()
    {
        var builder = Active!;
        Active = null;
        builder.Complete();

        var stroke = builder.ToStroke(_activeColor);
        Current = AddedStroke.Append(Current, stroke, CanvasSize);
        _undo.Push(new AddedStroke(stroke));
        _redo.Clear();
        StrokeCompleted?.Invoke(stroke);
        Changed?.Invoke();
    }

    /// <summary>
    /// The platform timestamp at which signature time is 0. Within a session it stays fixed, so the pauses between
    /// strokes are kept. A new signature starts at 0; otherwise a stroke never starts before the end of the ink already
    /// there, which keeps time increasing after loading data or undoing a clear.
    /// </summary>
    private double TimeOriginFor(double timestamp)
    {
        if (Current.IsEmpty)
        {
            _timeOrigin = timestamp;
            return timestamp;
        }

        var lastTime = Current.LastTime;
        if (_timeOrigin is not { } origin || timestamp - origin < lastTime)
            _timeOrigin = origin = timestamp - lastTime;
        return origin;
    }

    private static bool IsFinite(in InkSample sample) =>
        float.IsFinite(sample.X) && float.IsFinite(sample.Y) && double.IsFinite(sample.TimestampMs);

    private abstract class HistoryEntry
    {
        public abstract SignatureData Undo(SignatureData current, SizeF canvasSize);

        public abstract SignatureData Redo(SignatureData current, SizeF canvasSize);
    }

    private sealed class AddedStroke(SignatureStroke stroke) : HistoryEntry
    {
        public static SignatureData Append(SignatureData current, SignatureStroke stroke, SizeF canvasSize)
        {
            var strokes = new SignatureStroke[current.Strokes.Count + 1];
            for (var i = 0; i < current.Strokes.Count; i++)
                strokes[i] = current.Strokes[i];
            strokes[^1] = stroke;
            return new SignatureData(strokes, canvasSize);
        }

        public override SignatureData Undo(SignatureData current, SizeF canvasSize)
        {
            var strokes = new SignatureStroke[current.Strokes.Count - 1];
            for (var i = 0; i < strokes.Length; i++)
                strokes[i] = current.Strokes[i];
            return new SignatureData(strokes, canvasSize);
        }

        public override SignatureData Redo(SignatureData current, SizeF canvasSize) => Append(current, stroke, canvasSize);
    }

    private sealed class Cleared(IReadOnlyList<SignatureStroke> strokes) : HistoryEntry
    {
        public override SignatureData Undo(SignatureData current, SizeF canvasSize) => new(strokes, canvasSize);

        public override SignatureData Redo(SignatureData current, SizeF canvasSize) => new([], canvasSize);
    }
}
