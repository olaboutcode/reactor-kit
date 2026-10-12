using System.Numerics;

namespace Reactor.Kit.Internals.Object3D;

static class OrbitMath
{
    public const float FovDegrees = 40f;

    public static float Radians(float degrees)
        => degrees * (MathF.PI / 180f);

    public static Matrix4x4 Projection(float aspect, float distance, float radius, bool openGlClip)
    {
        if (aspect <= 0f)
            aspect = 1f;

        var dist = MathF.Max(distance, 0.001f);
        var reach = MathF.Max(radius, 0.001f);
        var near = MathF.Max(0.01f, dist - (reach * 2f));
        var far = MathF.Max(near + 0.1f, dist + (reach * 2f));
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(Radians(FovDegrees), aspect, near, far);
        if (!openGlClip)
            return projection;

        // CreatePerspectiveFieldOfView maps depth to 0..1. OpenGL ES wants -1..1.
        var remap = new Matrix4x4(
            1f, 0f, 0f, 0f,
            0f, 1f, 0f, 0f,
            0f, 0f, 2f, 0f,
            0f, 0f, -1f, 1f);
        return projection * remap;
    }

    public static Vector3 Eye(Vector3 target, float yawDegrees, float pitchDegrees, float distance)
    {
        var yaw = Radians(yawDegrees);
        var pitch = Radians(pitchDegrees);
        var flat = MathF.Cos(pitch);
        return target + (distance * new Vector3(flat * MathF.Sin(yaw), MathF.Sin(pitch), flat * MathF.Cos(yaw)));
    }

    public static float FrameDistance(float radius)
    {
        var safe = MathF.Max(radius, 0.001f);
        return safe / MathF.Sin(Radians(FovDegrees) * 0.5f) * 1.2f;
    }
}
