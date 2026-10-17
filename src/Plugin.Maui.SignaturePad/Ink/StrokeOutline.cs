using System.Numerics;

namespace Plugin.Maui.SignaturePad.Ink;

/// <summary>
/// Turns the centre line of a stroke into the closed outline of its ink: left side forwards, a round cap at the end,
/// the right side backwards and a round cap at the start. Filling the outline draws the whole stroke in one go, with no
/// seams or overlaps where its segments meet, and the same outline becomes one SVG path.
/// </summary>
/// <remarks>
/// A port of <c>getStrokePoints</c> and <c>getStrokeOutlinePoints</c> from perfect-freehand 1.2 (MIT, Copyright (c)
/// 2021 Stephen Ruiz Ltd, https://github.com/steveruizok/perfect-freehand), adapted: the radius of every point comes
/// from <see cref="InkPoint.Radius"/> instead of simulated pressure, there are no tapers or flat caps, and the
/// streamline smoothing has already been applied by <see cref="StrokeBuilder"/>.
/// </remarks>
internal static class StrokeOutline
{
    public const int StartCapSegments = 13;
    public const int EndCapSegments = 29;
    public const int CornerCapSegments = 13;
    public const int DotSegments = StartCapSegments;

    // perfect-freehand adds 0.0001 to π because exact half turns make some renderers leave a hairline gap.
    private const float FixedPi = MathF.PI + 0.0001f;
    private const float EndNoiseThreshold = 3f;
    private const float MinRadius = 0.01f;
    private const float Smoothing = 0.5f;
    private const float DotExtent = 0.5f;

    public static PointF[] Build(ReadOnlySpan<InkPoint> input)
    {
        if (input.Length == 0)
            return [];

        var first = new Vector2(input[0].X, input[0].Y);
        var maxRadius = MinRadius;
        var extentSquared = 0f;
        foreach (ref readonly var point in input)
        {
            maxRadius = MathF.Max(maxRadius, point.Radius);
            extentSquared = MathF.Max(extentSquared, Vector2.DistanceSquared(first, new Vector2(point.X, point.Y)));
        }

        // A tap, or a stroke that barely moved, is a dot as wide as the stroke got.
        if (extentSquared <= DotExtent * DotExtent)
            return Dot(first, maxRadius);

        var size = 2 * maxRadius;
        var points = Prepare(input, size, out var count);
        return count == 1 ? Dot(first, maxRadius) : Outline(points.AsSpan(0, count), size);
    }

    private readonly record struct StrokePoint(Vector2 Point, float Radius, Vector2 Vector, float RunningLength);

    // getStrokePoints: drops repeated points and the noise right at the start, and works out each point's direction
    // (a unit vector pointing back to the previous point) and its distance along the stroke.
    private static StrokePoint[] Prepare(ReadOnlySpan<InkPoint> input, float size, out int count)
    {
        var points = new StrokePoint[input.Length];
        points[0] = new StrokePoint(new Vector2(input[0].X, input[0].Y), input[0].Radius, Vector2.Zero, 0);
        count = 1;

        var previous = points[0];
        var runningLength = 0f;
        var hasReachedMinimumLength = false;
        var max = input.Length - 1;
        for (var i = 1; i < input.Length; i++)
        {
            var point = new Vector2(input[i].X, input[i].Y);
            if (point == previous.Point)
                continue;

            runningLength += Vector2.Distance(point, previous.Point);

            // Until the stroke is as long as it is wide, intermediate points are mostly the jitter of putting the pen down.
            if (i < max && !hasReachedMinimumLength)
            {
                if (runningLength < size)
                    continue;
                hasReachedMinimumLength = true;
            }

            previous = new StrokePoint(point, input[i].Radius, Vector2.Normalize(previous.Point - point), runningLength);
            points[count++] = previous;
        }

        if (count > 1)
            points[0] = points[0] with { Vector = points[1].Vector };
        return points;
    }

