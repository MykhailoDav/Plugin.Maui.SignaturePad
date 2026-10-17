using System.Buffers;
using System.Text;
using System.Text.Json;

namespace Plugin.Maui.SignaturePad;

/// <summary>
/// Reads and writes version 1 of the signature JSON format:
/// <code>
/// {"format":"plugin.maui.signaturepad","version":1,"width":320,"height":180,
///  "strokes":[{"device":"pen","color":"#000000FF","minWidth":1,"maxWidth":3.5,"x":[...],"y":[...],"t":[...],"p":[...]}]}
/// </code>
/// Points are stored by column, which keeps the text small. <c>p</c> is left out when no point has pressure, and holds
/// <c>null</c> for the points without it. Numbers use their shortest round-trip form, so reading what was written gives
/// back an equal <see cref="SignatureData"/>. The JSON is handled token by token, without reflection, so the code is
/// safe for trimming and Native AOT.
/// </summary>
internal static class SignatureJson
{
    internal const string FormatName = "plugin.maui.signaturepad";
    internal const int Version = 1;

    /// <summary>The input is untrusted, so the reader stops at these sizes instead of allocating without bounds.</summary>
    internal const int MaxTotalPoints = 100_000;

    /// <inheritdoc cref="MaxTotalPoints"/>
    internal const int MaxStrokes = 10_000;

    // Indexed by SignatureInputDevice, so writing and reading need no switch.
    private static readonly string[] DeviceNames = ["unknown", "touch", "pen", "mouse"];

    private static readonly JsonReaderOptions ReaderOptions = new()
    {
        CommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false,
        MaxDepth = 32,
    };

    public static string Write(SignatureData data)
    {
        var buffer = new ArrayBufferWriter<byte>(256 + (data.PointCount * 32));
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("format", FormatName);
            writer.WriteNumber("version", Version);
            writer.WriteNumber("width", data.CanvasSize.Width);
            writer.WriteNumber("height", data.CanvasSize.Height);
            writer.WriteStartArray("strokes");
            for (var i = 0; i < data.Strokes.Count; i++)
                WriteStroke(writer, data.Strokes[i]);
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public static SignatureData Read(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            return Read(Encoding.UTF8.GetBytes(json));
        }
        catch (JsonException exception)
        {
            throw new FormatException("The signature is not valid JSON: " + exception.Message, exception);
        }
    }

    private static void WriteStroke(Utf8JsonWriter writer, SignatureStroke stroke)
    {
        var points = stroke.PointSpan;

        writer.WriteStartObject();
        writer.WriteString("device", DeviceNames[(int)stroke.Device]);
        writer.WriteString("color", ColorBytes.ToHex(stroke.Color));
        writer.WriteNumber("minWidth", stroke.MinWidth);
        writer.WriteNumber("maxWidth", stroke.MaxWidth);

        writer.WriteStartArray("x");
        foreach (ref readonly var point in points)
            writer.WriteNumberValue(point.X);
        writer.WriteEndArray();

        writer.WriteStartArray("y");
        foreach (ref readonly var point in points)
            writer.WriteNumberValue(point.Y);
        writer.WriteEndArray();

        writer.WriteStartArray("t");
        foreach (ref readonly var point in points)
            writer.WriteNumberValue(point.Time);
        writer.WriteEndArray();

        if (HasPressure(points))
        {
            writer.WriteStartArray("p");
            foreach (ref readonly var point in points)
            {
                if (point.Pressure is { } pressure)
                    writer.WriteNumberValue(pressure);
                else
                    writer.WriteNullValue();
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    private static bool HasPressure(ReadOnlySpan<SignaturePoint> points)
    {
        foreach (ref readonly var point in points)
        {
            if (point.Pressure.HasValue)
                return true;
        }

        return false;
    }

    // The reader works on the whole input as a final block, so it throws on incomplete JSON. Read() can therefore only
    // return false after the root object, which is why the loops below do not check its result.
    private static SignatureData Read(ReadOnlySpan<byte> utf8)
    {
        var reader = new Utf8JsonReader(utf8, ReaderOptions);
        reader.Read();
        if (reader.TokenType != JsonTokenType.StartObject)
            throw Error("", "The signature JSON must be an object.");

        // Fields can come in any order, but the format and version decide how to read the rest, so check them first.
        CheckFormatAndVersion(reader);

        float? width = null;
        float? height = null;
        List<SignatureStroke>? strokes = null;
        while (true)
        {
            reader.Read();
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.ValueTextEquals("width"))
                width = ReadSize(ref reader, "width");
            else if (reader.ValueTextEquals("height"))
                height = ReadSize(ref reader, "height");
            else if (reader.ValueTextEquals("strokes"))
                strokes = ReadStrokes(ref reader);
            else
                reader.Skip();
        }

        // Throws when anything but whitespace follows the root object.
        reader.Read();

        var canvasSize = new SizeF(Required(width, "width"), Required(height, "height"));
        return new SignatureData(Required(strokes, "strokes"), canvasSize);
    }

    private static void CheckFormatAndVersion(Utf8JsonReader reader)
    {
        string? format = null;
        int? version = null;
        while (true)
        {
            reader.Read();
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.ValueTextEquals("format"))
            {
                reader.Read();
                format = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
            }
            else if (reader.ValueTextEquals("version"))
            {
                reader.Read();
                version = reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var value) ? value : -1;
            }
            else
            {
                reader.Skip();
            }
        }

        if (format != FormatName)
            throw Error("format", $"expected \"{FormatName}\"; this is not signature data.");
        if (version != Version)
            throw Error("version", $"expected {Version}; this version of the format is not supported.");
    }

