using System.Runtime.CompilerServices;
using System.Windows.Input;
using Plugin.Maui.SignaturePad.Capture;

namespace Plugin.Maui.SignaturePad;

/// <summary>
/// A pad to sign on with a finger, pen or mouse. The ink gets thinner when drawn fast and thicker when drawn slowly or
/// pressed harder with a pen, and every stroke keeps its points with their timing and pressure.
/// </summary>
/// <remarks>
/// Bind <see cref="Signature"/> to get and set the signature as data, use <see cref="IsEmpty"/> to validate a form, and
/// bind buttons to <see cref="UndoCommand"/>, <see cref="RedoCommand"/> and <see cref="ClearCommand"/>. Export with
/// <see cref="ExportImageAsync"/>, <see cref="ExportSvg"/> or <see cref="SignatureData.ToJson"/>. Register the control
/// with <c>UseSignaturePad()</c> in <c>MauiProgram</c>.
/// </remarks>
public partial class SignaturePadView : View
{
    /// <summary>Bindable property for <see cref="StrokeColor"/>.</summary>
    public static readonly BindableProperty StrokeColorProperty =
        BindableProperty.Create(nameof(StrokeColor), typeof(Color), typeof(SignaturePadView), Colors.Black);

    // MAUI ignores (and logs) values that fail validation. Neither width is coerced against the other, so the result
    // does not depend on the order the properties are set in; strokes use the larger of the two as their widest ink.

    /// <summary>Bindable property for <see cref="MinStrokeWidth"/>.</summary>
    public static readonly BindableProperty MinStrokeWidthProperty = BindableProperty.Create(
        nameof(MinStrokeWidth), typeof(double), typeof(SignaturePadView), 1.0,
        validateValue: static (_, value) => double.IsFinite((double)value),
        coerceValue: static (_, value) => Math.Max(MinimumWidth, (double)value));

    /// <summary>Bindable property for <see cref="MaxStrokeWidth"/>.</summary>
    public static readonly BindableProperty MaxStrokeWidthProperty = BindableProperty.Create(
        nameof(MaxStrokeWidth), typeof(double), typeof(SignaturePadView), 3.5,
        validateValue: static (_, value) => double.IsFinite((double)value),
        coerceValue: static (_, value) => Math.Max(MinimumWidth, (double)value));

    private const double MinimumWidth = 0.1;

