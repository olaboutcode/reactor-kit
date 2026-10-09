namespace Reactor.Kit.Glaze;

/// <summary>
/// Shared acrylic recipe. Platform blur is only the backlight.
/// Tint, noise, and stroke use the same numbers on every OS.
/// </summary>
internal static class GlazeTokens
{
    public const float BlurRadius = 8f;
    public const float TintOpacity = 0.28f;
    public const float NoiseOpacity = 0.015f;
    public const float FallbackTintOpacity = 0.76f;

    /// <summary>Windows 11 Mica light base, not pure white.</summary>
    public static readonly Color LightTint = Color.FromRgba(243, 243, 243, 255);

    public static readonly Color Stroke = Color.FromRgba(255, 255, 255, 46);

    /// <summary>
    /// A white wash at <paramref name="opacity"/> covers the blur. A dark wash does not.
    /// Ease high-luminance tints so both stay glassy. iOS already veils with the material,
    /// so the extra tint there is thinner.
    /// </summary>
    public static float WashOpacity(Color tint, double opacity, bool overSystemMaterial)
    {
        var luminance = (0.2126 * tint.Red) + (0.7152 * tint.Green) + (0.0722 * tint.Blue);
        var ease = overSystemMaterial ? 0.82 : 0.58;
        var keep = 1d - (luminance * ease);
        return (float)Math.Clamp(opacity * keep, 0d, 1d);
    }
}