    private static float ReadSize(ref Utf8JsonReader reader, string name)
    {
        var value = ReadSingle(ref reader, name);
        if (value < 0)
            throw Error(name, "must not be negative.");
        return value;
    }

    private static List<SignatureStroke> ReadStrokes(ref Utf8JsonReader reader)
    {
        reader.Read();
        if (reader.TokenType != JsonTokenType.StartArray)
            throw Error("strokes", "expected an array of strokes.");

        var strokes = new List<SignatureStroke>();
        var totalPoints = 0;
        while (true)
        {
            reader.Read();
            if (reader.TokenType == JsonTokenType.EndArray)
                return strokes;

            if (strokes.Count == MaxStrokes)
                throw Error("strokes", $"a signature can have at most {MaxStrokes} strokes.");

            var stroke = ReadStroke(ref reader, strokes.Count, MaxTotalPoints - totalPoints);
            totalPoints += stroke.Points.Count;
            strokes.Add(stroke);
        }
    }

    private static SignatureStroke ReadStroke(ref Utf8JsonReader reader, int index, int pointBudget)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw Error(Place(index), "expected a stroke object.");

        SignatureInputDevice? device = null;
        Color? color = null;
        float? minWidth = null;
        float? maxWidth = null;
        List<float>? xs = null;
        List<float>? ys = null;
        List<double>? ts = null;
        List<float?>? ps = null;
        while (true)
        {
            reader.Read();
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.ValueTextEquals("device"))
                device = ReadDevice(ref reader, index);
            else if (reader.ValueTextEquals("color"))
                color = ReadColor(ref reader, index);
            else if (reader.ValueTextEquals("minWidth"))
                minWidth = ReadSingle(ref reader, Place(index, "minWidth"));
            else if (reader.ValueTextEquals("maxWidth"))
                maxWidth = ReadSingle(ref reader, Place(index, "maxWidth"));
            else if (reader.ValueTextEquals("x"))
                xs = ReadSingles(ref reader, index, "x", pointBudget);
            else if (reader.ValueTextEquals("y"))
                ys = ReadSingles(ref reader, index, "y", pointBudget);
            else if (reader.ValueTextEquals("t"))
                ts = ReadTimes(ref reader, index, pointBudget);
            else if (reader.ValueTextEquals("p"))
                ps = ReadPressures(ref reader, index, pointBudget);
            else
                reader.Skip();
        }

        var x = Required(xs, Place(index, "x"));
        var y = Required(ys, Place(index, "y"));
        var t = Required(ts, Place(index, "t"));
        if (y.Count != x.Count || t.Count != x.Count || (ps is not null && ps.Count != x.Count))
            throw Error(Place(index), "x, y, t and p must have the same number of values.");

        var points = new SignaturePoint[x.Count];
        for (var i = 0; i < points.Length; i++)
            points[i] = new SignaturePoint(x[i], y[i], t[i], ps?[i]);

        try
        {
            return new SignatureStroke(
                points,
                Required(color, Place(index, "color")),
                Required(minWidth, Place(index, "minWidth")),
                Required(maxWidth, Place(index, "maxWidth")),
                Required(device, Place(index, "device")));
        }
        catch (ArgumentException exception)
        {
            throw new FormatException($"{Place(index)}: {exception.Message}", exception);
        }
    }

    private static SignatureInputDevice ReadDevice(ref Utf8JsonReader reader, int index)
    {
        reader.Read();
        var device = reader.TokenType == JsonTokenType.String ? Array.IndexOf(DeviceNames, reader.GetString()) : -1;
        if (device < 0)
            throw Error(Place(index, "device"), "expected one of " + string.Join(", ", DeviceNames) + ".");
        return (SignatureInputDevice)device;
    }

    private static Color ReadColor(ref Utf8JsonReader reader, int index)
    {
        reader.Read();
        if (reader.TokenType != JsonTokenType.String || !ColorBytes.TryParseHex(reader.GetString(), out var color))
            throw Error(Place(index, "color"), "expected a color in #RRGGBBAA form.");
        return color;
    }

    private static float ReadSingle(ref Utf8JsonReader reader, string place)
    {
        reader.Read();
        if (!TryGetFinite(ref reader, out float value))
            throw Error(place, "expected a finite number.");
        return value;
    }

    private static List<float> ReadSingles(ref Utf8JsonReader reader, int index, string name, int budget)
    {
        StartArray(ref reader, index, name);
        var values = new List<float>();
        while (true)
        {
            reader.Read();
            if (reader.TokenType == JsonTokenType.EndArray)
                return values;

            CheckBudget(values.Count, budget, index, name);
            if (!TryGetFinite(ref reader, out float value))
                throw Error(Place(index, name), "expected an array of finite numbers.");
            values.Add(value);
        }
    }

    private static List<double> ReadTimes(ref Utf8JsonReader reader, int index, int budget)
    {
        StartArray(ref reader, index, "t");
        var values = new List<double>();
        while (true)
        {
            reader.Read();
            if (reader.TokenType == JsonTokenType.EndArray)
                return values;

            CheckBudget(values.Count, budget, index, "t");
            if (!TryGetFinite(ref reader, out double value))
                throw Error(Place(index, "t"), "expected an array of finite numbers.");
            values.Add(value);
        }
    }

    private static List<float?> ReadPressures(ref Utf8JsonReader reader, int index, int budget)
    {
        StartArray(ref reader, index, "p");
        var values = new List<float?>();
        while (true)
        {
            reader.Read();
            if (reader.TokenType == JsonTokenType.EndArray)
                return values;

            CheckBudget(values.Count, budget, index, "p");
            if (reader.TokenType == JsonTokenType.Null)
                values.Add(null);
            else if (TryGetFinite(ref reader, out float value))
                values.Add(value);
            else
                throw Error(Place(index, "p"), "expected an array of numbers or nulls.");
        }
    }

    // TryGetSingle and TryGetDouble return true with infinity for numbers out of range such as 1e999, so these check
    // IsFinite themselves.
    private static bool TryGetFinite(ref Utf8JsonReader reader, out float value)
    {
        value = reader.TokenType == JsonTokenType.Number ? reader.GetSingle() : float.NaN;
        return float.IsFinite(value);
    }

    private static bool TryGetFinite(ref Utf8JsonReader reader, out double value)
    {
        value = reader.TokenType == JsonTokenType.Number ? reader.GetDouble() : double.NaN;
        return double.IsFinite(value);
    }

    private static void StartArray(ref Utf8JsonReader reader, int index, string name)
    {
        reader.Read();
        if (reader.TokenType != JsonTokenType.StartArray)
            throw Error(Place(index, name), "expected an array.");
    }

    private static void CheckBudget(int count, int budget, int index, string name)
    {
        if (count == budget)
            throw Error(Place(index, name), $"a signature can have at most {MaxTotalPoints} points.");
    }

    private static T Required<T>(T? value, string place) where T : class =>
        value ?? throw Error(place, "is missing.");

    private static T Required<T>(T? value, string place) where T : struct =>
        value ?? throw Error(place, "is missing.");

    private static string Place(int index) => $"strokes[{index}]";

    private static string Place(int index, string field) => $"strokes[{index}].{field}";

    private static FormatException Error(string place, string message) =>
        new(place.Length == 0 ? message : $"{place}: {message}");
}
