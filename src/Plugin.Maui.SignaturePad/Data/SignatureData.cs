using System.Collections.ObjectModel;

namespace Plugin.Maui.SignaturePad;

/// <summary>
/// A signature: its strokes and the size of the pad it was drawn on. Instances are immutable, so they can be shared,
/// stored and compared freely.
/// </summary>
public sealed class SignatureData : IEquatable<SignatureData>
{
    private readonly SignatureStroke[] _strokes;

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

    /// <inheritdoc/>
    public override int GetHashCode() =>
        _strokes.Length == 0
            ? HashCode.Combine(CanvasSize)
            : HashCode.Combine(CanvasSize, _strokes.Length, _strokes[0], _strokes[^1]);
}
