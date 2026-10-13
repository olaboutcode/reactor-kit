using System.Numerics;
using Reactor.Kit.Internals.Object3D;

namespace Reactor.Kit.ObjectViewTokens;

/// <summary>
/// One rigid triangle mesh. The view copies the arrays it is given.
/// Positions are xyz triplets. Front faces are counter-clockwise.
/// </summary>
public sealed class Mesh
{
    internal const int MaxVertices = 500_000;
    internal const int MaxIndices = 1_500_000;
    internal const int FloatsPerVertex = 8;
    internal const int VertexStride = FloatsPerVertex * sizeof(float);

    Mesh(float[] interleaved, int[] indices, MeshGroup[] groups, MeshImage[] images, Vector3 center, float radius)
    {
        Interleaved = interleaved;
        Indices = indices;
        Groups = groups;
        Images = images;
        Center = center;
        Radius = radius;
    }

    internal float[] Interleaved { get; }

    internal int[] Indices { get; }

    internal MeshGroup[] Groups { get; }

    internal MeshImage[] Images { get; }

    internal Vector3 Center { get; }

    internal float Radius { get; }

    internal int VertexCount
        => Interleaved.Length / FloatsPerVertex;

    internal bool IsEmpty
        => Indices.Length == 0;

    internal static Mesh Empty { get; } = new([], [], [], [], Vector3.Zero, 0f);

    /// <summary>
    /// Triangles with flat normals. <paramref name="positions"/> is xyz. <paramref name="indices"/> is three indices per triangle.
    /// </summary>
    public static Mesh Triangles(float[] positions, int[] indices)
        => From(positions, normals: null, indices, groupColor: Vector3.One);

    /// <summary>
    /// Triangles with one normal per vertex. Both arrays are xyz, and the normal array matches the position count.
    /// </summary>
    public static Mesh Triangles(float[] positions, float[] normals, int[] indices)
        => From(positions, normals, indices, groupColor: Vector3.One);

    /// <summary>
    /// A sphere of <paramref name="radius"/> with outward normals.
    /// <paramref name="slices"/> runs around the equator and <paramref name="stacks"/> runs from pole to pole.
    /// </summary>
    public static Mesh Sphere(float radius, int slices, int stacks)
    {
        if (radius <= 0f || slices < 3 || stacks < 2)
            return Empty;

        var columns = slices + 1;
        var rows = stacks + 1;
        var vertexCount = columns * rows;
        var indexCount = stacks * slices * 6;
        if (vertexCount > MaxVertices || indexCount > MaxIndices)
            return Empty;

        var interleaved = new float[vertexCount * FloatsPerVertex];
        var indices = new int[indexCount];
        WriteSphere(interleaved, radius, slices, stacks, columns);
        WriteSphereIndices(indices, slices, stacks, columns);
        return new Mesh(interleaved, indices, [new MeshGroup(0, indices.Length, Vector3.One)], [], Vector3.Zero, radius);
    }

    /// <summary>
    /// A cube centered on the origin. <paramref name="size"/> is the edge length.
    /// </summary>
    public static Mesh Box(float size)
    {
        if (size <= 0f)
            return Empty;

        var half = size * 0.5f;
        var interleaved = new float[24 * FloatsPerVertex];
        var indices = new int[36];
        var vertex = 0;
        var index = 0;
        AddFace(interleaved, indices, ref vertex, ref index, new Vector3(0f, 0f, 1f),
            new Vector3(-half, -half, half), new Vector3(half, -half, half), new Vector3(half, half, half), new Vector3(-half, half, half));
        AddFace(interleaved, indices, ref vertex, ref index, new Vector3(0f, 0f, -1f),
            new Vector3(half, -half, -half), new Vector3(-half, -half, -half), new Vector3(-half, half, -half), new Vector3(half, half, -half));
        AddFace(interleaved, indices, ref vertex, ref index, new Vector3(0f, 1f, 0f),
            new Vector3(-half, half, half), new Vector3(half, half, half), new Vector3(half, half, -half), new Vector3(-half, half, -half));
        AddFace(interleaved, indices, ref vertex, ref index, new Vector3(0f, -1f, 0f),
            new Vector3(-half, -half, -half), new Vector3(half, -half, -half), new Vector3(half, -half, half), new Vector3(-half, -half, half));
        AddFace(interleaved, indices, ref vertex, ref index, new Vector3(1f, 0f, 0f),
            new Vector3(half, -half, half), new Vector3(half, -half, -half), new Vector3(half, half, -half), new Vector3(half, half, half));
        AddFace(interleaved, indices, ref vertex, ref index, new Vector3(-1f, 0f, 0f),
            new Vector3(-half, -half, -half), new Vector3(-half, -half, half), new Vector3(-half, half, half), new Vector3(-half, half, -half));

        return new Mesh(interleaved, indices, [new MeshGroup(0, indices.Length, Vector3.One)], [], Vector3.Zero, half * MathF.Sqrt(3f));
    }

    internal static Mesh FromParts(float[] interleaved, int[] indices, MeshGroup[] groups, MeshImage[]? images = null)
    {
        if (interleaved.Length == 0 || indices.Length == 0 || groups.Length == 0)
            return Empty;
        if (interleaved.Length % FloatsPerVertex != 0)
            return Empty;
        if (interleaved.Length / FloatsPerVertex > MaxVertices || indices.Length > MaxIndices)
            return Empty;

        var min = new Vector3(float.PositiveInfinity);
        var max = new Vector3(float.NegativeInfinity);
        for (var i = 0; i < interleaved.Length; i += FloatsPerVertex)
        {
            var point = new Vector3(interleaved[i], interleaved[i + 1], interleaved[i + 2]);
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }

        var center = (min + max) * 0.5f;
        var radius = Vector3.Distance(center, max);
        return new Mesh(interleaved, indices, groups, images ?? [], center, radius);
    }

