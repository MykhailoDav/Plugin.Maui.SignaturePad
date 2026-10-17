namespace Plugin.Maui.SignaturePad.Ink;

/// <summary>
/// The constants of the ink model. They are internal so they can be tuned without an API change; the tests pin the
/// formulas that use them.
/// </summary>
internal static class InkDefaults
{
    /// <summary>A sample closer than this to the last kept one (in DIP) is noise and is dropped.</summary>
    public const float MinSampleDistance = 0.25f;

    /// <summary>A pen sample that did not move is still kept when its pressure changed by this much.</summary>
    public const float MinPressureChange = 0.02f;

    /// <summary>The shortest time between samples used for velocity (ms), so equal timestamps cannot give infinity.</summary>
    public const double MinDeltaTime = 1.0;

    /// <summary>The weight of the newest velocity in the exponential filter: higher reacts faster, lower is smoother.</summary>
    public const float VelocityFilterWeight = 0.7f;

    /// <summary>The velocity (DIP/ms) at which the velocity part of the width is halfway between its extremes.</summary>
    public const float ReferenceVelocity = 1.0f;

    /// <summary>How much velocity still thins a pen stroke whose width comes from pressure (0 = none, 1 = fully).</summary>
    public const float PenVelocityInfluence = 0.3f;

    /// <summary>How strongly the drawn centre line trails the input to smooth it (0 = not at all).</summary>
    public const float Streamline = 0.5f;
}
