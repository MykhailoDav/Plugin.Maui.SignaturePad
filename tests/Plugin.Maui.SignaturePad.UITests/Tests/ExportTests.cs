using System.Globalization;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Plugin.Maui.SignaturePad.UITests.Infrastructure;

namespace Plugin.Maui.SignaturePad.UITests.Tests;

/// <summary>Exports what was drawn; the scenario shows the size of the PNG and the length of the SVG.</summary>
public partial class ExportTests : UITest
{
    [Test]
    public void Png_IsCroppedToTheInk()
    {
        App.OpenScenario("Basic");
        App.Draw("Pad", [new(0.2f, 0.5f), new(0.6f, 0.5f)], TimeSpan.FromMilliseconds(400));
        App.WaitForText("StrokeCount", "1");

        App.Tap("ExportPng");

        var export = App.WaitForText("LastExport", text => text.StartsWith("png ", StringComparison.Ordinal), TimeSpan.FromSeconds(10));
        var match = PngSize().Match(export);
        Assert.That(match.Success, Is.True, $"LastExport shows '{export}'.");

        // A horizontal line across 40% of the 320 DIP pad, plus the ink and 8 DIP of padding on each side, at 1 px per DIP.
        var width = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var height = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        Assert.That(width, Is.InRange(130, 160));
        Assert.That(height, Is.InRange(16, 30));
    }

    [Test]
    public void Svg_IsWritten()
    {
        App.OpenScenario("Basic");
        App.Scribble();
        App.WaitForText("StrokeCount", "1");

        App.Tap("ExportSvg");

        var export = App.WaitForText("LastExport", text => text.StartsWith("svg ", StringComparison.Ordinal));
        Assert.That(int.Parse(export[4..], CultureInfo.InvariantCulture), Is.GreaterThan(200));
    }

    [Test]
    public void EmptyPad_CannotBeExported()
    {
        App.OpenScenario("Basic");

        App.Tap("ExportPng");

        Assert.That(App.WaitForText("LastExport", "error InvalidOperationException"), Is.EqualTo("error InvalidOperationException"));
    }

    [GeneratedRegex(@"^png (\d+)x(\d+)$")]
    private static partial Regex PngSize();
}
