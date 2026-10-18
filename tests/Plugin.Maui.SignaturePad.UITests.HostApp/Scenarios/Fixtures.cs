namespace Plugin.Maui.SignaturePad.UITests.HostApp.Scenarios;

/// <summary>
/// Signatures built from fixed numbers, so their screenshots are the same on every run: unlike a drawn gesture, they do
/// not depend on how fast the test driver moves the pointer.
/// </summary>
public static class Fixtures
{
    private static readonly SizeF Canvas = new((float)ScenarioCatalog.PadWidth, (float)ScenarioCatalog.PadHeight);

    /// <summary>A pen wave whose pressure rises and falls, drawn at a steady speed.</summary>
    public static SignatureData PenPressure()
    {
        var points = new List<SignaturePoint>();
        for (var i = 0; i <= 120; i++)
        {
            var x = 20 + (i * 2.3f);
            var y = 100 + (35 * MathF.Sin(i / 12f));
            var pressure = 0.15f + (0.8f * MathF.Sin(i / 120f * MathF.PI));
            points.Add(new SignaturePoint(x, y, i * 8, MathF.Round(pressure, 3)));
        }

        return new SignatureData([new SignatureStroke(points, Colors.Black, 1, 4, SignatureInputDevice.Pen)], Canvas);
    }

    /// <summary>A finger stroke that starts slow, speeds up in the middle and slows down again.</summary>
    public static SignatureData Velocity()
    {
        var points = new List<SignaturePoint>();
        double time = 0;
        for (var i = 0; i <= 100; i++)
        {
            var x = 20 + (i * 2.8f);
            var y = 100 + (30 * MathF.Cos(i / 9f));

            // 1 ms per sample in the middle, 20 ms at the ends: ten times the speed.
            time += 1 + (19 * Math.Pow(Math.Cos(i / 100.0 * Math.PI), 2));
            points.Add(new SignaturePoint(x, y, Math.Round(time, 2)));
        }

        return new SignatureData([new SignatureStroke(points, Colors.Black, 1, 3.5f, SignatureInputDevice.Touch)], Canvas);
    }

    /// <summary>
    /// A heavy signature for the benchmark: 50 strokes of 300 points each, far more than a real signature, so drawing
    /// costs that grow with the number of strokes or points show up clearly.
    /// </summary>
    public static SignatureData Heavy()
    {
        var strokes = new List<SignatureStroke>();
        for (var s = 0; s < 50; s++)
        {
            var points = new SignaturePoint[300];
            for (var i = 0; i < points.Length; i++)
            {
                var x = 10 + (i * 1f);
                var y = 20 + (s * 3.2f) + (12 * MathF.Sin((i / 15f) + s));
                points[i] = new SignaturePoint(x, y, (s * 3000) + (i * 6));
            }

            strokes.Add(new SignatureStroke(points, Colors.Black, 1, 3.5f, SignatureInputDevice.Touch));
        }

        return new SignatureData(strokes, Canvas);
    }

    /// <summary>Dots, a loop and a sharp turn: the shapes an outline handles specially.</summary>
    public static SignatureData DotsAndLoops()
    {
        SignatureStroke Dot(float x, float y, double time) =>
            new([new SignaturePoint(x, y, time)], Colors.Black, 1, 3.5f, SignatureInputDevice.Touch);

        var loop = new List<SignaturePoint>();
        for (var i = 0; i <= 80; i++)
        {
            var angle = i / 80f * MathF.PI * 2.2f;
            loop.Add(new SignaturePoint(110 + (60 * MathF.Cos(angle)) + (i * 0.6f), 90 + (45 * MathF.Sin(angle)), 200 + (i * 6)));
        }

        var turn = new List<SignaturePoint>();
        for (var i = 0; i <= 40; i++)
            turn.Add(new SignaturePoint(230 + (i < 20 ? i * 3 : (40 - i) * 3), 40 + (i * 3), 800 + (i * 8)));

        return new SignatureData(
            [
                Dot(30, 40, 0),
                Dot(45, 40, 100),
                new SignatureStroke(loop, Color.FromArgb("#1E88E5"), 1, 3.5f, SignatureInputDevice.Touch),
                new SignatureStroke(turn, Colors.Black, 1, 3.5f, SignatureInputDevice.Mouse),
            ],
            Canvas);
    }
}
