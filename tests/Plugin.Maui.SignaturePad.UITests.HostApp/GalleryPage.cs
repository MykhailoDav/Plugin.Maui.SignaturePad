using Plugin.Maui.SignaturePad.UITests.HostApp.Scenarios;

namespace Plugin.Maui.SignaturePad.UITests.HostApp;

/// <summary>
/// The first page. The UI tests open a scenario by typing its name into <c>ScenarioEntry</c> and tapping <c>GoButton</c>,
/// which works however many scenarios there are; people can tap the scenario buttons below.
/// </summary>
public sealed class GalleryPage : ContentPage
{
    public GalleryPage()
    {
        Title = "Scenarios";
        BackgroundColor = Colors.White;

        var entry = new Entry { AutomationId = "ScenarioEntry", Placeholder = "Scenario", HorizontalOptions = LayoutOptions.Fill };
        var go = new Button { AutomationId = "GoButton", Text = "Go" };
        go.Clicked += (_, _) => Open(entry.Text);

        var list = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 8 };
        list.Add(new Label { Text = "Scenarios", FontSize = 20 });
        list.Add(new Grid
        {
            ColumnDefinitions = [new(GridLength.Star), new(GridLength.Auto)],
            ColumnSpacing = 8,
            Children = { entry, WithColumn(go, 1) },
        });
        foreach (var scenario in ScenarioCatalog.All)
        {
            var button = new Button { Text = scenario.Name, HorizontalOptions = LayoutOptions.Fill };
            button.Clicked += (_, _) => App.Show(scenario.Create());
            list.Add(button);
        }

        Content = new ScrollView { Content = list };
    }

    private static void Open(string? name)
    {
        var scenario = ScenarioCatalog.All.FirstOrDefault(s => string.Equals(s.Name, name?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (scenario is not null)
            App.Show(scenario.Create());
    }

    private static View WithColumn(View view, int column)
    {
        Grid.SetColumn(view, column);
        return view;
    }
}
