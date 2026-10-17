namespace Plugin.Maui.SignaturePad;

/// <summary>
/// The kind of input device that drew a stroke.
/// </summary>
public enum SignatureInputDevice
{
    /// <summary>The device is not known, for example in data from another source.</summary>
    Unknown,

    /// <summary>A finger on a touch screen.</summary>
    Touch,

    /// <summary>A pen or stylus, such as Apple Pencil, an Android stylus or a Windows pen. Pens report pressure.</summary>
    Pen,

    /// <summary>A mouse, trackpad or other indirect pointer.</summary>
    Mouse,
}
