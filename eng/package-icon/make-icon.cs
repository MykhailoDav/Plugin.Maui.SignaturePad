#:project ../../src/Plugin.Maui.SignaturePad/Plugin.Maui.SignaturePad.csproj
#:property TargetFramework=net10.0

// Draws the logo with the library's own ink and SVG export, and writes every file that shows it. From the repository
// root:
//
//   dotnet run eng/package-icon/make-icon.cs
//   rsvg-convert -w 256 -h 256 eng/package-icon/nuget.svg -o nuget.png
//
// The first command writes eng/package-icon/nuget.svg and the sample app's icon and splash screen; the second turns the
// package icon into the nuget.png the package ships. The mark is a signature flourish over a line, written with a natural
// speed profile (slow in the turns, fast on the straights), so its width varies as the ink of a real signature does.

using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Plugin.Maui.SignaturePad;

const float Canvas = 128;
const string GradientFrom = "#10B981";
const string GradientTo = "#065F46";
const float LineOpacity = 0.6f;

var root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(ScriptPath())!, "..", ".."));
var sampleResources = Path.Combine(root, "samples", "Plugin.Maui.SignaturePad.Sample", "Resources");

var (flourish, line) = DrawMark(scale: 1.18f);

// The package icon: the mark on a rounded square with the emerald gradient.
File.WriteAllText(Path.Combine(root, "eng", "package-icon", "nuget.svg"), Svg(128,
    Gradient() + "<rect width=\"128\" height=\"128\" rx=\"26\" fill=\"url(#g)\"/>" + Mark(flourish, line, "#FFFFFF")));

// The app icon: the platforms shape the background themselves, and the mark keeps inside the safe area of adaptive icons.
File.WriteAllText(Path.Combine(sampleResources, "AppIcon", "appicon.svg"), Svg(456,
    Gradient() + "<rect width=\"456\" height=\"456\" fill=\"url(#g)\"/>"));
var centred = Centred(flourish, line, width: 0.56f * 456, size: 456);
File.WriteAllText(Path.Combine(sampleResources, "AppIcon", "appiconfg.svg"), Svg(456, centred));
File.WriteAllText(Path.Combine(sampleResources, "Splash", "splash.svg"), Svg(456, centred));

Console.WriteLine("Wrote the package icon, the app icon and the splash screen.");

static string Svg(int size, string body) =>
    $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{size}\" height=\"{size}\" viewBox=\"0 0 {size} {size}\">{body}</svg>\n";

static string Gradient() =>
    $"<defs><linearGradient id=\"g\" x1=\"0\" y1=\"0\" x2=\"1\" y2=\"1\"><stop offset=\"0\" stop-color=\"{GradientFrom}\"/>" +
    $"<stop offset=\"1\" stop-color=\"{GradientTo}\"/></linearGradient></defs>";

static string Mark(SignatureStroke flourish, SignatureStroke line, string ink) =>
    $"<path d=\"{PathData(line)}\" fill=\"{ink}\" fill-opacity=\"{LineOpacity.ToString(CultureInfo.InvariantCulture)}\"/>" +
    $"<path d=\"{PathData(flourish)}\" fill=\"{ink}\"/>";

// The mark scaled to `width` and centred in a square of `size`.
static string Centred(SignatureStroke flourish, SignatureStroke line, float width, float size)
{
    var bounds = new SignatureData([flourish, line], new Microsoft.Maui.Graphics.SizeF(Canvas, Canvas)).Bounds;
    var scale = width / bounds.Width;
    var x = (size / 2) - ((bounds.X + (bounds.Width / 2)) * scale);
    var y = (size / 2) - ((bounds.Y + (bounds.Height / 2)) * scale);
    var transform = string.Create(CultureInfo.InvariantCulture, $"translate({x:0.##} {y:0.##}) scale({scale:0.####})");
    return $"<g transform=\"{transform}\">{Mark(flourish, line, "#FFFFFF")}</g>";
}

