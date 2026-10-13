using System.Numerics;

namespace Reactor.Kit.Internals.Object3D;

static class ShadeUniforms
{
    public static float[] Pack(ObjectDraw draw, MeshGroup group)
    {
        var data = new float[44];
        MatrixPack.Store(draw.ModelViewProjection, data);
        MatrixPack.Store(draw.ModelView, data.AsSpan(16));
        data[32] = draw.Tint.X;
        data[33] = draw.Tint.Y;
        data[34] = draw.Tint.Z;
        data[36] = group.Color.X;
        data[37] = group.Color.Y;
        data[38] = group.Color.Z;
        data[39] = group.Alpha;
        data[40] = group.AlphaCutoff;
        data[41] = group.Masked ? 1f : 0f;
        data[42] = group.DoubleSided ? 1f : 0f;
        return data;
    }
}
