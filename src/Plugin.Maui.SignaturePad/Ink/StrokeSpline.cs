using System.Numerics;

namespace Plugin.Maui.SignaturePad.Ink;

/// <summary>
/// Fills the gaps between far apart points of a stroke's centre line with points on a curve through them. A fast
/// stroke, or a device that reports few samples, then draws round ink instead of a chain of straight segments.
/// </summary>
/// <remarks>
/// The curve is a centripetal Catmull-Rom spline (Yuksel, Schaefer and Keyser, "Parameterization and applications of
/// Catmull-Rom curves", 2011). It passes through every point and, unlike the uniform spline, never forms a cusp or a
/// loop within a segment. At the two ends, a point mirrored across the end continues the stroke straight. The radius
/// changes linearly along each segment.
/// </remarks>
internal static class StrokeSpline
{
    /// <summary>Segments longer than this, in DIP, get points in between, at most this far apart.</summary>
    public const float MaxStep = 2f;

    /// <summary>The most steps one segment is split into, which bounds the work for a jump across the pad.</summary>
    public const int MaxStepsPerSegment = 64;

    // Neighbours closer than this, in DIP, cannot give the curve a direction.
    private const float MinNeighbourDistance = 0.01f;

    /// <summary>The points with the curve between them filled in, or <paramref name="points"/> itself when no segment is too long.</summary>
    public static ReadOnlySpan<InkPoint> Densify(ReadOnlySpan<InkPoint> points)
    {
        var added = 0;
        for (var i = 1; i < points.Length; i++)
            added += Steps(points[i - 1], points[i]) - 1;
        if (added == 0)
            return points;

        var dense = new InkPoint[points.Length + added];
        var count = 0;
        for (var i = 0; i < points.Length - 1; i++)
        {
            var start = points[i];
            var end = points[i + 1];
            dense[count++] = start;

            var steps = Steps(start, end);
            if (steps == 1)
                continue;

            var p1 = Position(start);
            var p2 = Position(end);
            var p0 = Neighbour(points, i - 1, p1, p2);
            var p3 = Neighbour(points, i + 2, p2, p1);

            // The tangents at the segment's ends for the centripetal parameterisation (knots spaced by the square root
            // of the distance), scaled to the segment, turn it into a cubic Hermite curve.
            var d01 = MathF.Sqrt(Vector2.Distance(p0, p1));
            var d12 = MathF.Sqrt(Vector2.Distance(p1, p2));
            var d23 = MathF.Sqrt(Vector2.Distance(p2, p3));
            var m1 = d12 * (((p1 - p0) / d01) - ((p2 - p0) / (d01 + d12)) + ((p2 - p1) / d12));
            var m2 = d12 * (((p2 - p1) / d12) - ((p3 - p1) / (d12 + d23)) + ((p3 - p2) / d23));

            for (var k = 1; k < steps; k++)
            {
                var t = (float)k / steps;
                var t2 = t * t;
                var t3 = t2 * t;
                var position = (((2 * t3) - (3 * t2) + 1) * p1) + ((t3 - (2 * t2) + t) * m1) + (((-2 * t3) + (3 * t2)) * p2) + ((t3 - t2) * m2);
                dense[count++] = new InkPoint(position.X, position.Y, start.Radius + ((end.Radius - start.Radius) * t));
            }
        }

        dense[count] = points[^1];
        return dense;
    }

    private static int Steps(in InkPoint from, in InkPoint to) =>
        Math.Clamp((int)MathF.Ceiling(Vector2.Distance(Position(from), Position(to)) / MaxStep), 1, MaxStepsPerSegment);

    // The point before or after a segment, or the segment's own end mirrored across it where there is no such point or
    // it lies on the end (a pen pressing harder without moving repeats its point).
    private static Vector2 Neighbour(ReadOnlySpan<InkPoint> points, int index, Vector2 end, Vector2 otherEnd)
    {
        if (index >= 0 && index < points.Length)
        {
            var neighbour = Position(points[index]);
            if (Vector2.Distance(neighbour, end) >= MinNeighbourDistance)
                return neighbour;
        }

        return (2 * end) - otherEnd;
    }

    private static Vector2 Position(in InkPoint point) => new(point.X, point.Y);
}