    static Mesh From(float[] positions, float[]? normals, int[] indices, Vector3 groupColor)
    {
        ArgumentNullException.ThrowIfNull(positions);
        ArgumentNullException.ThrowIfNull(indices);
        if (positions.Length % 3 != 0 || indices.Length % 3 != 0 || indices.Length == 0)
            return Empty;

        var vertexCount = positions.Length / 3;
        if (normals is not null && normals.Length != positions.Length)
            return Empty;
        if (vertexCount > MaxVertices || indices.Length > MaxIndices)
            return Empty;

        for (var i = 0; i < indices.Length; i++)
        {
            if ((uint)indices[i] >= (uint)vertexCount)
                return Empty;
        }

        if (normals is null)
            return Flat(positions, indices, groupColor);

        var interleaved = new float[vertexCount * FloatsPerVertex];
        for (var i = 0; i < vertexCount; i++)
        {
            var source = i * 3;
            var slot = i * FloatsPerVertex;
            interleaved[slot] = positions[source];
            interleaved[slot + 1] = positions[source + 1];
            interleaved[slot + 2] = positions[source + 2];
            interleaved[slot + 3] = normals[source];
            interleaved[slot + 4] = normals[source + 1];
            interleaved[slot + 5] = normals[source + 2];
        }

        return FromParts(interleaved, indices.ToArray(), [new MeshGroup(0, indices.Length, groupColor)]);
    }

    static Mesh Flat(float[] positions, int[] indices, Vector3 groupColor)
    {
        var triangleCount = indices.Length / 3;
        if (triangleCount > MaxVertices / 3)
            return Empty;

        var interleaved = new float[triangleCount * 3 * FloatsPerVertex];
        var flatIndices = new int[triangleCount * 3];
        var vertex = 0;
        for (var i = 0; i < indices.Length; i += 3)
        {
            var a = Read(positions, indices[i]);
            var b = Read(positions, indices[i + 1]);
            var c = Read(positions, indices[i + 2]);
            var normal = Vector3.Cross(b - a, c - a);
            normal = normal.LengthSquared() < 1e-12f ? Vector3.UnitY : Vector3.Normalize(normal);
            flatIndices[vertex] = Append(interleaved, ref vertex, a, normal);
            flatIndices[vertex] = Append(interleaved, ref vertex, b, normal);
            flatIndices[vertex] = Append(interleaved, ref vertex, c, normal);
        }

        return FromParts(interleaved, flatIndices, [new MeshGroup(0, flatIndices.Length, groupColor)]);
    }

    static void WriteSphere(float[] interleaved, float radius, int slices, int stacks, int columns)
    {
        var vertex = 0;
        for (var stack = 0; stack <= stacks; stack++)
        {
            var phi = MathF.PI * stack / stacks;
            var y = MathF.Cos(phi);
            var ring = MathF.Sin(phi);
            for (var slice = 0; slice <= slices; slice++)
            {
                var theta = MathF.PI * 2f * slice / slices;
                var x = ring * MathF.Cos(theta);
                var z = ring * MathF.Sin(theta);
                var slot = vertex * FloatsPerVertex;
                interleaved[slot] = x * radius;
                interleaved[slot + 1] = y * radius;
                interleaved[slot + 2] = z * radius;
                interleaved[slot + 3] = x;
                interleaved[slot + 4] = y;
                interleaved[slot + 5] = z;
                vertex++;
            }
        }
    }

    static void WriteSphereIndices(int[] indices, int slices, int stacks, int columns)
    {
        var index = 0;
        for (var stack = 0; stack < stacks; stack++)
        {
            for (var slice = 0; slice < slices; slice++)
            {
                var i0 = (stack * columns) + slice;
                var i1 = i0 + 1;
                var i2 = i0 + columns;
                var i3 = i2 + 1;
                // Counter-clockwise from outside. The camera-facing shell is culled otherwise.
                indices[index++] = i0;
                indices[index++] = i1;
                indices[index++] = i2;
                indices[index++] = i1;
                indices[index++] = i3;
                indices[index++] = i2;
            }
        }
    }

    static void AddFace(
        float[] interleaved,
        int[] indices,
        ref int vertex,
        ref int index,
        Vector3 normal,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 d)
    {
        var i0 = Append(interleaved, ref vertex, a, normal);
        var i1 = Append(interleaved, ref vertex, b, normal);
        var i2 = Append(interleaved, ref vertex, c, normal);
        var i3 = Append(interleaved, ref vertex, d, normal);
        indices[index++] = i0;
        indices[index++] = i1;
        indices[index++] = i2;
        indices[index++] = i0;
        indices[index++] = i2;
        indices[index++] = i3;
    }

    static int Append(float[] interleaved, ref int vertex, Vector3 position, Vector3 normal)
    {
        var index = vertex;
        var slot = vertex * FloatsPerVertex;
        interleaved[slot] = position.X;
        interleaved[slot + 1] = position.Y;
        interleaved[slot + 2] = position.Z;
        interleaved[slot + 3] = normal.X;
        interleaved[slot + 4] = normal.Y;
        interleaved[slot + 5] = normal.Z;
        vertex++;
        return index;
    }

    static Vector3 Read(float[] positions, int index)
    {
        var source = index * 3;
        return new Vector3(positions[source], positions[source + 1], positions[source + 2]);
    }
}
