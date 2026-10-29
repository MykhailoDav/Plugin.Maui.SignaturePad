using NUnit.Framework;
using Plugin.Maui.SignaturePad.UITests.Infrastructure;

namespace Plugin.Maui.SignaturePad.UITests.Tests;

/// <summary>How layouts size the pad on a device.</summary>
public class LayoutTests : UITest
{
    [Test]
    public void PadWithoutASize_InAVerticalStack_HasItsNaturalHeight_AndFillsTheWidth()
    {
        App.OpenScenario("NaturalSize");

        // Sizes come in points on iOS and in pixels on Android, so the pad is compared with a ruler 100 tall.
        var ruler = App.SizeOf("Ruler");
        var pad = App.SizeOf("Pad");

        Assert.That((double)pad.Height / ruler.Height, Is.EqualTo(2).Within(0.03));
        Assert.That(pad.Width, Is.EqualTo(ruler.Width).Within(2));
    }
}
