using System.Numerics;

namespace Reactor.Kit.Internals.Object3D;

/// <summary>
/// Packs a row-vector <see cref="Matrix4x4"/> so a column-vector shader can multiply <c>matrix * position</c>.
/// Field order is column-major of the transpose, which is what Metal, GLES, and HLSL read.
/// </summary>
static class MatrixPack
{
    public static void Store(Matrix4x4 rowVector, Span<float> destination)
    {
        destination[0] = rowVector.M11;
        destination[1] = rowVector.M12;
        destination[2] = rowVector.M13;
        destination[3] = rowVector.M14;
        destination[4] = rowVector.M21;
        destination[5] = rowVector.M22;
        destination[6] = rowVector.M23;
        destination[7] = rowVector.M24;
        destination[8] = rowVector.M31;
        destination[9] = rowVector.M32;
        destination[10] = rowVector.M33;
        destination[11] = rowVector.M34;
        destination[12] = rowVector.M41;
        destination[13] = rowVector.M42;
        destination[14] = rowVector.M43;
        destination[15] = rowVector.M44;
    }
}
