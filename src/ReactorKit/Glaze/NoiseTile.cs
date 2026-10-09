namespace Reactor.Kit.Glaze;

/// <summary>64×64 gray grain. The same seed on every platform.</summary>
static class NoiseTile
{
    public const int Size = 64;

    public static byte[] Rgba { get; } = Create();

    static byte[] Create()
    {
        var random = new Random(7);
        var pixels = new byte[Size * Size * 4];
        for (var i = 0; i < pixels.Length; i += 4)
        {
            var grain = (byte)random.Next(170, 256);
            pixels[i] = grain;
            pixels[i + 1] = grain;
            pixels[i + 2] = grain;
            pixels[i + 3] = (byte)random.Next(18, 70);
        }

        return pixels;
    }
}
