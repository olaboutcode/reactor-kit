namespace Reactor.Kit.Glaze;

/// <summary>Fine, low-contrast dust. Stretched or tiled, it should not read as speckles.</summary>
internal static class NoiseTile
{
    public const int Size = 128;

    public static byte[] Rgba { get; } = Create();

    static byte[] Create()
    {
        var random = new Random(7);
        var pixels = new byte[Size * Size * 4];
        for (var i = 0; i < pixels.Length; i += 4)
        {
            var grain = (byte)random.Next(236, 256);
            pixels[i] = grain;
            pixels[i + 1] = grain;
            pixels[i + 2] = grain;
            pixels[i + 3] = (byte)random.Next(6, 16);
        }

        return pixels;
    }
}
