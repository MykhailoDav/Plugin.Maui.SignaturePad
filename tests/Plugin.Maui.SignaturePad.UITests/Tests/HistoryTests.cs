using NUnit.Framework;
using Plugin.Maui.SignaturePad.UITests.Infrastructure;

namespace Plugin.Maui.SignaturePad.UITests.Tests;

/// <summary>Undo, redo and clear through the pad's methods, as the buttons of the scenario call them.</summary>
public class HistoryTests : UITest
{
    [Test]
    public void UndoRedoAndClear_WorkTogether()
    {
        App.OpenScenario("Basic");
        App.Scribble(top: 0.1f);
        App.Scribble(top: 0.5f);
        Assert.That(App.WaitForText("StrokeCount", "2"), Is.EqualTo("2"));

        App.Tap("Undo");
        Assert.That(App.WaitForText("StrokeCount", "1"), Is.EqualTo("1"));
        Assert.That(App.Text("CanRedo"), Is.EqualTo("True"));

        App.Tap("Redo");
        Assert.That(App.WaitForText("StrokeCount", "2"), Is.EqualTo("2"));

        App.Tap("Clear");
        Assert.That(App.WaitForText("IsEmpty", "True"), Is.EqualTo("True"));

        App.Tap("Undo");
        Assert.That(App.WaitForText("StrokeCount", "2"), Is.EqualTo("2"));
        Assert.That(App.Text("CanUndo"), Is.EqualTo("True"));
    }
}
