using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Plugin.Maui.SignaturePad;

/// <summary>
/// Converts colors to and from 8 bits per channel. MAUI's own <see cref="Color.ToInt"/> and hex methods truncate, so a
/// color parsed from hex can come back one step lower; rounding here keeps every color stable through a round trip.
/// </summary>
internal static class ColorBytes
{
    /// <summary>The color as 0xRRGGBBAA.</summary>
    public static uint ToRgba(Color color) =>
        (ToByte(color.Red) << 24) | (ToByte(color.Green) << 16) | (ToByte(color.Blue) << 8) | ToByte(color.Alpha);

    /// <summary>The color as <c>#RRGGBBAA</c>, with uppercase hex digits.</summary>
    public static string ToHex(Color color) => "#" + ToRgba(color).ToString("X8", CultureInfo.InvariantCulture);

    /// <summary>Parses exactly <c>#RRGGBBAA</c> (hex digits in any case).</summary>
    public static bool TryParseHex(ReadOnlySpan<char> hex, [NotNullWhen(true)] out Color? color)
    {
        if (hex.Length != 9 || hex[0] != '#'
            || !uint.TryParse(hex[1..], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var rgba))
        {
            color = null;
            return false;
        }

        color = new Color(
            ((rgba >> 24) & 0xFF) / 255f,
            ((rgba >> 16) & 0xFF) / 255f,
            ((rgba >> 8) & 0xFF) / 255f,
            (rgba & 0xFF) / 255f);
        return true;
    }

    private static uint ToByte(float component) => (uint)Math.Clamp(MathF.Round(component * 255f), 0f, 255f);
}
