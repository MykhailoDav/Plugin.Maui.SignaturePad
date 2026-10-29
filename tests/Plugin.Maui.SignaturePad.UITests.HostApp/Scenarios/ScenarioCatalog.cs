using System.Collections.ObjectModel;
using System.Globalization;

namespace Plugin.Maui.SignaturePad.UITests.HostApp.Scenarios;

/// <summary>A scenario of the gallery: its name (what the tests type to open it) and how to build its page.</summary>
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
        new("InScrollView", InScrollView),
        new("Disabled", Disabled),
        new("Rendering", Rendering),
        new("Tabs", Tabs),
        new("BindableLayout", BindableLayoutScenario),
        new("Benchmark", Benchmark),
        new("NaturalSize", NaturalSize),
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

    /// <summary>
    /// A pad in a scroll view that can scroll: a stroke on the pad must not scroll it, and a swipe on the handle below
    /// must (<c>ScrollY</c> shows the offset).
    /// </summary>
    private static ScenarioPage InScrollView()
    {
        var pad = Pad();
        var scrollY = new Label { AutomationId = "ScrollY", Text = "0", FontSize = 11 };
        var scroll = new ScrollView
        {
            HeightRequest = 380,
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label { Text = "The pad is in a ScrollView.", FontSize = 12 },
                    pad,

                    // A visible area right below the pad to swipe on; the filler only makes the content tall enough to scroll.
                    new BoxView { AutomationId = "ScrollHandle", HeightRequest = 100, Color = Color.FromArgb("#BBDEFB") },
                    new BoxView { HeightRequest = 900, Color = Color.FromArgb("#E3F2FD") },
                },
            },
        };
        scroll.Scrolled += (_, e) => scrollY.Text = Math.Round(e.ScrollY).ToString(CultureInfo.InvariantCulture);

        return new ScenarioPage("InScrollView", pad, new VerticalStackLayout { Children = { scroll, scrollY } });
    }

    /// <summary>A disabled pad and a button that enables or disables it.</summary>
    private static ScenarioPage Disabled()
    {
        var pad = Pad();
        pad.IsEnabled = false;
        var state = new Label { AutomationId = "PadEnabled", Text = "False", FontSize = 11 };
        return new ScenarioPage("Disabled", pad, new VerticalStackLayout { Children = { pad, state } },
        [
            new("ToggleEnabled", "Enable/Disable", () =>
            {
                pad.IsEnabled = !pad.IsEnabled;
                state.Text = pad.IsEnabled.ToString();
                return Task.CompletedTask;
            }),
        ]);
    }

    /// <summary>Loads the fixed signatures of <see cref="Fixtures"/> for the screenshot tests.</summary>
    private static ScenarioPage Rendering()
    {
        var pad = Pad();
        return new ScenarioPage("Rendering", pad, actions:
        [
            new("LoadPenPressure", "Pen", () => Load(pad, Fixtures.PenPressure())),
            new("LoadVelocity", "Velocity", () => Load(pad, Fixtures.Velocity())),
            new("LoadDotsAndLoops", "Dots", () => Load(pad, Fixtures.DotsAndLoops())),
        ]);

        static Task Load(SignaturePadView pad, SignatureData data)
        {
            pad.Signature = data;
            return Task.CompletedTask;
        }
    }

    /// <summary>The pad on the first tab of a Shell; the ink must survive switching tabs.</summary>
    private static Shell Tabs()
    {
        var pad = Pad();
        return new Shell
        {
            Items =
            {
                new TabBar
                {
                    Items =
                    {
                        new ShellContent { Title = "PadTab", Content = new ScenarioPage("Tabs", pad) },
                        new ShellContent
                        {
                            Title = "OtherTab",
                            Content = new ContentPage { Content = new Label { Text = "Another tab", AutomationId = "OtherTabContent" } },
                        },
                    },
                },
            },
        };
    }

    /// <summary>
    /// A pad with no size of its own in a vertical stack, below a ruler 100 tall: the pad takes its natural height, twice
    /// the ruler's, and fills the width as the ruler does.
    /// </summary>
    private static ScenarioPage NaturalSize()
    {
        var pad = new SignaturePadView { BackgroundColor = Color.FromArgb("#FAFAFA"), Placeholder = "Sign here" };
        var ruler = new BoxView { AutomationId = "Ruler", HeightRequest = 100, Color = Color.FromArgb("#BBDEFB") };
        return new ScenarioPage("NaturalSize", pad, new VerticalStackLayout { Spacing = 8, Children = { ruler, pad } });
    }

    /// <summary>A pad that loads a heavy signature, to measure the cost of drawing over it.</summary>
    private static ScenarioPage Benchmark()
    {
        var pad = Pad();
        return new ScenarioPage("Benchmark", pad, actions:
        [
            new("LoadHeavy", "Heavy", () =>
            {
                pad.Signature = Fixtures.Heavy();
                return Task.CompletedTask;
            }),
        ]);
    }

    /// <summary>Three pads made by a BindableLayout item template; each counts its own strokes.</summary>
    private static ScenarioPage BindableLayoutScenario()
    {
        var index = 0;
        var list = new VerticalStackLayout { Spacing = 6 };
        BindableLayout.SetItemTemplate(list, new DataTemplate(() =>
        {
            var number = index++;
            var pad = Pad();
            pad.HeightRequest = 110;
            pad.AutomationId = "Pad" + number.ToString(CultureInfo.InvariantCulture);
            var count = new Label { AutomationId = "Count" + number.ToString(CultureInfo.InvariantCulture), Text = "0", FontSize = 11 };
            pad.StrokeCompleted += (_, _) => count.Text = pad.Signature.Strokes.Count.ToString(CultureInfo.InvariantCulture);
            return new VerticalStackLayout { Children = { pad, count } };
        }));
        BindableLayout.SetItemsSource(list, new ObservableCollection<string> { "first", "second", "third" });

        // The status and the default actions refer to a pad of its own, named Pad, which the tests do not draw on.
        var statusPad = Pad();
        statusPad.HeightRequest = 1;
        return new ScenarioPage("BindableLayout", statusPad, new VerticalStackLayout { Children = { list, statusPad } });
    }
}
