using System.Globalization;
using NUnit.Framework;
using Plugin.Maui.SignaturePad.UITests.Infrastructure;

namespace Plugin.Maui.SignaturePad.UITests.Tests;

/// <summary>Draws on the pad with the platform's pointer and checks the strokes it records.</summary>
public class DrawingTests : UITest
{
    [Test]
    public void Stroke_IsRecorded_WithItsSamples()
    {
        App.OpenScenario("Basic");

        App.Scribble();

        Assert.That(App.WaitForText("StrokeCount", "1"), Is.EqualTo("1"));
        Assert.That(App.Text("IsEmpty"), Is.EqualTo("False"));
        Assert.That(int.Parse(App.Text("PointCount"), CultureInfo.InvariantCulture), Is.GreaterThan(10));
        Assert.That(App.Text("Events"), Is.EqualTo("started 1 completed 1"));
        Assert.That(App.Text("LastDevice"), Is.EqualTo(SignaturePadApp.IsTouch ? "Touch" : "Mouse"));
        Assert.That(App.Text("HasPressure"), Is.EqualTo("False"));
    }

    [Test]
    public void Tap_DrawsADot()
    {
        App.OpenScenario("Basic");

        App.Dot("Pad", 0.5f, 0.5f);

        Assert.That(App.WaitForText("StrokeCount", "1"), Is.EqualTo("1"));
        Assert.That(int.Parse(App.Text("PointCount"), CultureInfo.InvariantCulture), Is.LessThanOrEqualTo(3));
    }

    [Test]
    public void SeveralStrokes_AreRecordedInOrder()
    {
        App.OpenScenario("Basic");

        App.Scribble(top: 0.1f);
        App.Scribble(top: 0.5f);
        App.Dot("Pad", 0.95f, 0.9f);

        Assert.That(App.WaitForText("StrokeCount", "3"), Is.EqualTo("3"));
        Assert.That(App.Text("Events"), Is.EqualTo("started 3 completed 3"));
    }

    [Test]
    public void Stylus_RecordsAPenStrokeWithPressure()
    {
        OnlyOn(TestPlatform.Android, "only Android can be driven with a stylus");
        App.OpenScenario("Basic");

        App.DrawWithStylus("Pad", new(0.1f, 0.5f), new(0.9f, 0.4f), TimeSpan.FromMilliseconds(500));

        Assert.That(App.WaitForText("StrokeCount", "1"), Is.EqualTo("1"));
        Assert.That(App.Text("LastDevice"), Is.EqualTo("Pen"));
        Assert.That(App.Text("HasPressure"), Is.EqualTo("True"));
    }
}
