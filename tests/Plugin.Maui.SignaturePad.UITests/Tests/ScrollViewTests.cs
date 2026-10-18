using NUnit.Framework;
using Plugin.Maui.SignaturePad.UITests.Infrastructure;

namespace Plugin.Maui.SignaturePad.UITests.Tests;

/// <summary>A pad in a ScrollView: signing must not scroll the page, and scrolling elsewhere must still work.</summary>
public class ScrollViewTests : UITest
{
    [Test]
    public void VerticalStrokeOnThePad_DrawsWithoutScrolling()
    {
        App.OpenScenario("InScrollView");

        App.Draw("Pad", [new(0.5f, 0.1f), new(0.45f, 0.9f)], TimeSpan.FromMilliseconds(400));

        Assert.That(App.WaitForText("StrokeCount", "1"), Is.EqualTo("1"));
        Assert.That(App.Text("ScrollY"), Is.EqualTo("0"));
    }

    [Test]
    public void SwipeOutsideThePad_Scrolls()
    {
        App.OpenScenario("InScrollView");

        App.Swipe("ScrollHandle", new(0.5f, 0.9f), new(0.5f, 0.1f));

        Assert.That(App.WaitForText("ScrollY", text => text != "0"), Is.Not.EqualTo("0"));
        Assert.That(App.Text("StrokeCount"), Is.EqualTo("0"));
    }
}
