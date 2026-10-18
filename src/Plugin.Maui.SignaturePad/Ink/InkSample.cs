namespace Plugin.Maui.SignaturePad.Ink;

/// <summary>
/// One raw input sample as the platform reports it, already in DIP.
/// </summary>
/// <param name="X">The horizontal position in DIP.</param>
/// <param name="Y">The vertical position in DIP.</param>
/// <param name="TimestampMs">The platform's timestamp of the sample in milliseconds, on any clock that does not jump.</param>
/// <param name="Pressure">The pen pressure from 0 to 1, or <see cref="float.NaN"/> when the device reports none.</param>
/// <param name="Device">The kind of device that produced the sample.</param>
internal readonly record struct InkSample(float X, float Y, double TimestampMs, float Pressure, SignatureInputDevice Device);
