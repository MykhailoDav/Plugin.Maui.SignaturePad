using Plugin.Maui.SignaturePad.UITests.HostApp.Scenarios;

namespace Plugin.Maui.SignaturePad.UITests.HostApp;

/// <summary>The first page: one button per scenario, whose automation id is the scenario's name.</summary>
public sealed class GalleryPage : ContentPage
{
    public GalleryPage()
    {
        Title = "Scenarios";
        var list = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 8 };
        list.Add(new Label { Text = "Scenarios", FontSize = 20, AutomationId = "GalleryTitle" });
        foreach (var scenario in ScenarioCatalog.All)
        {
            var button = new Button { Text = scenario.Name, AutomationId = scenario.Name, HorizontalOptions = LayoutOptions.Fill };
            button.Clicked += (_, _) => App.Show(scenario.Create());
            list.Add(button);
        }

        Content = new ScrollView { Content = list };
    }
}
