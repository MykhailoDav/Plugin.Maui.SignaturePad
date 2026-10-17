namespace Plugin.Maui.SignaturePad.Ink;

/// <summary>
/// A point of the drawn centre line: the smoothed position and the half-width of the ink there, in DIP.
/// </summary>
internal readonly record struct InkPoint(float X, float Y, float Radius);
