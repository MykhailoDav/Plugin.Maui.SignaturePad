namespace Plugin.Maui.SignaturePad;

public partial class SignaturePadView
{
    /// <summary>
    /// Measures the pad. It fills the space its layout gives it. Where the layout leaves the height open, as a vertical
    /// stack, a scroll view or an Auto row does, the pad is 200 tall, and where it leaves the width open, 300 wide.
    /// <see cref="VisualElement.HeightRequest"/>, <see cref="VisualElement.WidthRequest"/> and the minimum and maximum
    /// requests win over both.
    /// </summary>
    /// <param name="widthConstraint">The width available, which may be infinite.</param>
    /// <param name="heightConstraint">The height available, which may be infinite.</param>
    /// <returns>The size the pad asks for, with its margin.</returns>
    protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
    {
        // The native view is still measured for its own layout, but it has no content to size the pad by.
        _ = base.MeasureOverride(widthConstraint, heightConstraint);

        var margin = Margin;
        var size = PadSize.Desired(this, widthConstraint - margin.HorizontalThickness, heightConstraint - margin.VerticalThickness);
        return new Size(size.Width + margin.HorizontalThickness, size.Height + margin.VerticalThickness);
    }
}
