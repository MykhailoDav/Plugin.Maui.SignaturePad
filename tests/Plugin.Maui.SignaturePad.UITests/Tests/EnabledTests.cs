using NUnit.Framework;
using Plugin.Maui.SignaturePad.UITests.Infrastructure;

namespace Plugin.Maui.SignaturePad.UITests.Tests;

/// <summary>A disabled pad ignores input until it is enabled again.</summary>
public class EnabledTests : UITest
{
    [Test]
    public void DisabledPad_IgnoresInput_ThenDrawsOnceEnabled()
    {
        App.OpenScenario("Disabled");

        App.Scribble();
        Assert.That(App.WaitForText("StrokeCount", "1", TimeSpan.FromSeconds(2)), Is.EqualTo("0"));

        App.Tap("ToggleEnabled");
        Assert.That(App.WaitForText("PadEnabled", "True"), Is.EqualTo("True"));
        App.Scribble();

        Assert.That(App.WaitForText("StrokeCount", "1"), Is.EqualTo("1"));
    }
}
