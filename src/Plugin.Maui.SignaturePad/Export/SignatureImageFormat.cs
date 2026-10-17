namespace Plugin.Maui.SignaturePad;

/// <summary>
/// The format of an exported signature image.
/// </summary>
public enum SignatureImageFormat
{
    /// <summary>PNG, lossless and with transparency. The default.</summary>
    Png,

    /// <summary>JPEG, smaller but lossy and without transparency; the background is white unless set.</summary>
    Jpeg,
}
