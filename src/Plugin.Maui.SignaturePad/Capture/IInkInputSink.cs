using Plugin.Maui.SignaturePad.Ink;

namespace Plugin.Maui.SignaturePad.Capture;

/// <summary>
/// Where the native views send input. The platform code only translates native events into samples; every decision
/// about strokes is made behind this interface, in shared code that the unit tests cover.
/// </summary>
internal interface IInkInputSink
{
    /// <summary>Whether input should be handled at all. Native views can skip the work of reading samples when not.</summary>
    bool IsInputEnabled { get; }

    /// <summary>A finger, pen or mouse button went down.</summary>
    void BeginStroke(in InkSample sample);

    /// <summary>It moved; <paramref name="samples"/> holds every sample since the last call, oldest first.</summary>
    void ContinueStroke(ReadOnlySpan<InkSample> samples);

    /// <summary>It lifted, at <paramref name="sample"/>.</summary>
    void EndStroke(in InkSample sample);

    /// <summary>The platform took the input away (a system gesture, another view, the app going to the background).</summary>
    void CancelStroke();
}
