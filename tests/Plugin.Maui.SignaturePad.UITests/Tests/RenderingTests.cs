using NUnit.Framework;
using Plugin.Maui.SignaturePad.UITests.Infrastructure;

namespace Plugin.Maui.SignaturePad.UITests.Tests;

/// <summary>
/// Compares screenshots of fixed signatures with the baselines. A drawn gesture never repeats exactly (the driver's timing
/// varies), so only loaded signatures and the empty pad have baselines.
/// </summary>
public class RenderingTests : UITest
{
    [Test]
    public void EmptyPad_ShowsThePlaceholderAndTheLine()
    {
        App.OpenScenario("Basic");

        PadMatchesBaseline("Empty");
    }

    [TestCase("LoadPenPressure", "PenPressure")]
    [TestCase("LoadVelocity", "Velocity")]
    [TestCase("LoadDotsAndLoops", "DotsAndLoops")]
    public void LoadedSignature_MatchesTheBaseline(string action, string baseline)
    {
        App.OpenScenario("Rendering");

        App.Tap(action);

        Assert.That(App.WaitForText("IsEmpty", "False"), Is.EqualTo("False"));
        PadMatchesBaseline(baseline);
    }
}
