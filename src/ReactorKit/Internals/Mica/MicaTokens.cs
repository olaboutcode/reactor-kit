using Reactor.Kit.Internals.Glaze;

namespace Reactor.Kit.Internals.Mica;

/// <summary>
/// Painted mica. The base color, noise, and stroke match glaze. The wash is translucent.
/// Lengths are dp. A negative shade opacity means the bottom alpha is derived from the tint.
/// The hairline is one physical pixel, shared by iOS and Android.
/// </summary>
internal static class MicaTokens
{
    public const float ShadeRgb = 0.94f;
    public const float ShadeAlpha = 0.88f;
    public const double UnsetOpacity = -1d;

    public const float TintOpacity = 0.5f;
    public const float NoiseOpacity = GlazeTokens.NoiseOpacity;

    public static Color TintColor => GlazeTokens.LightTint;

    public static Color Stroke => GlazeTokens.Stroke;

    /// <summary>
    /// One physical pixel, in dp. <c>1 / density</c>. @2x is 0.5. @3x is about 0.33.
    /// </summary>
    public static double HairlineThickness
    {
        get
        {
            var density = DeviceDisplay.MainDisplayInfo.Density;
            return density > 0 ? 1d / density : 1d;
        }
    }
}