    /// <summary>Bindable property for <see cref="Placeholder"/>.</summary>
    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(SignaturePadView));

    /// <summary>Bindable property for <see cref="PlaceholderColor"/>.</summary>
    public static readonly BindableProperty PlaceholderColorProperty =
        BindableProperty.Create(nameof(PlaceholderColor), typeof(Color), typeof(SignaturePadView), Color.FromArgb("#9E9E9E"));

    /// <summary>Bindable property for <see cref="ShowSignatureLine"/>.</summary>
    public static readonly BindableProperty ShowSignatureLineProperty =
        BindableProperty.Create(nameof(ShowSignatureLine), typeof(bool), typeof(SignaturePadView), true);

    /// <summary>Bindable property for <see cref="SignatureLineColor"/>.</summary>
    public static readonly BindableProperty SignatureLineColorProperty =
        BindableProperty.Create(nameof(SignatureLineColor), typeof(Color), typeof(SignaturePadView), Color.FromArgb("#BDBDBD"));

    /// <summary>Bindable property for <see cref="Signature"/>.</summary>
    public static readonly BindableProperty SignatureProperty = BindableProperty.Create(
        nameof(Signature), typeof(SignatureData), typeof(SignaturePadView), SignatureData.Empty, BindingMode.TwoWay,
        coerceValue: static (_, value) => value ?? SignatureData.Empty,
        propertyChanged: static (bindable, _, newValue) => ((SignaturePadView)bindable).OnSignatureChanged((SignatureData)newValue));

    /// <summary>Bindable property for <see cref="StrokeCompletedCommand"/>.</summary>
    public static readonly BindableProperty StrokeCompletedCommandProperty =
        BindableProperty.Create(nameof(StrokeCompletedCommand), typeof(ICommand), typeof(SignaturePadView));

    private static readonly BindablePropertyKey IsEmptyPropertyKey =
        BindableProperty.CreateReadOnly(nameof(IsEmpty), typeof(bool), typeof(SignaturePadView), true);

    private static readonly BindablePropertyKey CanUndoPropertyKey =
        BindableProperty.CreateReadOnly(nameof(CanUndo), typeof(bool), typeof(SignaturePadView), false);

    private static readonly BindablePropertyKey CanRedoPropertyKey =
        BindableProperty.CreateReadOnly(nameof(CanRedo), typeof(bool), typeof(SignaturePadView), false);

    /// <summary>Bindable property for <see cref="IsEmpty"/>.</summary>
    public static readonly BindableProperty IsEmptyProperty = IsEmptyPropertyKey.BindableProperty;

    /// <summary>Bindable property for <see cref="CanUndo"/>.</summary>
    public static readonly BindableProperty CanUndoProperty = CanUndoPropertyKey.BindableProperty;

    /// <summary>Bindable property for <see cref="CanRedo"/>.</summary>
    public static readonly BindableProperty CanRedoProperty = CanRedoPropertyKey.BindableProperty;

    /// <summary>Creates a pad.</summary>
    public SignaturePadView()
    {
        Session = new SignatureSession(() =>
            new InkStyle(StrokeColor, (float)MinStrokeWidth, (float)Math.Max(MinStrokeWidth, MaxStrokeWidth)));
        Session.Changed += OnSessionChanged;
        Session.ActiveStrokeChanged += OnActiveStrokeChanged;
        Session.StrokeStarted += OnStrokeStarted;
        Session.StrokeCompleted += OnStrokeCompleted;

        UndoCommand = new Command(() => Undo(), () => CanUndo);
        RedoCommand = new Command(() => Redo(), () => CanRedo);
        ClearCommand = new Command(Clear, () => !IsEmpty);
    }

    /// <summary>Raised when a stroke starts.</summary>
    public event EventHandler? StrokeStarted;

    /// <summary>Raised when a stroke is finished, after <see cref="Signature"/> includes it.</summary>
    public event EventHandler<SignatureStrokeEventArgs>? StrokeCompleted;

    /// <summary>Raised when the committed ink changed and the whole pad must be drawn again.</summary>
    internal event Action? InvalidateRequested;

    /// <summary>Raised when the stroke being drawn changed; only its area needs to be drawn again.</summary>
    internal event Action? ActiveStrokeUpdated;

    /// <summary>The color of new strokes. Strokes already drawn keep their color. The default is black.</summary>
    public Color StrokeColor
    {
        get => (Color)GetValue(StrokeColorProperty);
        set => SetValue(StrokeColorProperty, value);
    }

    /// <summary>
    /// The narrowest ink of new strokes, in device-independent units, reached when drawing fast or pressing lightly
    /// with a pen. The default is 1; values below 0.1 become 0.1, and values that are not finite are ignored.
    /// </summary>
    public double MinStrokeWidth
    {
        get => (double)GetValue(MinStrokeWidthProperty);
        set => SetValue(MinStrokeWidthProperty, value);
    }

    /// <summary>
    /// The widest ink of new strokes, in device-independent units, reached when drawing slowly or pressing firmly
    /// with a pen. The default is 3.5; values below 0.1 become 0.1, and values that are not finite are ignored. When it
    /// is less than <see cref="MinStrokeWidth"/>, strokes have the width of <see cref="MinStrokeWidth"/>.
    /// </summary>
    public double MaxStrokeWidth
    {
        get => (double)GetValue(MaxStrokeWidthProperty);
        set => SetValue(MaxStrokeWidthProperty, value);
    }

    /// <summary>The text shown on an empty pad, such as "Sign here". The default is none.</summary>
    public string? Placeholder
    {
        get => (string?)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>The color of <see cref="Placeholder"/>. The default is a medium gray.</summary>
    public Color PlaceholderColor
    {
        get => (Color)GetValue(PlaceholderColorProperty);
        set => SetValue(PlaceholderColorProperty, value);
    }

    /// <summary>Whether a line to sign on is drawn across the pad. It is never exported. The default is true.</summary>
    public bool ShowSignatureLine
    {
        get => (bool)GetValue(ShowSignatureLineProperty);
        set => SetValue(ShowSignatureLineProperty, value);
    }

    /// <summary>The color of the signature line. The default is a light gray.</summary>
    public Color SignatureLineColor
    {
        get => (Color)GetValue(SignatureLineColorProperty);
        set => SetValue(SignatureLineColorProperty, value);
    }

    /// <summary>
    /// The signature on the pad, updated after every stroke, undo, redo and clear. Setting it shows that signature
    /// and forgets the undo history; <see langword="null"/> clears the pad. It binds two-way by default.
    /// </summary>
    public SignatureData Signature
    {
        get => (SignatureData)GetValue(SignatureProperty);
        set => SetValue(SignatureProperty, value);
    }

    /// <summary>Executed with the <see cref="SignatureStroke"/> when a stroke is finished, if it can execute.</summary>
    public ICommand? StrokeCompletedCommand
    {
        get => (ICommand?)GetValue(StrokeCompletedCommandProperty);
        set => SetValue(StrokeCompletedCommandProperty, value);
    }

    /// <summary>Whether the pad has no strokes, for example to disable a submit button.</summary>
    public bool IsEmpty => (bool)GetValue(IsEmptyProperty);

    /// <summary>Whether <see cref="Undo"/> has something to undo.</summary>
    public bool CanUndo => (bool)GetValue(CanUndoProperty);

    /// <summary>Whether <see cref="Redo"/> has something to redo.</summary>
    public bool CanRedo => (bool)GetValue(CanRedoProperty);

    /// <summary>The pad's state; it outlives the handler, so the ink survives the native view being recreated.</summary>
    internal SignatureSession Session { get; }

    /// <inheritdoc/>
    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == IsEnabledProperty.PropertyName)
            Session.IsInputEnabled = IsEnabled;
    }

    /// <inheritdoc/>
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        Session.CanvasSize = new SizeF((float)Math.Max(0, width), (float)Math.Max(0, height));
    }

    private void OnSignatureChanged(SignatureData signature)
    {
        // The pad sets this property itself after every change; only data from outside needs loading.
        if (!ReferenceEquals(signature, Session.Current))
            Session.Load(signature);
    }

    private void OnSessionChanged()
    {
        // SetValueFromRenderer, unlike SetValue, keeps a OneWay binding on Signature working.
        if (!ReferenceEquals(Signature, Session.Current))
            ((IElementController)this).SetValueFromRenderer(SignatureProperty, Session.Current);

        SetValue(IsEmptyPropertyKey, Session.Current.IsEmpty);
        SetValue(CanUndoPropertyKey, Session.CanUndo);
        SetValue(CanRedoPropertyKey, Session.CanRedo);
        ((Command)UndoCommand).ChangeCanExecute();
        ((Command)RedoCommand).ChangeCanExecute();
        ((Command)ClearCommand).ChangeCanExecute();
        InvalidateRequested?.Invoke();
    }

    private void OnActiveStrokeChanged() => ActiveStrokeUpdated?.Invoke();

    private void OnStrokeStarted()
    {
        // The first stroke on an empty pad hides the placeholder, which is outside the stroke's own area.
        if (Session.Current.IsEmpty)
            InvalidateRequested?.Invoke();

        StrokeStarted?.Invoke(this, EventArgs.Empty);
    }

    private void OnStrokeCompleted(SignatureStroke stroke)
    {
        StrokeCompleted?.Invoke(this, new SignatureStrokeEventArgs(stroke));

        if (StrokeCompletedCommand is { } command && command.CanExecute(stroke))
            command.Execute(stroke);
    }
}