// The outline of a stroke, exactly as the library's SVG export writes it.
static string PathData(SignatureStroke stroke)
{
    var svg = new SignatureData([stroke], new Microsoft.Maui.Graphics.SizeF(Canvas, Canvas))
        .ToSvg(new SignatureSvgExportOptions { CropToContent = false });
    return Regex.Match(svg, "<path d=\"([^\"]+)\"").Groups[1].Value;
}

// The flourish and the line, in a 128-unit canvas.
static (SignatureStroke Flourish, SignatureStroke Line) DrawMark(float scale)
{
    // The flourish as cubic Béziers in a 456-unit box.
    Vector2[][] curves =
    [
        [new(110, 262), new(130, 205), new(152, 156), new(175, 156)],
        [new(175, 156), new(200, 156), new(181, 238), new(160, 260)],
        [new(160, 260), new(146.5f, 275), new(165, 278.5f), new(183.5f, 263.7f)],
        [new(183.5f, 263.7f), new(204.5f, 246.4f), new(217, 218), new(230.5f, 218)],
        [new(230.5f, 218), new(246.5f, 218), new(234.2f, 260), new(250.2f, 260)],
        [new(250.2f, 260), new(265, 260), new(282.2f, 233), new(333, 228)],
    ];

    // Moves the middle of the mark (x 110..346, y 156..316 in the 456 box) to the middle of the canvas.
    var unit = Canvas / 456f * scale;
    Vector2 Map(Vector2 p) => new Vector2(Canvas / 2, Canvas / 2) + ((p - new Vector2(228, 236)) * unit);

    var dense = new List<Vector2>();
    foreach (var c in curves)
    {
        for (var i = dense.Count == 0 ? 0 : 1; i <= 200; i++)
        {
            var t = i / 200f;
            var u = 1 - t;
            dense.Add(Map((u * u * u * c[0]) + (3 * u * u * t * c[1]) + (3 * u * t * t * c[2]) + (t * t * t * c[3])));
        }
    }

    // The speed falls in the turns, measured by how much the direction changes over a short window.
    var points = new List<SignaturePoint> { new(dense[0].X, dense[0].Y, 0) };
    double time = 0;
    var kept = dense[0];
    for (var i = 1; i < dense.Count; i++)
    {
        var a = dense[Math.Max(0, i - 6)];
        var b = dense[i];
        var c = dense[Math.Min(dense.Count - 1, i + 6)];
        var turn = 0.0;
        if (Vector2.Distance(a, b) > 1e-3f && Vector2.Distance(b, c) > 1e-3f)
            turn = 1 - Math.Clamp(Vector2.Dot(Vector2.Normalize(b - a), Vector2.Normalize(c - b)), -1, 1);   // 0 straight, 2 back
        time += Vector2.Distance(dense[i - 1], b) / (0.12 + (1.1 * Math.Exp(-6 * turn)));   // DIP per ms
        if (Vector2.Distance(kept, b) >= 1.2f || i == dense.Count - 1)
        {
            points.Add(new SignaturePoint(MathF.Round(b.X, 2), MathF.Round(b.Y, 2), Math.Round(time, 2)));
            kept = b;
        }
    }

    var flourish = new SignatureStroke(points, Microsoft.Maui.Graphics.Colors.White, 2.4f, 7.5f, SignatureInputDevice.Touch);
    var start = Map(new Vector2(110, 316));
    var end = Map(new Vector2(346, 316));
    var line = new SignatureStroke(
        [new SignaturePoint(start.X, start.Y, time + 200), new SignaturePoint(end.X, end.Y, time + 400)],
        Microsoft.Maui.Graphics.Colors.White, 2.2f, 2.2f, SignatureInputDevice.Touch);
    return (flourish, line);
}

static string ScriptPath([CallerFilePath] string path = "") => path;
