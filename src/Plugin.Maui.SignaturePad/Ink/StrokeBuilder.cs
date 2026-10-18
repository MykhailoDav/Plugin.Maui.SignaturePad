namespace Plugin.Maui.SignaturePad.Ink;

/// <summary>
/// Builds one stroke from input samples as they arrive. It keeps the samples that matter (rounded, as they will be
/// stored) and, for each of them, the smoothed point and ink width to draw.
/// </summary>
/// <remarks>
/// The width follows the velocity measured from real input timestamps, smoothed by an exponential filter, and the pen
/// pressure when there is one. The drawn line trails the input a little (streamline) to remove jitter, and a completed
/// stroke ends exactly where the input ended. Everything is computed from the stored values, in input order, so
/// <see cref="ComputeRenderPoints"/> reproduces a stored stroke bit for bit. Adding a sample is O(1) amortized and does
/// not allocate apart from growing the buffers.
/// </remarks>
internal sealed class StrokeBuilder
{
    private const float StreamlineFactor = 0.15f + ((1f - InkDefaults.Streamline) * 0.85f);
    private const float MinSampleDistanceSquared = InkDefaults.MinSampleDistance * InkDefaults.MinSampleDistance;

    private readonly double _timeOrigin;
    private SignaturePoint[] _points = new SignaturePoint[64];
    private InkPoint[] _render = new InkPoint[64];
    private float _filteredVelocity;
    private float _smoothX;
    private float _smoothY;

    /// <summary>Starts a stroke at its first sample.</summary>
    /// <param name="first">The first sample; its device becomes the device of the stroke.</param>
    /// <param name="minWidth">The narrowest ink width in DIP.</param>
    /// <param name="maxWidth">The widest ink width in DIP.</param>
    /// <param name="timeOrigin">The platform timestamp that becomes time 0 of the signature.</param>
    public StrokeBuilder(in InkSample first, float minWidth, float maxWidth, double timeOrigin)
        : this(minWidth, maxWidth, first.Device)
    {
        _timeOrigin = timeOrigin;
        AppendPoint(ToPoint(first), isLast: false);
    }

    private StrokeBuilder(float minWidth, float maxWidth, SignatureInputDevice device)
    {
        MinWidth = minWidth;
        MaxWidth = maxWidth;
        Device = device;
    }

    public float MinWidth { get; }

    public float MaxWidth { get; }

    public SignatureInputDevice Device { get; }

    /// <summary>Whether the input ended; a completed stroke takes no more samples.</summary>
    public bool IsCompleted { get; private set; }

    /// <summary>The number of kept samples.</summary>
    public int Count { get; private set; }

    /// <summary>The kept samples, rounded and timed as they will be stored.</summary>
    public ReadOnlySpan<SignaturePoint> Points => _points.AsSpan(0, Count);

    /// <summary>The smoothed centre line with the ink radius at each point, one per kept sample.</summary>
    public ReadOnlySpan<InkPoint> RenderPoints => _render.AsSpan(0, Count);

    /// <summary>Adds a sample.</summary>
    /// <param name="sample">The sample.</param>
    /// <param name="isLast">Whether the input ends with this sample, which completes the stroke.</param>
    /// <exception cref="InvalidOperationException">The stroke is already completed.</exception>
    public void Add(in InkSample sample, bool isLast = false)
    {
        if (IsCompleted)
            throw new InvalidOperationException("The stroke is already completed.");

        var point = ToPoint(sample);
        var last = _points[Count - 1];
        var keep = isLast ? point != last : IsWorthKeeping(last, point);
        if (keep)
            AppendPoint(point, isLast);
        if (isLast)
            Complete();
    }

    /// <summary>
    /// Ends the stroke without another sample (for example when the platform cancels the input). The drawn line is
    /// snapped to the last kept sample, exactly as replaying the stored stroke does.
    /// </summary>
    public void Complete()
    {
        if (IsCompleted)
            return;

        IsCompleted = true;
        var last = _points[Count - 1];
        _render[Count - 1] = _render[Count - 1] with { X = last.X, Y = last.Y };
    }

    /// <summary>Creates the immutable stroke from the kept samples.</summary>
    public SignatureStroke ToStroke(Color color) =>
        new(new ArraySegment<SignaturePoint>(_points, 0, Count), color, MinWidth, MaxWidth, Device);

