namespace Plugin.Maui.SignaturePad.Ink;

/// <summary>
/// Rounds stored sample values to a precision well below what input hardware resolves. The JSON stays short, and
/// because the ink is computed from the rounded values, a restored signature renders exactly like the live one.
/// </summary>
internal static class Quantize
{
    /// <summary>Rounds to 0.01 DIP.</summary>
    public static float Coordinate(float value) => MathF.Round(value * 100f) / 100f;

    /// <summary>Rounds to 0.01 ms.</summary>
    public static double Time(double value) => Math.Round(value * 100.0) / 100.0;

    /// <summary>Rounds to 0.001.</summary>
    public static float Pressure(float value) => MathF.Round(value * 1000f) / 1000f;
}
