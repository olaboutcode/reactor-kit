using SkiaSharp;

namespace Reactor.Kit.RichText;

/// <summary>Draws characters the Skia fonts on this device cannot paint.</summary>
internal static partial class SystemText
{
    internal delegate bool MeasureDelegate(
        string text,
        float size,
        out float width,
        out float ascent,
        out float descent);

    internal delegate bool DrawDelegate(
        string text,
        float size,
        SKColor color,
        out SKImage? image,
        out float width,
        out float ascent,
        out float descent);

    static SystemText() => Register();

    static partial void Register();

    internal static MeasureDelegate? Measure { get; private set; }

    internal static DrawDelegate? Draw { get; private set; }

    internal static bool CanDraw
        => Measure is not null && Draw is not null;
}