    /// <summary>Computes the drawn centre line of a stored stroke, exactly as it was computed while it was drawn.</summary>
    public static InkPoint[] ComputeRenderPoints(SignatureStroke stroke)
    {
        var builder = new StrokeBuilder(stroke.MinWidth, stroke.MaxWidth, stroke.Device);
        var points = stroke.PointSpan;
        for (var i = 0; i < points.Length; i++)
            builder.AppendPoint(points[i], isLast: i == points.Length - 1);

        return builder.RenderPoints.ToArray();
    }

    private SignaturePoint ToPoint(in InkSample sample)
    {
        // Only pens report real pressure; a finger's "pressure" is its contact size and would make the ink jump.
        float? pressure = Device == SignatureInputDevice.Pen && float.IsFinite(sample.Pressure)
            ? Quantize.Pressure(Math.Clamp(sample.Pressure, 0f, 1f))
            : null;

        return new SignaturePoint(
            Quantize.Coordinate(sample.X),
            Quantize.Coordinate(sample.Y),
            Quantize.Time(sample.TimestampMs - _timeOrigin),
            pressure);
    }

    private static bool IsWorthKeeping(SignaturePoint last, SignaturePoint point)
    {
        var dx = point.X - last.X;
        var dy = point.Y - last.Y;
        if ((dx * dx) + (dy * dy) >= MinSampleDistanceSquared)
            return true;

        // A pen pressing harder in place changes the ink without moving.
        return point.Pressure is { } pressure && last.Pressure is { } lastPressure
            && MathF.Abs(pressure - lastPressure) >= InkDefaults.MinPressureChange;
    }

    private void AppendPoint(SignaturePoint point, bool isLast)
    {
        if (Count == _points.Length)
        {
            Array.Resize(ref _points, _points.Length * 2);
            Array.Resize(ref _render, _render.Length * 2);
        }

        if (Count == 0)
        {
            // A stroke starts from the reference velocity, which puts the first point halfway between the extreme
            // widths: starting at rest would give every stroke a blob at its start.
            _filteredVelocity = InkDefaults.ReferenceVelocity;
            _smoothX = point.X;
            _smoothY = point.Y;
        }
        else
        {
            var previous = _points[Count - 1];
            var dx = point.X - previous.X;
            var dy = point.Y - previous.Y;
            var step = MathF.Sqrt((dx * dx) + (dy * dy));
            var deltaTime = Math.Max(point.Time - previous.Time, InkDefaults.MinDeltaTime);
            var velocity = (float)(step / deltaTime);
            _filteredVelocity = (InkDefaults.VelocityFilterWeight * velocity)
                + ((1f - InkDefaults.VelocityFilterWeight) * _filteredVelocity);

            // Streamlining removes the jitter of dense samples. A long step is movement rather than jitter, and
            // smoothing it would only make the ink lag and cut its curves, so the longer the step, the closer the line
            // follows it.
            var movement = Math.Clamp((step - InkDefaults.JitterStep) / (InkDefaults.MovementStep - InkDefaults.JitterStep), 0f, 1f);
            var follow = StreamlineFactor + ((1f - StreamlineFactor) * movement);
            _smoothX += (point.X - _smoothX) * follow;
            _smoothY += (point.Y - _smoothY) * follow;
        }

        var velocityWeight = 1f / (1f + (_filteredVelocity / InkDefaults.ReferenceVelocity));
        float width;
        if (point.Pressure is { } pressure)
        {
            // A pen's pressure sets the width, and speed thins it a little.
            var weight = pressure * (1f - InkDefaults.PenVelocityInfluence + (InkDefaults.PenVelocityInfluence * velocityWeight));
            width = MinWidth + ((MaxWidth - MinWidth) * weight);
        }
        else
        {
            // Without pressure, speed alone thins the ink, as a pen's does: the widest at rest, half that at the
            // reference velocity, and the narrowest from (MaxWidth / MinWidth − 1) times it on.
            width = MathF.Max(MinWidth, MaxWidth * velocityWeight);
        }

        _points[Count] = point;
        _render[Count] = isLast ? new InkPoint(point.X, point.Y, width / 2f) : new InkPoint(_smoothX, _smoothY, width / 2f);
        Count++;
    }
}
