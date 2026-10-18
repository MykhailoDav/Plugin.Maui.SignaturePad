using Plugin.Maui.SignaturePad.Capture;

namespace Plugin.Maui.SignaturePad.Tests.Capture;

public class PointerTrackerTests
{
    [Fact]
    public void FirstPointer_IsTracked()
    {
        var tracker = new PointerTracker();

        Assert.True(tracker.TryBegin(7));
        Assert.True(tracker.IsTracking);
        Assert.True(tracker.IsTracked(7));
        Assert.False(tracker.IsTracked(8));
    }

    [Fact]
    public void SecondPointer_IsIgnored_UntilTheFirstEnds()
    {
        var tracker = new PointerTracker();
        tracker.TryBegin(1);

        Assert.False(tracker.TryBegin(2));
        Assert.False(tracker.End(2));
        Assert.True(tracker.IsTracked(1));

        Assert.True(tracker.End(1));
        Assert.False(tracker.IsTracking);
        Assert.True(tracker.TryBegin(2));
    }

    [Fact]
    public void Reset_ForgetsThePointer()
    {
        var tracker = new PointerTracker();
        tracker.TryBegin(1);

        tracker.Reset();

        Assert.False(tracker.IsTracking);
        Assert.False(tracker.IsTracked(1));
    }
}
