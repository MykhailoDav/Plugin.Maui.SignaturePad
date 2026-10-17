using System.Globalization;
using System.Text;

namespace Plugin.Maui.SignaturePad.Tests.Data;

public class SignatureJsonTests
{
    private const string GoldenJson =
        """{"format":"plugin.maui.signaturepad","version":1,"width":320,"height":180,"strokes":[""" +
        """{"device":"pen","color":"#000000FF","minWidth":1,"maxWidth":3.5,"x":[10.5,11.25],"y":[20,21.5],"t":[0,4.17],"p":[0.5,0.52]},""" +
        """{"device":"touch","color":"#1E88E5FF","minWidth":1,"maxWidth":3.5,"x":[1,2],"y":[3,4],"t":[10,20]}]}""";

    private static SignatureData GoldenData() => new(
    [
        new SignatureStroke([new(10.5f, 20f, 0, 0.5f), new(11.25f, 21.5f, 4.17, 0.52f)], Colors.Black, 1, 3.5f, SignatureInputDevice.Pen),
        new SignatureStroke([new(1, 3, 10), new(2, 4, 20)], Color.FromRgba(0x1E, 0x88, 0xE5, 0xFF), 1, 3.5f, SignatureInputDevice.Touch),
    ], new SizeF(320, 180));

    [Fact]
    public void Write_MatchesGoldenJson() =>
        Assert.Equal(GoldenJson, GoldenData().ToJson());

    [Fact]
    public void Read_GoldenJson_GivesGoldenData() =>
        Assert.Equal(GoldenData(), SignatureData.FromJson(GoldenJson));

    [Fact]
    public void Write_IsCultureInvariant()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            Assert.Equal(GoldenJson, GoldenData().ToJson());
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void RoundTrip_IsExact_ForArbitraryValues()
    {
        var random = new Random(42);
        var strokes = new List<SignatureStroke>();
        foreach (var device in Enum.GetValues<SignatureInputDevice>())
        {
            var points = new SignaturePoint[200];
            for (var i = 0; i < points.Length; i++)
            {
                float? pressure = device == SignatureInputDevice.Pen ? (float)random.NextDouble() : null;
                points[i] = new SignaturePoint(
                    (float)(random.NextDouble() * 1000), (float)(random.NextDouble() * 1000), random.NextDouble() * 100000, pressure);
            }

            strokes.Add(new SignatureStroke(points, new Color((float)random.NextDouble(), 0.25f, 1f, 0.5f), 0.7f, 4.25f, device));
        }

        var data = new SignatureData(strokes, new SizeF(412.33f, 280.5f));

        Assert.Equal(data, SignatureData.FromJson(data.ToJson()));
    }

    [Fact]
    public void RoundTrip_KeepsMixedPressure()
    {
        var data = new SignatureData(
            [new SignatureStroke([new(0, 0, 0, 0.5f), new(1, 1, 1), new(2, 2, 2, 1f)], Colors.Black, 1, 2, SignatureInputDevice.Unknown)],
            new SizeF(10, 10));

        var json = data.ToJson();

        Assert.Contains("\"p\":[0.5,null,1]", json, StringComparison.Ordinal);
        Assert.Equal(data, SignatureData.FromJson(json));
    }

    [Fact]
    public void RoundTrip_Empty() =>
        Assert.Equal(SignatureData.Empty, SignatureData.FromJson(SignatureData.Empty.ToJson()));

    [Fact]
    public void Read_IgnoresUnknownProperties_AndAcceptsAnyFieldOrder()
    {
        var json = """
            {
              "strokes": [ { "t": [0, 4.17], "extra": {"nested": [1, 2, {"deep": true}]}, "p": [0.5, 0.52],
                             "x": [10.5, 11.25], "y": [20, 21.5], "maxWidth": 3.5, "minWidth": 1, "color": "#000000FF", "device": "pen" },
                           { "device": "touch", "color": "#1E88E5FF", "minWidth": 1, "maxWidth": 3.5, "x": [1, 2], "y": [3, 4], "t": [10, 20] } ],
              "height": 180, "width": 320, "comment": "made by hand", "version": 1, "format": "plugin.maui.signaturepad"
            }
            """;

        Assert.Equal(GoldenData(), SignatureData.FromJson(json));
    }

