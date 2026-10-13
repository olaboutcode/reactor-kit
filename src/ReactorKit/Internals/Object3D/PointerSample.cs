using System.Runtime.InteropServices;

namespace Reactor.Kit.Internals.Object3D;

[StructLayout(LayoutKind.Auto)]
readonly struct PointerSample(int id, float x, float y)
{
    public int Id { get; } = id;

    public float X { get; } = x;

    public float Y { get; } = y;
}
