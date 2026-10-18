namespace Plugin.Maui.SignaturePad.UITests.HostApp.Scenarios;

/// <summary>A scenario of the gallery: its name (the automation id of its button) and how to build its page.</summary>
public sealed record Scenario(string Name, Func<Page> Create);

/// <summary>Every scenario the UI tests open.</summary>
public static class ScenarioCatalog
{
    /// <summary>The size of every pad, so laid-out pads and screenshots match on every device.</summary>
    public const double PadWidth = 320;

    /// <inheritdoc cref="PadWidth"/>
    public const double PadHeight = 200;

    public static IReadOnlyList<Scenario> All { get; } =
    [
        new("Basic", () => new ScenarioPage("Basic", Pad())),
    ];

    /// <summary>A pad with the size, colors and placeholder the scenarios share.</summary>
    public static SignaturePadView Pad() => new()
    {
        WidthRequest = PadWidth,
        HeightRequest = PadHeight,
        HorizontalOptions = LayoutOptions.Start,
        BackgroundColor = Color.FromArgb("#FAFAFA"),
        Placeholder = "Sign here",
    };
}
