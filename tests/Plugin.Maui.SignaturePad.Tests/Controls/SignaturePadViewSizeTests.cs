namespace Plugin.Maui.SignaturePad.Tests.Controls;

/// <summary>The size a pad asks for: the space it is given, and its natural size where a layout leaves the size open.</summary>
public class SignaturePadViewSizeTests
{
    private const double Open = double.PositiveInfinity;

    private static Size Measure(SignaturePadView view, double width, double height) => ((IView)view).Measure(width, height);

    [Fact]
    public void WithNoSizeAndNoLimit_ItHasItsNaturalSize() =>
        Assert.Equal(new Size(300, 200), Measure(new SignaturePadView(), Open, Open));

    [Fact]
    public void InAVerticalStack_ItFillsTheWidth_AndHasItsNaturalHeight() =>
        Assert.Equal(new Size(360, 200), Measure(new SignaturePadView(), 360, Open));

    [Fact]
    public void InABoundedSpace_ItFillsTheSpace() =>
        Assert.Equal(new Size(360, 500), Measure(new SignaturePadView(), 360, 500));

    [Fact]
    public void ARequestedSize_WinsOverTheNaturalSize() =>
        Assert.Equal(new Size(250, 120), Measure(new SignaturePadView { WidthRequest = 250, HeightRequest = 120 }, Open, Open));

    [Fact]
    public void ARequestedSize_WinsOverTheSpace() =>
        Assert.Equal(new Size(250, 120), Measure(new SignaturePadView { WidthRequest = 250, HeightRequest = 120 }, 360, 500));

    [Fact]
    public void ARequestedSize_IsCutToTheSpace() =>
        Assert.Equal(new Size(360, 500), Measure(new SignaturePadView { WidthRequest = 400, HeightRequest = 600 }, 360, 500));

    [Fact]
    public void TheMinimumAndMaximum_BoundTheNaturalSize()
    {
        Assert.Equal(new Size(300, 250), Measure(new SignaturePadView { MinimumHeightRequest = 250 }, Open, Open));
        Assert.Equal(new Size(200, 150), Measure(new SignaturePadView { MaximumWidthRequest = 200, MaximumHeightRequest = 150 }, Open, Open));
    }

    [Fact]
    public void TheMargin_IsAddedAroundTheNaturalSize() =>
        Assert.Equal(new Size(320, 220), Measure(new SignaturePadView { Margin = new Thickness(10) }, Open, Open));

    [Fact]
    public void TheMargin_IsTakenFromTheSpace() =>
        Assert.Equal(new Size(360, 500), Measure(new SignaturePadView { Margin = new Thickness(10) }, 360, 500));

    [Fact]
    public void Measuring_SetsTheDesiredSize()
    {
        var view = new SignaturePadView();

        Measure(view, 360, Open);

        Assert.Equal(new Size(360, 200), view.DesiredSize);
    }
}
