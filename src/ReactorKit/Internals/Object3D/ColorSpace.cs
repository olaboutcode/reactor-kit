namespace Reactor.Kit.Internals.Object3D;

static class ColorSpace
{
    public static (float R, float G, float B) Linear(float r, float g, float b)
        => (Channel(r), Channel(g), Channel(b));

    public static (float R, float G, float B, float A) LinearPremultiplied(float r, float g, float b, float a)
    {
        var (lr, lg, lb) = Linear(r, g, b);
        var alpha = Math.Clamp(a, 0f, 1f);
        return (lr * alpha, lg * alpha, lb * alpha, alpha);
    }

    static float Channel(float value)
    {
        var channel = Math.Clamp(value, 0f, 1f);
        return channel <= 0.04045f
            ? channel / 12.92f
            : MathF.Pow((channel + 0.055f) / 1.055f, 2.4f);
    }
}
