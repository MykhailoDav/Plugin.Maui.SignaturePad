namespace Plugin.Maui.SignaturePad.Tests.Data;

public class ColorBytesTests
{
    [Fact]
    public void ToHex_WritesUppercaseRgba() =>
        Assert.Equal("#1E88E5FF", ColorBytes.ToHex(Color.FromRgba(0x1E, 0x88, 0xE5, 0xFF)));

    [Fact]
    public void ToRgba_RoundsInsteadOfTruncating()
    {
        // MAUI's own Color.ToInt() truncates, so 0.5 would become 127 there; rounding keeps a parsed color stable.
        var rgba = ColorBytes.ToRgba(new Color(0.5f, 1f, 0f, 1f));

        Assert.Equal(0x80FF00FFu, rgba);
    }

    [Fact]
    public void ToRgba_ClampsOutOfRangeComponents() =>
        Assert.Equal(0xFF0000FFu, ColorBytes.ToRgba(new Color(2f, -1f, 0f, 1f)));

    [Fact]
    public void HexRoundTrip_IsStable_ForEveryByteValue()
    {
        for (var value = 0; value <= 255; value++)
        {
            var hex = $"#{value:X2}{255 - value:X2}{value:X2}{value:X2}";

            Assert.True(ColorBytes.TryParseHex(hex, out var color));
            Assert.Equal(hex, ColorBytes.ToHex(color));
        }
    }

    [Theory]
    [InlineData("#00000000")]
    [InlineData("#abcdef12")]
    public void TryParseHex_AcceptsRgbaInAnyCase(string hex)
    {
        Assert.True(ColorBytes.TryParseHex(hex, out var color));
        Assert.Equal(hex.ToUpperInvariant(), ColorBytes.ToHex(color));
    }

    [Theory]
    [InlineData("")]
    [InlineData("000000FF")]
    [InlineData("#000000")]
    [InlineData("#000000FFF")]
    [InlineData("#00000GFF")]
    [InlineData("#00 000FF")]
    public void TryParseHex_RejectsAnythingElse(string hex)
    {
        Assert.False(ColorBytes.TryParseHex(hex, out var color));
        Assert.Null(color);
    }
}
