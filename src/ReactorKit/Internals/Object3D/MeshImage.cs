namespace Reactor.Kit.Internals.Object3D;

sealed class MeshImage(int width, int height, byte[] rgba)
{
    public int Width { get; } = width;

    public int Height { get; } = height;

    public byte[] Rgba { get; } = rgba;
}
