using System.Collections.ObjectModel;

namespace Plugin.Maui.SignaturePad;

/// <summary>
/// One continuous stroke of a signature: the input samples from the moment the finger, pen or mouse touched the pad
/// until it lifted, and the ink it was drawn with. Instances are immutable.
/// </summary>
public sealed class SignatureStroke : IEquatable<SignatureStroke>
{
    private readonly SignaturePoint[] _points;

    /// <summary>
    /// Creates a stroke.
    /// </summary>
    /// <param name="points">The input samples in the order they were drawn. There must be at least one.</param>
    /// <param name="color">The ink color.</param>
    /// <param name="minWidth">The narrowest ink width, in device-independent units, reached by fast movement or light pressure.</param>
    /// <param name="maxWidth">The widest ink width, in device-independent units, reached by slow movement or firm pressure.</param>
    /// <param name="device">The kind of device that drew the stroke.</param>
    /// <exception cref="ArgumentNullException"><paramref name="points"/> or <paramref name="color"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="points"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A point has a coordinate or time that is not a finite number, or a pressure outside 0 to 1; a width is not a
    /// positive finite number; <paramref name="maxWidth"/> is less than <paramref name="minWidth"/>; or
    /// <paramref name="device"/> is not a defined value.
    /// </exception>
    public SignatureStroke(IEnumerable<SignaturePoint> points, Color color, float minWidth, float maxWidth, SignatureInputDevice device)
    {
        ArgumentNullException.ThrowIfNull(points);
        ArgumentNullException.ThrowIfNull(color);

        _points = [.. points];
        if (_points.Length == 0)
            throw new ArgumentException("A stroke needs at least one point.", nameof(points));

        for (var i = 0; i < _points.Length; i++)
        {
            var point = _points[i];
            if (!float.IsFinite(point.X) || !float.IsFinite(point.Y) || !double.IsFinite(point.Time))
                throw new ArgumentOutOfRangeException(nameof(points), point, $"Point {i} has a coordinate or time that is not a finite number.");
            if (point.Pressure is { } pressure && !(pressure >= 0 && pressure <= 1))
                throw new ArgumentOutOfRangeException(nameof(points), point, $"Point {i} has a pressure outside 0 to 1.");
        }

        if (!float.IsFinite(minWidth) || minWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(minWidth), minWidth, "The minimum width must be a positive finite number.");
        if (!float.IsFinite(maxWidth) || maxWidth < minWidth)
            throw new ArgumentOutOfRangeException(nameof(maxWidth), maxWidth, "The maximum width must be a finite number no less than the minimum width.");
        if (!Enum.IsDefined(device))
            throw new ArgumentOutOfRangeException(nameof(device), device, "The device is not a defined value.");

        Points = new ReadOnlyCollection<SignaturePoint>(_points);
        Color = color;
        MinWidth = minWidth;
        MaxWidth = maxWidth;
        Device = device;
    }

    /// <summary>The input samples in the order they were drawn. There is always at least one.</summary>
    public IReadOnlyList<SignaturePoint> Points { get; }

    /// <summary>The ink color.</summary>
    public Color Color { get; }

    /// <summary>The narrowest ink width, in device-independent units.</summary>
    public float MinWidth { get; }

    /// <summary>The widest ink width, in device-independent units.</summary>
    public float MaxWidth { get; }

    /// <summary>The kind of device that drew the stroke.</summary>
    public SignatureInputDevice Device { get; }

    /// <summary>The time of the first point, in milliseconds since the first point of the signature.</summary>
    public double StartTime => _points[0].Time;

    /// <summary>The time of the last point, in milliseconds since the first point of the signature.</summary>
    public double EndTime => _points[^1].Time;

    /// <summary>The points without the read-only wrapper, for the engine's hot paths.</summary>
    internal ReadOnlySpan<SignaturePoint> PointSpan => _points;

    /// <inheritdoc/>
    public bool Equals(SignatureStroke? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        // Colors compare at 8 bits per channel, the precision of the JSON format, so a stroke equals its round trip.
        return other is not null
            && Device == other.Device
            && MinWidth.Equals(other.MinWidth)
            && MaxWidth.Equals(other.MaxWidth)
            && Color.ToInt() == other.Color.ToInt()
            && PointSpan.SequenceEqual(other.PointSpan);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as SignatureStroke);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        HashCode.Combine(_points.Length, _points[0], _points[^1], Color.ToInt(), MinWidth, MaxWidth, Device);
}
