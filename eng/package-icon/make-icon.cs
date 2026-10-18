#:project ../../src/Plugin.Maui.SignaturePad/Plugin.Maui.SignaturePad.csproj
#:property TargetFramework=net10.0

// Draws the package icon with the library's own ink and SVG export. From the repository root:
//
//   dotnet run eng/package-icon/make-icon.cs
//   rsvg-convert -w 256 -h 256 eng/package-icon/nuget.svg -o nuget.png
//
// The first command writes eng/package-icon/nuget.svg; the second turns it into the nuget.png the package ships.
// The mark is the signature flourish of the sample app's icon, written with a natural speed profile (slow in the turns,
// fast on the straights), so its width varies as the ink of a real signature does.

using System.Numerics;
using System.Runtime.CompilerServices;
using Plugin.Maui.SignaturePad;

const float Canvas = 128;
const float Scale = Canvas / 456f * 1.18f;

// The flourish of samples/.../Resources/AppIcon/appiconfg.svg as cubic Béziers in its 456-unit box.
Vector2[][] curves =
[
    [new(110, 262), new(130, 205), new(152, 156), new(175, 156)],
    [new(175, 156), new(200, 156), new(181, 238), new(160, 260)],
    [new(160, 260), new(146.5f, 275), new(165, 278.5f), new(183.5f, 263.7f)],
    [new(183.5f, 263.7f), new(204.5f, 246.4f), new(217, 218), new(230.5f, 218)],
    [new(230.5f, 218), new(246.5f, 218), new(234.2f, 260), new(250.2f, 260)],
    [new(250.2f, 260), new(265, 260), new(282.2f, 233), new(333, 228)],
];

var dense = new List<Vector2>();
foreach (var c in curves)
{
    for (var i = dense.Count == 0 ? 0 : 1; i <= 200; i++)
    {
        var t = i / 200f;
        var u = 1 - t;
        dense.Add(((u * u * u) * c[0] + (3 * u * u * t) * c[1] + (3 * u * t * t) * c[2] + (t * t * t) * c[3]) * Scale + Offset());
    }
}

// The speed falls in the turns, measured by how much the direction changes over a short window.
var points = new List<SignaturePoint>();
double time = 0;
var lastKept = dense[0];
points.Add(new SignaturePoint(lastKept.X, lastKept.Y, 0));
for (var i = 1; i < dense.Count; i++)
{
    var a = dense[Math.Max(0, i - 6)];
    var b = dense[i];
    var c = dense[Math.Min(dense.Count - 1, i + 6)];
    var turn = 0.0;
    if (Vector2.Distance(a, b) > 1e-3f && Vector2.Distance(b, c) > 1e-3f)
        turn = 1 - Math.Clamp(Vector2.Dot(Vector2.Normalize(b - a), Vector2.Normalize(c - b)), -1, 1);   // 0 straight, 2 back
    var speed = 0.12 + (1.1 * Math.Exp(-6 * turn));   // DIP per ms
    time += Vector2.Distance(dense[i - 1], b) / speed;
    if (Vector2.Distance(lastKept, b) >= 1.2f || i == dense.Count - 1)
    {
        points.Add(new SignaturePoint(MathF.Round(b.X, 2), MathF.Round(b.Y, 2), Math.Round(time, 2)));
        lastKept = b;
    }
}

var y = (316 * Scale) + Offset().Y;
var line = new SignatureStroke(
    [new SignaturePoint((110 * Scale) + Offset().X, y, time + 200), new SignaturePoint((346 * Scale) + Offset().X, y, time + 400)],
    Color.FromRgba(255, 255, 255, 140), 2.2f, 2.2f, SignatureInputDevice.Touch);
var flourish = new SignatureStroke(points, Colors.White, 2.4f, 7.5f, SignatureInputDevice.Touch);
var signature = new SignatureData([flourish, line], new SizeF(Canvas, Canvas));

File.WriteAllText(Path.Combine(Path.GetDirectoryName(ScriptPath())!, "nuget.svg"), signature.ToSvg(new SignatureSvgExportOptions
{
    CropToContent = false,
    BackgroundColor = Color.FromArgb("#1E3A5F"),
}));
Console.WriteLine($"{points.Count} points, {time:0} ms");

static string ScriptPath([CallerFilePath] string path = "") => path;

// Moves the middle of the mark (x 110..346, y 156..316 in the 456 box) to the middle of the canvas.
static Vector2 Offset() => new Vector2(Canvas / 2, Canvas / 2) - (new Vector2((110 + 346) / 2f, (156 + 316) / 2f) * Scale);
