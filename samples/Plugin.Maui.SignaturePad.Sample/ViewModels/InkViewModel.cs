using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Plugin.Maui.SignaturePad.Sample.ViewModels;

/// <summary>A color to pick for the ink.</summary>
/// <param name="Name">The name read by screen readers.</param>
/// <param name="Color">The color.</param>
public sealed record InkColor(string Name, Color Color);

/// <summary>
/// The Ink page: the pad's ink settings, bound to controls, and the details of the last stroke, which come from the
/// pad's <see cref="SignaturePadView.StrokeCompletedCommand"/>.
/// </summary>
public partial class InkViewModel : ObservableObject
{
    public InkViewModel()
    {
        SelectedColor = InkColors[0];
    }

    public IReadOnlyList<InkColor> InkColors { get; } =
    [
        new("Black", Color.FromArgb("#000000")),
        new("Blue", Color.FromArgb("#1F4FBF")),
        new("Navy", Color.FromArgb("#1E3A5F")),
        new("Red", Color.FromArgb("#C62828")),
        new("Green", Color.FromArgb("#2E7D32")),
    ];

    [ObservableProperty]
    public partial InkColor SelectedColor { get; set; }

    /// <summary>The narrowest ink, reached when drawing fast or pressing lightly with a pen.</summary>
    [ObservableProperty]
    public partial double MinWidth { get; set; } = 1.0;

    /// <summary>The widest ink, reached when drawing slowly or pressing firmly with a pen.</summary>
    [ObservableProperty]
    public partial double MaxWidth { get; set; } = 3.5;

    [ObservableProperty]
    public partial bool ShowSignatureLine { get; set; } = true;

    [ObservableProperty]
    public partial string Placeholder { get; set; } = "Sign here";

    [ObservableProperty]
    public partial string LastStroke { get; private set; } = "Draw a stroke to see how it was recorded.";

    [ObservableProperty]
    public partial string? LastStrokePressure { get; private set; }

    [RelayCommand]
    private void StrokeCompleted(SignatureStroke stroke)
    {
        var points = stroke.Points;
        var length = 0.0;
        for (var i = 1; i < points.Count; i++)
            length += Math.Sqrt(Square(points[i].X - points[i - 1].X) + Square(points[i].Y - points[i - 1].Y));

        var milliseconds = stroke.EndTime - stroke.StartTime;
        var speed = milliseconds > 0 ? length / milliseconds : 0;
        LastStroke = string.Format(
            CultureInfo.CurrentCulture, "{0}: {1} points in {2:0.00} s, {3:0.00} DIP/ms on average",
            stroke.Device, points.Count, milliseconds / 1000, speed);

        float lightest = float.MaxValue, firmest = float.MinValue;
        foreach (var point in points)
        {
            if (point.Pressure is { } pressure)
            {
                lightest = Math.Min(lightest, pressure);
                firmest = Math.Max(firmest, pressure);
            }
        }

        LastStrokePressure = lightest <= firmest
            ? string.Format(CultureInfo.CurrentCulture, "Pen pressure from {0:0.00} to {1:0.00}", lightest, firmest)
            : "No pressure: only a pen reports it.";
    }

    private static double Square(double value) => value * value;
}
