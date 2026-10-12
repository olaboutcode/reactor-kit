using System.Buffers.Binary;
using System.Numerics;
using System.Text.Json;
using Reactor.Kit.ObjectViewTokens;

namespace Reactor.Kit.Internals.Object3D;

static class GlbReader
{
    const uint Magic = 0x46546C67;
    const uint JsonChunk = 0x4E4F534A;
    const uint BinChunk = 0x004E4942;
    const int Triangles = 4;
    const int FloatType = 5126;
    const int UnsignedShort = 5123;
    const int UnsignedInt = 5125;

    public static Mesh Read(string path)
    {
        try
        {
            return ReadBytes(File.ReadAllBytes(path));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException
            or JsonException or InvalidDataException or OverflowException or IndexOutOfRangeException
            or FormatException or InvalidOperationException or NotSupportedException)
        {
            return Mesh.Empty;
        }
    }

    static Mesh ReadBytes(byte[] bytes)
    {
        if (bytes.Length < 20 || BinaryPrimitives.ReadUInt32LittleEndian(bytes) != Magic)
            return Mesh.Empty;
        if (BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4)) != 2)
            return Mesh.Empty;

        var jsonLength = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(12));
        var jsonType = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(16));
        if (jsonType != JsonChunk || 20 + jsonLength > bytes.Length)
            return Mesh.Empty;

        var binStart = 20 + jsonLength;
        ReadOnlySpan<byte> bin = [];
        if (binStart + 8 <= bytes.Length && BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(binStart + 4)) == BinChunk)
        {
            var binLength = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(binStart));
            var data = binStart + 8;
            if (data + binLength <= bytes.Length)
                bin = bytes.AsSpan(data, binLength);
        }

        using var document = JsonDocument.Parse(bytes.AsMemory(20, jsonLength));
        var root = document.RootElement;
        if (!root.TryGetProperty("nodes", out var nodes) || !root.TryGetProperty("meshes", out var meshes))
            return Mesh.Empty;

        var sceneNodes = SceneNodes(root);
        if (sceneNodes.ValueKind != JsonValueKind.Array)
            return Mesh.Empty;

        var buckets = new List<Bucket>();
        for (var i = 0; i < sceneNodes.GetArrayLength(); i++)
        {
            if (TakeFirst(root, nodes, meshes, bin, sceneNodes[i].GetInt32(), Matrix4x4.Identity, buckets))
                break;
        }

        return Combine(buckets);
    }

    static JsonElement SceneNodes(JsonElement root)
    {
        var scene = root.TryGetProperty("scene", out var sceneIndex) ? sceneIndex.GetInt32() : 0;
        if (!root.TryGetProperty("scenes", out var scenes) || scene < 0 || scene >= scenes.GetArrayLength())
            return default;
        return scenes[scene].TryGetProperty("nodes", out var nodes) ? nodes : default;
    }

    static bool TakeFirst(
        JsonElement root,
        JsonElement nodes,
        JsonElement meshes,
        ReadOnlySpan<byte> bin,
        int nodeIndex,
        Matrix4x4 parent,
        List<Bucket> buckets)
    {
        if (nodeIndex < 0 || nodeIndex >= nodes.GetArrayLength())
            return false;

        var node = nodes[nodeIndex];
        var world = Local(node) * parent;
        if (node.TryGetProperty("mesh", out var meshProperty))
        {
            ReadMesh(root, meshes, bin, meshProperty.GetInt32(), world, buckets);
            return true;
        }

        if (!node.TryGetProperty("children", out var children))
            return false;

        for (var i = 0; i < children.GetArrayLength(); i++)
        {
            if (TakeFirst(root, nodes, meshes, bin, children[i].GetInt32(), world, buckets))
                return true;
        }

        return false;
    }

    static void ReadMesh(
        JsonElement root,
        JsonElement meshes,
        ReadOnlySpan<byte> bin,
        int meshIndex,
        Matrix4x4 world,
        List<Bucket> buckets)
    {
        if (meshIndex < 0 || meshIndex >= meshes.GetArrayLength())
            return;
        if (!meshes[meshIndex].TryGetProperty("primitives", out var primitives))
            return;
        if (!root.TryGetProperty("accessors", out var accessors) || !root.TryGetProperty("bufferViews", out var views))
            return;

        Matrix4x4.Invert(world, out var inverse);
        var normalMatrix = Matrix4x4.Transpose(inverse);
        for (var i = 0; i < primitives.GetArrayLength(); i++)
            ReadPrimitive(root, primitives[i], accessors, views, bin, world, normalMatrix, buckets);
    }

    static void ReadPrimitive(
        JsonElement root,
        JsonElement primitive,
        JsonElement accessors,
        JsonElement views,
        ReadOnlySpan<byte> bin,
        Matrix4x4 world,
        Matrix4x4 normalMatrix,
        List<Bucket> buckets)
    {
        if (primitive.TryGetProperty("mode", out var mode) && mode.GetInt32() != Triangles)
            return;
        if (!primitive.TryGetProperty("attributes", out var attributes))
            return;
        if (!attributes.TryGetProperty("POSITION", out var positionAccessor))
            return;
        if (!TryVec3(accessors, views, bin, positionAccessor.GetInt32(), out var positions))
            return;

        for (var i = 0; i < positions.Length; i += 3)
        {
            var transformed = Vector3.Transform(new Vector3(positions[i], positions[i + 1], positions[i + 2]), world);
            positions[i] = transformed.X;
            positions[i + 1] = transformed.Y;
            positions[i + 2] = transformed.Z;
        }

        float[]? normals = null;
        if (attributes.TryGetProperty("NORMAL", out var normalAccessor)
            && TryVec3(accessors, views, bin, normalAccessor.GetInt32(), out var readNormals)
            && readNormals.Length == positions.Length)
        {
            normals = readNormals;
            for (var i = 0; i < normals.Length; i += 3)
            {
                var transformed = Vector3.Normalize(Vector3.TransformNormal(
                    new Vector3(normals[i], normals[i + 1], normals[i + 2]),
                    normalMatrix));
                normals[i] = transformed.X;
                normals[i + 1] = transformed.Y;
                normals[i + 2] = transformed.Z;
            }
        }

        var indices = primitive.TryGetProperty("indices", out var indexAccessor)
            ? ReadIndices(accessors, views, bin, indexAccessor.GetInt32())
            : Sequence(positions.Length / 3);
        if (indices is null || indices.Length < 3 || indices.Length % 3 != 0)
            return;

        var color = MaterialColor(root, primitive);
        var bucket = BucketFor(buckets, color);
        if (normals is null)
            AppendFlat(bucket, positions, indices);
        else
            AppendSmooth(bucket, positions, normals, indices);
    }

    static void AppendSmooth(Bucket bucket, float[] positions, float[] normals, int[] indices)
    {
        var count = positions.Length / 3;
        for (var i = 0; i < indices.Length; i++)
        {
            if ((uint)indices[i] >= (uint)count)
                return;
        }

        var start = bucket.Vertices.Count / 6;
        for (var i = 0; i < count; i++)
        {
            var source = i * 3;
            bucket.Vertices.Add(positions[source]);
            bucket.Vertices.Add(positions[source + 1]);
            bucket.Vertices.Add(positions[source + 2]);
            bucket.Vertices.Add(normals[source]);
            bucket.Vertices.Add(normals[source + 1]);
            bucket.Vertices.Add(normals[source + 2]);
        }

        for (var i = 0; i < indices.Length; i++)
            bucket.Indices.Add(start + indices[i]);
    }

    static void AppendFlat(Bucket bucket, float[] positions, int[] indices)
    {
        for (var i = 0; i < indices.Length; i += 3)
        {
            if (!TryCorner(positions, indices[i], out var a)
                || !TryCorner(positions, indices[i + 1], out var b)
                || !TryCorner(positions, indices[i + 2], out var c))
            {
                return;
            }

            var normal = Vector3.Cross(b - a, c - a);
            normal = normal.LengthSquared() < 1e-12f ? Vector3.UnitY : Vector3.Normalize(normal);
            AddCorner(bucket, a, normal);
            AddCorner(bucket, b, normal);
            AddCorner(bucket, c, normal);
        }
    }

    static void AddCorner(Bucket bucket, Vector3 position, Vector3 normal)
    {
        var index = bucket.Vertices.Count / 6;
        bucket.Vertices.Add(position.X);
        bucket.Vertices.Add(position.Y);
        bucket.Vertices.Add(position.Z);
        bucket.Vertices.Add(normal.X);
        bucket.Vertices.Add(normal.Y);
        bucket.Vertices.Add(normal.Z);
        bucket.Indices.Add(index);
    }

    static bool TryCorner(float[] positions, int index, out Vector3 corner)
    {
        corner = default;
        if ((uint)index >= (uint)(positions.Length / 3))
            return false;

        var source = index * 3;
        corner = new Vector3(positions[source], positions[source + 1], positions[source + 2]);
        return true;
    }

    static Bucket BucketFor(List<Bucket> buckets, Vector3 color)
    {
        var key = HashCode.Combine(
            BitConverter.SingleToInt32Bits(color.X),
            BitConverter.SingleToInt32Bits(color.Y),
            BitConverter.SingleToInt32Bits(color.Z));
        foreach (var bucket in buckets)
        {
            if (bucket.Key == key)
                return bucket;
        }

        var created = new Bucket(key, color);
        buckets.Add(created);
        return created;
    }

    static Mesh Combine(List<Bucket> buckets)
    {
        if (buckets.Count == 0)
            return Mesh.Empty;

        var vertexCount = 0;
        var indexCount = 0;
        foreach (var bucket in buckets)
        {
            vertexCount += bucket.Vertices.Count / 6;
            indexCount += bucket.Indices.Count;
        }

        if (vertexCount == 0 || indexCount == 0 || vertexCount > Mesh.MaxVertices || indexCount > Mesh.MaxIndices)
            return Mesh.Empty;

        var interleaved = new float[vertexCount * 6];
        var indices = new int[indexCount];
        var groups = new MeshGroup[buckets.Count];
        var vertex = 0;
        var index = 0;
        for (var i = 0; i < buckets.Count; i++)
        {
            var bucket = buckets[i];
            var baseVertex = vertex / 6;
            bucket.Vertices.CopyTo(interleaved, vertex);
            var indexStart = index;
            foreach (var value in bucket.Indices)
                indices[index++] = baseVertex + value;
            groups[i] = new MeshGroup(indexStart, bucket.Indices.Count, bucket.Color);
            vertex += bucket.Vertices.Count;
        }

        return Mesh.FromParts(interleaved, indices, groups);
    }

    static Vector3 MaterialColor(JsonElement root, JsonElement primitive)
    {
        if (!primitive.TryGetProperty("material", out var materialIndex))
            return Vector3.One;
        if (!root.TryGetProperty("materials", out var materials))
            return Vector3.One;

        var index = materialIndex.GetInt32();
        if (index < 0 || index >= materials.GetArrayLength())
            return Vector3.One;

        var material = materials[index];
        if (!material.TryGetProperty("pbrMetallicRoughness", out var pbr))
            return Vector3.One;
        if (!pbr.TryGetProperty("baseColorFactor", out var factor) || factor.GetArrayLength() < 3)
            return Vector3.One;

        return new Vector3(factor[0].GetSingle(), factor[1].GetSingle(), factor[2].GetSingle());
    }

    static Matrix4x4 Local(JsonElement node)
    {
        if (node.TryGetProperty("matrix", out var matrix) && matrix.GetArrayLength() == 16)
        {
            var m = new float[16];
            for (var i = 0; i < 16; i++)
                m[i] = matrix[i].GetSingle();

            return new Matrix4x4(
                m[0], m[4], m[8], m[12],
                m[1], m[5], m[9], m[13],
                m[2], m[6], m[10], m[14],
                m[3], m[7], m[11], m[15]);
        }

        var translation = Vec3(node, "translation", new Vector3(0f, 0f, 0f));
        var scale = Vec3(node, "scale", Vector3.One);
        var rotation = node.TryGetProperty("rotation", out var quaternion) && quaternion.GetArrayLength() >= 4
            ? new Quaternion(quaternion[0].GetSingle(), quaternion[1].GetSingle(), quaternion[2].GetSingle(), quaternion[3].GetSingle())
            : Quaternion.Identity;
        return Matrix4x4.CreateScale(scale)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(translation);
    }

    static Vector3 Vec3(JsonElement node, string name, Vector3 fallback)
    {
        if (!node.TryGetProperty(name, out var value) || value.GetArrayLength() < 3)
            return fallback;

        return new Vector3(value[0].GetSingle(), value[1].GetSingle(), value[2].GetSingle());
    }

    static bool TryVec3(JsonElement accessors, JsonElement views, ReadOnlySpan<byte> bin, int index, out float[] values)
    {
        values = [];
        if (!TryView(accessors, views, index, "VEC3", FloatType, out var offset, out var count, out var stride))
            return false;
        if (stride == 0)
            stride = 12;
        if (count <= 0 || offset < 0 || (offset + ((count - 1) * stride)) + 12 > bin.Length)
            return false;

        values = new float[count * 3];
        for (var i = 0; i < count; i++)
        {
            var start = offset + (i * stride);
            values[i * 3] = BinaryPrimitives.ReadSingleLittleEndian(bin.Slice(start, 4));
            values[(i * 3) + 1] = BinaryPrimitives.ReadSingleLittleEndian(bin.Slice(start + 4, 4));
            values[(i * 3) + 2] = BinaryPrimitives.ReadSingleLittleEndian(bin.Slice(start + 8, 4));
        }

        return true;
    }

    static int[]? ReadIndices(JsonElement accessors, JsonElement views, ReadOnlySpan<byte> bin, int index)
    {
        if (!TryIndex(accessors, index, out var accessor))
            return null;

        var component = accessor.TryGetProperty("componentType", out var componentType) ? componentType.GetInt32() : 0;
        if (component != UnsignedShort && component != UnsignedInt)
            return null;
        if (!TryView(accessors, views, index, "SCALAR", component, out var offset, out var count, out var stride))
            return null;

        var width = component == UnsignedShort ? 2 : 4;
        if (stride == 0)
            stride = width;
        if (count <= 0 || offset < 0 || (offset + ((count - 1) * stride)) + width > bin.Length)
            return null;

        var indices = new int[count];
        for (var i = 0; i < count; i++)
        {
            var start = offset + (i * stride);
            indices[i] = component == UnsignedShort
                ? BinaryPrimitives.ReadUInt16LittleEndian(bin.Slice(start, 2))
                : (int)BinaryPrimitives.ReadUInt32LittleEndian(bin.Slice(start, 4));
        }

        return indices;
    }

    static bool TryView(
        JsonElement accessors,
        JsonElement views,
        int index,
        string type,
        int componentType,
        out int offset,
        out int count,
        out int stride)
    {
        offset = 0;
        count = 0;
        stride = 0;
        if (!TryIndex(accessors, index, out var accessor))
            return false;
        if (!accessor.TryGetProperty("bufferView", out var viewIndex))
            return false;
        if (!TryIndex(views, viewIndex.GetInt32(), out var view))
            return false;
        if (accessor.TryGetProperty("type", out var typeName)
            && !string.Equals(typeName.GetString(), type, StringComparison.Ordinal))
        {
            return false;
        }
        if (accessor.TryGetProperty("componentType", out var component) && component.GetInt32() != componentType)
            return false;

        count = accessor.TryGetProperty("count", out var countValue) ? countValue.GetInt32() : 0;
        var viewOffset = view.TryGetProperty("byteOffset", out var viewByte) ? viewByte.GetInt32() : 0;
        var accessorOffset = accessor.TryGetProperty("byteOffset", out var accessorByte) ? accessorByte.GetInt32() : 0;
        offset = viewOffset + accessorOffset;
        stride = view.TryGetProperty("byteStride", out var strideValue) ? strideValue.GetInt32() : 0;
        return count > 0;
    }

    static bool TryIndex(JsonElement element, int index, out JsonElement value)
    {
        value = default;
        if (element.ValueKind != JsonValueKind.Array || index < 0 || index >= element.GetArrayLength())
            return false;

        value = element[index];
        return true;
    }

    static int[] Sequence(int count)
    {
        var indices = new int[count];
        for (var i = 0; i < count; i++)
            indices[i] = i;
        return indices;
    }

    sealed class Bucket(int key, Vector3 color)
    {
        public int Key { get; } = key;

        public Vector3 Color { get; } = color;

        public List<float> Vertices { get; } = [];

        public List<int> Indices { get; } = [];
    }
}
