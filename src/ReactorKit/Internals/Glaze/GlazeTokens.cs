namespace Reactor.Kit.Internals.Glaze;

/// <summary>
/// Acrylic recipe. Tint, noise, and stroke are the same on every OS.
/// Blur and the material fill are platform-aware, because an 8dp gaussian
/// is not the same backlight as the system ultra-thin material.
/// </summary>
internal static class GlazeTokens
{
    public const float TintOpacity = 0.28f;
    public const float NoiseOpacity = 0.015f;
    public const float FallbackTintOpacity = 0.76f;

    /// <summary>
    /// iOS and Mac Catalyst use any positive radius only as an on/off for the system material.
    /// Android blurs a snapshot by this many dp. Eight dp there stays liquid glass.
    /// </summary>
#if ANDROID
    public const float BlurRadius = 32f;
#else
    public const float BlurRadius = 8f;
#endif

    /// <summary>
    /// True where a material already veils the backlight, so the shared wash stays thin.
    /// On Android the material fill below is that veil.
    /// </summary>
#if IOS || MACCATALYST || ANDROID
    public const bool OverSystemMaterial = true;
#else
    public const bool OverSystemMaterial = false;
#endif

    /// <summary>
    /// Android stand-in for the milk inside the system material, as a fraction of opaque.
    /// Night stays 1.25× light. Apple does not publish the ultra-thin fill, so this is matched by eye.
    /// Other platforms paint that milk themselves.
    /// </summary>
#if ANDROID
    public const float LightMaterialOpacity = 0.44f;
    public const float NightMaterialOpacity = 0.55f;
#else
    public const float LightMaterialOpacity = 0f;
    public const float NightMaterialOpacity = 0f;
#endif

    /// <summary>Windows 11 Mica light base, not pure white.</summary>
    public static readonly Color LightTint = Color.FromRgba(243, 243, 243, 255);

    public static readonly Color Stroke = Color.FromRgba(255, 255, 255, 46);

    /// <summary>
    /// A white wash at <paramref name="opacity"/> covers the blur. A dark wash does not.
    /// Ease high-luminance tints so both stay glassy. A material backlight already veils,
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
