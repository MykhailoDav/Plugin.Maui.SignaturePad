using NUnit.Framework;
using Plugin.Maui.SignaturePad.UITests.Infrastructure;

namespace Plugin.Maui.SignaturePad.UITests.Tests;

/// <summary>Ink survives the pad's native view going away and coming back, and pads in a BindableLayout stay separate.</summary>
public class NavigationTests : UITest
{
    [Test]
    public void Shell_InkSurvivesSwitchingTabs()
    {
        App.OpenScenario("Tabs");
        App.Scribble();
        Assert.That(App.WaitForText("StrokeCount", "1"), Is.EqualTo("1"));

        App.TapText("OtherTab");
        App.Element("OtherTabContent");
        App.TapText("PadTab");

        Assert.That(App.WaitForText("StrokeCount", "1"), Is.EqualTo("1"));
        App.Scribble(top: 0.5f);
        Assert.That(App.WaitForText("StrokeCount", "2"), Is.EqualTo("2"));
    }

    [Test]
    public void BindableLayout_EachPadKeepsItsOwnStrokes()
    {
        App.OpenScenario("BindableLayout");

        App.Scribble("Pad1");

        Assert.That(App.WaitForText("Count1", "1"), Is.EqualTo("1"));
        Assert.That(App.Text("Count0"), Is.EqualTo("0"));
        Assert.That(App.Text("Count2"), Is.EqualTo("0"));
    }
}