    // getStrokeOutlinePoints.
    private static PointF[] Outline(ReadOnlySpan<StrokePoint> points, float size)
    {
        var left = new List<Vector2>(points.Length + CornerCapSegments + 1);
        var right = new List<Vector2>(points.Length + CornerCapSegments + 1);
        var totalLength = points[^1].RunningLength;
        var minDistance = MathF.Pow(size * Smoothing, 2);

        var previousVector = points[0].Vector;
        var previousLeft = points[0].Point;
        var previousRight = previousLeft;
        var isPreviousSharpCorner = false;
        var radius = MinRadius;

        for (var i = 0; i < points.Length; i++)
        {
            var current = points[i];
            var isLast = i == points.Length - 1;

            // The last few DIP before the end are usually the pen lifting off; the end cap covers them.
            if (!isLast && totalLength - current.RunningLength < EndNoiseThreshold)
                continue;

            radius = MathF.Max(MinRadius, current.Radius);

            var nextVector = isLast ? current.Vector : points[i + 1].Vector;
            var nextDot = isLast ? 1f : Vector2.Dot(current.Vector, nextVector);
            var previousDot = Vector2.Dot(current.Vector, previousVector);
            var isSharpCorner = previousDot < 0 && !isPreviousSharpCorner;
            var isNextSharpCorner = nextDot < 0;

            if (isSharpCorner || isNextSharpCorner)
            {
                // The stroke turns back by more than a right angle: offsetting the sides would cross them, so draw a
                // round cap around the corner instead.
                var offset = Per(previousVector) * radius;
                for (var k = 0; k <= CornerCapSegments; k++)
                {
                    var angle = FixedPi * k / CornerCapSegments;
                    previousLeft = RotateAround(current.Point - offset, current.Point, angle);
                    previousRight = RotateAround(current.Point + offset, current.Point, -angle);
                    left.Add(previousLeft);
                    right.Add(previousRight);
                }

                // perfect-freehand keeps the old direction here, which makes every following point look like another
                // corner when the turn was found from the previous direction; continuing from this point's direction
                // avoids that run of caps.
                previousVector = current.Vector;
                if (isNextSharpCorner)
                    isPreviousSharpCorner = true;
                continue;
            }

            isPreviousSharpCorner = false;

            if (isLast)
            {
                var offset = Per(current.Vector) * radius;
                left.Add(current.Point - offset);
                right.Add(current.Point + offset);
                continue;
            }

            // Offset along the average of this and the next direction, so the sides follow curves smoothly. Points
            // closer than the minimum distance to the last one on their side add nothing but noise.
            var sideOffset = Per(Vector2.Lerp(nextVector, current.Vector, nextDot)) * radius;
            var leftPoint = current.Point - sideOffset;
            if (i <= 1 || Vector2.DistanceSquared(previousLeft, leftPoint) > minDistance)
            {
                left.Add(leftPoint);
                previousLeft = leftPoint;
            }

            var rightPoint = current.Point + sideOffset;
            if (i <= 1 || Vector2.DistanceSquared(previousRight, rightPoint) > minDistance)
            {
                right.Add(rightPoint);
                previousRight = rightPoint;
            }

            previousVector = current.Vector;
        }

        var firstPoint = points[0].Point;
        var lastPoint = points[^1].Point;
        var outline = new PointF[left.Count + (EndCapSegments - 1) + right.Count + StartCapSegments];
        var index = 0;

        foreach (var point in left)
            outline[index++] = ToPointF(point);

        // The end cap turns one and a half times around the last point, which keeps it round even when the stroke
        // ends with a sharp turn.
        var endStart = lastPoint + (Per(-points[^1].Vector) * radius);
        for (var k = 1; k < EndCapSegments; k++)
            outline[index++] = ToPointF(RotateAround(endStart, lastPoint, FixedPi * 3 * k / EndCapSegments));

        for (var k = right.Count - 1; k >= 0; k--)
            outline[index++] = ToPointF(right[k]);

        for (var k = 1; k <= StartCapSegments; k++)
            outline[index++] = ToPointF(RotateAround(right[0], firstPoint, FixedPi * k / StartCapSegments));

        return outline;
    }

    private static PointF[] Dot(Vector2 center, float radius)
    {
        // Starts below and right of the centre, as perfect-freehand's drawDot does.
        var start = center - (Vector2.Normalize(Per(new Vector2(-1, -1))) * radius);
        var dot = new PointF[DotSegments];
        for (var k = 1; k <= DotSegments; k++)
            dot[k - 1] = ToPointF(RotateAround(start, center, FixedPi * 2 * k / DotSegments));
        return dot;
    }

    private static Vector2 Per(Vector2 vector) => new(vector.Y, -vector.X);

    private static Vector2 RotateAround(Vector2 point, Vector2 center, float angle)
    {
        var (sin, cos) = MathF.SinCos(angle);
        var offset = point - center;
        return new Vector2(
            center.X + (offset.X * cos) - (offset.Y * sin),
            center.Y + (offset.X * sin) + (offset.Y * cos));
    }

    private static PointF ToPointF(Vector2 point) => new(point.X, point.Y);
}
