namespace Plugin.Maui.SignaturePad;

internal static class ExportValidation
{
    public static float Padding(float value) =>
        float.IsFinite(value) && value >= 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "The padding must be a finite number that is not negative.");
}
