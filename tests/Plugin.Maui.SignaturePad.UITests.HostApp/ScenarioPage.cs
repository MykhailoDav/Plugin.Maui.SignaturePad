using System.Globalization;

namespace Plugin.Maui.SignaturePad.UITests.HostApp;

/// <summary>A button of a scenario that acts on its pad.</summary>
public sealed record ScenarioAction(string AutomationId, string Text, Func<Task> Run);

/// <summary>
/// The page of a scenario: a <c>BackButton</c>, the scenario's content with its pad (<c>Pad</c>), action buttons, and the
/// pad's state as texts the UI tests read (<c>IsEmpty</c>, <c>StrokeCount</c>, <c>PointCount</c>, <c>CanUndo</c>,
/// <c>CanRedo</c>, <c>LastDevice</c>, <c>HasPressure</c>, <c>LastExport</c>, <c>Events</c>).
/// </summary>
public sealed class ScenarioPage : ContentPage
{
    private static readonly string[] StatusKeys =
        ["IsEmpty", "StrokeCount", "PointCount", "CanUndo", "CanRedo", "LastDevice", "HasPressure", "LastExport", "Events"];

    private readonly SignaturePadView _pad;
    private readonly Dictionary<string, Label> _status = [];
    private int _strokesStarted;
    private int _strokesCompleted;

    /// <param name="name">The scenario's name, shown as the title.</param>
    /// <param name="pad">The pad the status and the default actions refer to.</param>
    /// <param name="content">What the scenario shows; the pad itself when <see langword="null"/>.</param>
    /// <param name="actions">Actions besides the default ones.</param>
    public ScenarioPage(string name, SignaturePadView pad, View? content = null, IReadOnlyList<ScenarioAction>? actions = null)
    {
        Title = name;
        BackgroundColor = Colors.White;
        _pad = pad;
        pad.AutomationId ??= "Pad";
        pad.PropertyChanged += (_, _) => UpdateStatus();
        pad.StrokeStarted += (_, _) => { _strokesStarted++; UpdateStatus(); };
        pad.StrokeCompleted += (_, _) => { _strokesCompleted++; UpdateStatus(); };

        var back = new Button { AutomationId = "BackButton", Text = "Back", Padding = new Thickness(12, 4) };
        back.Clicked += (_, _) => App.Show(new GalleryPage());
        var header = new HorizontalStackLayout
        {
            Spacing = 12,
            Children = { back, new Label { Text = name, FontSize = 18, VerticalOptions = LayoutOptions.Center } },
        };

        var buttons = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };
        foreach (var action in DefaultActions().Concat(actions ?? []))
        {
            var button = new Button { AutomationId = action.AutomationId, Text = action.Text, FontSize = 12, Margin = new Thickness(2), Padding = new Thickness(8, 2) };
            button.Clicked += async (_, _) =>
            {
                try
                {
                    await action.Run();
                }
                catch (Exception exception)
                {
                    SetStatus("LastExport", "error " + exception.GetType().Name);
                }

                UpdateStatus();
            };
            buttons.Add(button);
        }

        var status = new Grid { ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Star)], ColumnSpacing = 8 };
        foreach (var key in StatusKeys)
        {
            var row = status.RowDefinitions.Count;
            status.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            // The caption must not read like the id: iOS also finds an element by its text.
            status.Add(new Label { Text = key + ":", FontSize = 11, TextColor = Colors.Gray }, 0, row);
            var value = new Label { AutomationId = key, FontSize = 11, TextColor = Colors.Black };
            _status[key] = value;
            status.Add(value, 1, row);
        }

        Content = new Grid
        {
            Padding = new Thickness(12, 8),
            RowSpacing = 8,
            RowDefinitions = [new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto)],
            Children =
            {
                header,
                WithRow(content ?? pad, 1),
                WithRow(buttons, 2),
                WithRow(status, 3),
            },
        };

        UpdateStatus();
    }

    private void SetStatus(string key, string value) => _status[key].Text = value;

    private void UpdateStatus()
    {
        var signature = _pad.Signature;
        SetStatus("IsEmpty", _pad.IsEmpty.ToString());
        SetStatus("StrokeCount", signature.Strokes.Count.ToString(CultureInfo.InvariantCulture));
        SetStatus("PointCount", signature.PointCount.ToString(CultureInfo.InvariantCulture));
        SetStatus("CanUndo", _pad.CanUndo.ToString());
        SetStatus("CanRedo", _pad.CanRedo.ToString());
        SetStatus("LastDevice", signature.IsEmpty ? "None" : signature.Strokes[^1].Device.ToString());
        SetStatus("HasPressure", (!signature.IsEmpty && signature.Strokes[^1].Points.Any(p => p.Pressure.HasValue)).ToString());
        SetStatus("Events", $"started {_strokesStarted} completed {_strokesCompleted}");
    }

    private IEnumerable<ScenarioAction> DefaultActions()
    {
        yield return new("Undo", "Undo", () => { _pad.Undo(); return Task.CompletedTask; });
        yield return new("Redo", "Redo", () => { _pad.Redo(); return Task.CompletedTask; });
        yield return new("Clear", "Clear", () => { _pad.Clear(); return Task.CompletedTask; });
        yield return new("ExportPng", "PNG", ExportPngAsync);
        yield return new("ExportSvg", "SVG", () =>
        {
            SetStatus("LastExport", "svg " + _pad.ExportSvg().Length.ToString(CultureInfo.InvariantCulture));
            return Task.CompletedTask;
        });
    }

    private async Task ExportPngAsync()
    {
        await using var stream = await _pad.ExportImageAsync(new SignatureImageExportOptions { Scale = 1 });
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        var bytes = memory.ToArray();

        // A PNG starts with an 8-byte signature and the IHDR chunk, whose first fields are the width and height.
        var width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
        var height = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
        SetStatus("LastExport", string.Create(CultureInfo.InvariantCulture, $"png {width}x{height}"));
    }

    private static View WithRow(View view, int row)
    {
        Grid.SetRow(view, row);
        return view;
    }
}