    public static TheoryData<string, string> InvalidJson => new()
    {
        { "", "JSON" },
        { "[]", "object" },
        { "null", "object" },
        { """{"format":"plugin.maui.signaturepad","version":1,"width":1,"height":1,"strokes":[]""", "JSON" },
        { """{"format":"other","version":1,"width":1,"height":1,"strokes":[]}""", "format" },
        { """{"version":1,"width":1,"height":1,"strokes":[]}""", "format" },
        { """{"format":"plugin.maui.signaturepad","version":2,"width":1,"height":1,"strokes":[]}""", "version" },
        { """{"format":"plugin.maui.signaturepad","version":"1","width":1,"height":1,"strokes":[]}""", "version" },
        { """{"format":"plugin.maui.signaturepad","width":1,"height":1,"strokes":[]}""", "version" },
        { """{"format":"plugin.maui.signaturepad","version":1,"height":1,"strokes":[]}""", "width" },
        { """{"format":"plugin.maui.signaturepad","version":1,"width":-1,"height":1,"strokes":[]}""", "width" },
        { """{"format":"plugin.maui.signaturepad","version":1,"width":1e999,"height":1,"strokes":[]}""", "width" },
        { """{"format":"plugin.maui.signaturepad","version":1,"width":1,"height":1}""", "strokes" },
        { """{"format":"plugin.maui.signaturepad","version":1,"width":1,"height":1,"strokes":{}}""", "strokes" },
        { """{"format":"plugin.maui.signaturepad","version":1,"width":1,"height":1,"strokes":["stroke"]}""", "strokes[0]" },
        { Stroke("""  "x":[1],"y":[1],"t":[0]"""), "strokes[0].device" },
        { Stroke(""" "device":"stylus","x":[1],"y":[1],"t":[0]"""), "strokes[0].device" },
        { Stroke(""" "device":"pen","color":"black","x":[1],"y":[1],"t":[0]"""), "strokes[0].color" },
        { Stroke(""" "device":"pen","x":[1],"y":[1]"""), "strokes[0].t" },
        { Stroke(""" "device":"pen","x":[1,2],"y":[1],"t":[0,1]"""), "strokes[0]" },
        { Stroke(""" "device":"pen","x":[1],"y":[1],"t":[0],"p":[0.5,0.5]"""), "strokes[0]" },
        { Stroke(""" "device":"pen","x":[],"y":[],"t":[]"""), "strokes[0]" },
        { Stroke(""" "device":"pen","x":[1,"2"],"y":[1,2],"t":[0,1]"""), "strokes[0].x" },
        { Stroke(""" "device":"pen","x":[1],"y":[1],"t":[0],"p":[1.5]"""), "strokes[0]" },
        { Stroke(""" "device":"pen","x":[1],"y":[1],"t":[0],"p":["a"]"""), "strokes[0].p" },
        { Stroke(""" "device":"pen","x":[1],"y":[1],"t":[0]""", minWidth: "0"), "strokes[0]" },
        { Stroke(""" "device":"pen","x":[1],"y":[1],"t":[0]""", minWidth: "\"1\""), "strokes[0].minWidth" },
        { """{"format":1,"version":1,"width":1,"height":1,"strokes":[]}""", "format" },
        { """{"format":"plugin.maui.signaturepad","version":1.5,"width":1,"height":1,"strokes":[]}""", "version" },
        { """{"format":"plugin.maui.signaturepad","version":1,"width":"1","height":1,"strokes":[]}""", "width" },
        { Stroke(""" "device":1,"x":[1],"y":[1],"t":[0]"""), "strokes[0].device" },
        { Stroke(""" "device":"pen","color":1,"x":[1],"y":[1],"t":[0]"""), "strokes[0].color" },
        { Stroke(""" "device":"pen","x":[1],"y":[1],"t":[0,1]"""), "strokes[0]" },
        { Stroke(""" "device":"pen","x":[1],"y":[1],"t":[1e999]"""), "strokes[0].t" },
        { Stroke(""" "device":"pen","x":[1],"y":[1],"t":["0"]"""), "strokes[0].t" },
        { Stroke(""" "device":"pen","x":1,"y":[1],"t":[0]"""), "strokes[0].x" },
        { """{"format":"plugin.maui.signaturepad","version":1,"width":1,"height":1,"strokes":[],}""", "JSON" },
        { """{"format":"plugin.maui.signaturepad","version":1,"width":NaN,"height":1,"strokes":[]}""", "JSON" },
        { """{"format":"plugin.maui.signaturepad","version":1,"width":1,"height":1,"strokes":[]} trailing""", "JSON" },
    };

    private static string Stroke(string fields, string minWidth = "1") =>
        """{"format":"plugin.maui.signaturepad","version":1,"width":1,"height":1,"strokes":[{"color":"#000000FF","minWidth":""" +
        minWidth + ""","maxWidth":2,""" + fields + "}]}";

    [Theory]
    [MemberData(nameof(InvalidJson))]
    public void Read_InvalidJson_ThrowsFormatException_NamingThePlace(string json, string place)
    {
        var exception = Assert.Throws<FormatException>(() => SignatureData.FromJson(json));

        Assert.Contains(place, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_Null_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => SignatureData.FromJson(null!));

    [Fact]
    public void Read_TooManyPoints_ThrowsBeforeBuildingStrokes()
    {
        var points = string.Join(',', Enumerable.Repeat("1", SignatureJson.MaxTotalPoints + 1));
        var json = Stroke($"\"device\":\"pen\",\"x\":[{points}],\"y\":[{points}],\"t\":[{points}]");

        var exception = Assert.Throws<FormatException>(() => SignatureData.FromJson(json));
        Assert.Contains("points", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_TooManyStrokes_Throws()
    {
        var stroke = """{"device":"pen","color":"#000000FF","minWidth":1,"maxWidth":2,"x":[1],"y":[1],"t":[0]}""";
        var json = new StringBuilder("""{"format":"plugin.maui.signaturepad","version":1,"width":1,"height":1,"strokes":[""")
            .AppendJoin(',', Enumerable.Repeat(stroke, SignatureJson.MaxStrokes + 1))
            .Append("]}")
            .ToString();

        var exception = Assert.Throws<FormatException>(() => SignatureData.FromJson(json));
        Assert.Contains("strokes", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_TooDeep_Throws()
    {
        var json = """{"format":"plugin.maui.signaturepad","version":1,"width":1,"height":1,"strokes":[],"extra":""" +
            new string('[', 100) + new string(']', 100) + "}";

        Assert.Throws<FormatException>(() => SignatureData.FromJson(json));
    }
}
