namespace Plugin.Maui.SignaturePad;

/// <summary>
/// One input sample of a signature stroke.
/// </summary>
/// <param name="X">The horizontal position, in device-independent units from the left edge of the pad.</param>
/// <param name="Y">The vertical position, in device-independent units from the top edge of the pad.</param>
/// <param name="Time">The time of the sample, in milliseconds since the first point of the signature.</param>
/// <param name="Pressure">
/// The pen pressure from 0 to 1, or <see langword="null"/> when the input device does not report real pressure
/// (finger and mouse input).
/// </param>
public readonly record struct SignaturePoint(float X, float Y, double Time, float? Pressure = null);
