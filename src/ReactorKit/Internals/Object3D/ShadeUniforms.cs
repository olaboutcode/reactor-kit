using System.Numerics;

namespace Reactor.Kit.Internals.Object3D;

static class ShadeUniforms
{
    public static float[] Pack(ObjectDraw draw, Vector3 group)
    {
        var data = new float[40];
        MatrixPack.Store(draw.ModelViewProjection, data);
        MatrixPack.Store(draw.ModelView, data.AsSpan(16));
        data[32] = draw.Tint.X;
        data[33] = draw.Tint.Y;
        data[34] = draw.Tint.Z;
        data[36] = group.X;
        data[37] = group.Y;
        data[38] = group.Z;
        return data;
    }
}
