using System.Collections.ObjectModel;

namespace Plugin.Maui.SignaturePad;

/// <summary>
/// A signature: its strokes and the size of the pad it was drawn on. Instances are immutable, so they can be shared,
/// stored and compared freely.
/// </summary>
public sealed class SignatureData : IEquatable<SignatureData>
{
    private readonly SignatureStroke[] _strokes;

    // Boxed so that assigning it is atomic; see SignatureStroke.
    private object? _bounds;

    /// <summary>A signature without strokes.</summary>
    public static SignatureData Empty { get; } = new([], SizeF.Zero);

    /// <summary>
    /// Creates a signature.
    /// </summary>
    /// <param name="strokes">The strokes in the order they were drawn.</param>
    /// <param name="canvasSize">The size of the pad, in device-independent units, when the signature was drawn.</param>
    /// <exception cref="ArgumentNullException"><paramref name="strokes"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="strokes"/> contains <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The width or height of <paramref name="canvasSize"/> is negative or not finite.</exception>
    public SignatureData(IEnumerable<SignatureStroke> strokes, SizeF canvasSize)
    {
        ArgumentNullException.ThrowIfNull(strokes);

        _strokes = [.. strokes];
        if (Array.IndexOf(_strokes, null) >= 0)
            throw new ArgumentException("The strokes must not contain null.", nameof(strokes));
        if (!float.IsFinite(canvasSize.Width) || !float.IsFinite(canvasSize.Height) || canvasSize.Width < 0 || canvasSize.Height < 0)
            throw new ArgumentOutOfRangeException(nameof(canvasSize), canvasSize, "The canvas size must be finite and not negative.");

        Strokes = new ReadOnlyCollection<SignatureStroke>(_strokes);
        CanvasSize = canvasSize;

        var start = double.MaxValue;
        var end = double.MinValue;
        foreach (var stroke in _strokes)
        {
            PointCount += stroke.Points.Count;
            start = Math.Min(start, stroke.StartTime);
            end = Math.Max(end, stroke.EndTime);
        }

        Duration = _strokes.Length == 0 ? 0 : end - start;
    }

    /// <summary>The strokes in the order they were drawn.</summary>
    public IReadOnlyList<SignatureStroke> Strokes { get; }

    /// <summary>The size of the pad, in device-independent units, when the signature was drawn.</summary>
    public SizeF CanvasSize { get; }

    /// <summary>Whether the signature has no strokes.</summary>
    public bool IsEmpty => _strokes.Length == 0;

    /// <summary>The number of points in all strokes.</summary>
    public int PointCount { get; }

    /// <summary>The time from the start of the first stroke to the end of the last one, in milliseconds.</summary>
    public double Duration { get; }

    /// <summary>
    /// The area the ink of all strokes covers, in device-independent units, or an empty rectangle at the origin when
    /// there are no strokes.
    /// </summary>
    public RectF Bounds => (RectF)(_bounds ??= ComputeBounds());

    /// <summary>
    /// Writes the signature as JSON. The text keeps every point exactly, so <see cref="FromJson"/> gives back an equal
    /// signature, and it is versioned, so later versions of the package can still read it.
    /// </summary>
    /// <returns>The JSON text.</returns>
    public string ToJson() => SignatureJson.Write(this);

    /// <summary>
    /// Reads a signature from the JSON that <see cref="ToJson"/> writes. The input is validated completely before
    /// anything is returned, so it is safe to call with untrusted text.
    /// </summary>
    /// <param name="json">The JSON text.</param>
    /// <returns>The signature.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">
    /// The text is not valid JSON, is not signature data, has an unsupported version, has missing or invalid values,
    /// or has more than 10,000 strokes or 100,000 points. The message says where the problem is.
    /// </exception>
    public static SignatureData FromJson(string json) => SignatureJson.Read(json);

    /// <summary>
    /// Writes the signature as SVG, with one filled path per stroke that has exactly the shape of the ink on screen.
    /// </summary>
    /// <param name="options">The export options, or <see langword="null"/> for the defaults.</param>
    /// <returns>The SVG document, without an XML declaration.</returns>
    /// <exception cref="InvalidOperationException">The signature is empty.</exception>
    public string ToSvg(SignatureSvgExportOptions? options = null) => SvgExporter.Export(this, options ?? new SignatureSvgExportOptions());

    /// <inheritdoc/>
    public bool Equals(SignatureData? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        return other is not null
            && CanvasSize.Equals(other.CanvasSize)
            && _strokes.AsSpan().SequenceEqual(other._strokes);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as SignatureData);

    private RectF ComputeBounds()
    {
        if (_strokes.Length == 0)
            return RectF.Zero;

        var bounds = _strokes[0].Bounds;
        for (var i = 1; i < _strokes.Length; i++)
            bounds = RectF.Union(bounds, _strokes[i].Bounds);
        return bounds;
    }

    /// <inheritdoc/>
    public override int GetHashCode() =>
        _strokes.Length == 0
            ? HashCode.Combine(CanvasSize)
            : HashCode.Combine(CanvasSize, _strokes.Length, _strokes[0], _strokes[^1]);
}
