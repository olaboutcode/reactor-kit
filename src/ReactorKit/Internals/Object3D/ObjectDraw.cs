using System.Numerics;
using Reactor.Kit.ObjectViewTokens;

namespace Reactor.Kit.Internals.Object3D;

readonly struct ObjectDraw
{
    public ObjectDraw(
        Mesh mesh,
        int generation,
        Matrix4x4 modelViewProjection,
        Matrix4x4 modelView,
        Vector3 tint,
        Vector4 clear,
        bool opaque,
        bool autoRotate)
    {
        Mesh = mesh;
        Generation = generation;
        ModelViewProjection = modelViewProjection;
        ModelView = modelView;
        Tint = tint;
        Clear = clear;
        Opaque = opaque;
        AutoRotate = autoRotate;
    }

    public Mesh Mesh { get; }

    public int Generation { get; }

    public Matrix4x4 ModelViewProjection { get; }

    public Matrix4x4 ModelView { get; }

    public Vector3 Tint { get; }

    public Vector4 Clear { get; }

    public bool Opaque { get; }

    public bool AutoRotate { get; }
}
