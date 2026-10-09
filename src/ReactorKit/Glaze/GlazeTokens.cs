namespace Reactor.Kit.Glaze;

/// <summary>
/// Shared acrylic recipe. Platform blur is only the backlight.
/// Tint, noise, and stroke use the same numbers on every OS.
/// </summary>
public static class GlazeTokens
{
    public const float BlurRadius = 24f;
    public const float TintOpacity = 0.72f;
    public const float NoiseOpacity = 0.04f;
    public const float StrokeOpacity = 0.18f;
    public const float FallbackTintOpacity = 0.88f;

    public static readonly Color LightTint = Color.FromRgba(255, 255, 255, 255);
    public static readonly Color DarkTint = Color.FromRgba(28, 28, 30, 255);
    public static readonly Color Stroke = Color.FromRgba(255, 255, 255, 46);
}
