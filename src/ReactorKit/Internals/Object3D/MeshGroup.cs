using System.Numerics;
using System.Runtime.InteropServices;

namespace Reactor.Kit.Internals.Object3D;

[StructLayout(LayoutKind.Auto)]
readonly record struct MeshGroup(int IndexOffset, int IndexCount, Vector3 Color, int Texture = -1);
