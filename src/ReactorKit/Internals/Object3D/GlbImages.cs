using System.Text.Json;
using SkiaSharp;

namespace Reactor.Kit.Internals.Object3D;

static class GlbImages
{
    const int MaxCount = 8;
    const int MaxEdge = 2048;
    const int SkipEdge = 4096;

    internal static int Slot(Cache cache, JsonElement root, JsonElement primitive, ReadOnlySpan<byte> bin, string? directory)
    {
        var imageIndex = ImageIndex(root, primitive);
        if (imageIndex < 0)
            return -1;
        if (cache.Slots.TryGetValue(imageIndex, out var slot))
            return slot;
        if (cache.Images.Count >= MaxCount)
            return cache.Remember(imageIndex, -1);

        var image = DecodeImage(root, bin, directory, imageIndex);
        return cache.Remember(imageIndex, image is null ? -1 : cache.Add(image));
    }

    static int ImageIndex(JsonElement root, JsonElement primitive)
    {
        if (!primitive.TryGetProperty("material", out var materialIndex))
            return -1;
        if (!root.TryGetProperty("materials", out var materials))
            return -1;

        var materialAt = materialIndex.GetInt32();
        if ((uint)materialAt >= (uint)materials.GetArrayLength())
            return -1;

        var material = materials[materialAt];
        if (!material.TryGetProperty("pbrMetallicRoughness", out var pbr))
            return -1;
        if (!pbr.TryGetProperty("baseColorTexture", out var info))
            return -1;
        if (info.TryGetProperty("texCoord", out var set) && set.GetInt32() != 0)
            return -1;
        if (!info.TryGetProperty("index", out var textureIndex))
            return -1;
        if (!root.TryGetProperty("textures", out var textures))
            return -1;

        var textureAt = textureIndex.GetInt32();
        if ((uint)textureAt >= (uint)textures.GetArrayLength())
            return -1;
        if (!textures[textureAt].TryGetProperty("source", out var source))
            return -1;

        return source.GetInt32();
    }

    static MeshImage? DecodeImage(JsonElement root, ReadOnlySpan<byte> bin, string? directory, int imageIndex)
    {
        if (!root.TryGetProperty("images", out var images))
            return null;
        if ((uint)imageIndex >= (uint)images.GetArrayLength())
            return null;

        var encoded = ImageBytes(root, images[imageIndex], bin, directory);
        return encoded is null ? null : Decode(encoded);
    }

    static byte[]? ImageBytes(JsonElement root, JsonElement image, ReadOnlySpan<byte> bin, string? directory)
    {
        if (image.TryGetProperty("bufferView", out var viewIndex))
            return ViewBytes(root, bin, viewIndex.GetInt32());
        if (!image.TryGetProperty("uri", out var uri))
            return null;

        return UriBytes(directory, uri.GetString());
    }

    static byte[]? ViewBytes(JsonElement root, ReadOnlySpan<byte> bin, int index)
    {
        if (!root.TryGetProperty("bufferViews", out var views))
            return null;
        if ((uint)index >= (uint)views.GetArrayLength())
            return null;

        var view = views[index];
        var buffer = view.TryGetProperty("buffer", out var bufferIndex) ? bufferIndex.GetInt32() : 0;
        if (buffer != 0)
            return null;

        var offset = view.TryGetProperty("byteOffset", out var byteOffset) ? byteOffset.GetInt32() : 0;
        var length = view.TryGetProperty("byteLength", out var byteLength) ? byteLength.GetInt32() : 0;
        if (length <= 0 || offset < 0 || offset + length > bin.Length)
            return null;

        return bin.Slice(offset, length).ToArray();
    }

    static byte[]? UriBytes(string? directory, string? uri)
    {
        if (string.IsNullOrEmpty(uri))
            return null;

        try
        {
            if (uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                return DataUri(uri);
            if (directory is null || uri.Contains(':', StringComparison.Ordinal))
                return null;

            var relative = Uri.UnescapeDataString(uri);
            if (Path.IsPathRooted(relative))
                return null;

            var full = Path.GetFullPath(Path.Combine(directory, relative));
            return File.Exists(full) ? File.ReadAllBytes(full) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException
            or UriFormatException or ArgumentException)
        {
            return null;
        }
    }

    static byte[]? DataUri(string uri)
    {
        var comma = uri.IndexOf(',', StringComparison.Ordinal);
        if (comma < 0 || !uri.AsSpan(5, comma - 5).Contains("base64", StringComparison.OrdinalIgnoreCase))
            return null;

        return Convert.FromBase64String(uri[(comma + 1)..]);
    }

    // A bad image leaves that group white. It does not drop the mesh.
    static MeshImage? Decode(byte[] encoded)
    {
        try
        {
            var bounds = SKBitmap.DecodeBounds(encoded);
            if (bounds.Width <= 0 || bounds.Height <= 0 || bounds.Width > SkipEdge || bounds.Height > SkipEdge)
                return null;

            using var decoded = SKBitmap.Decode(encoded);
            if (decoded is null)
                return null;

            var edge = Math.Max(decoded.Width, decoded.Height);
            var scale = edge > MaxEdge ? MaxEdge / (float)edge : 1f;
            var width = Math.Max(1, (int)MathF.Round(decoded.Width * scale));
            var height = Math.Max(1, (int)MathF.Round(decoded.Height * scale));
            var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            using var bitmap = new SKBitmap(info);
            if (!decoded.ScalePixels(bitmap, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None)))
                return null;

            return Copy(bitmap);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or IOException or FormatException)
        {
            return null;
        }
    }

    static MeshImage? Copy(SKBitmap bitmap)
    {
        var tight = bitmap.Width * 4;
        var row = bitmap.RowBytes;
        var raw = bitmap.GetPixelSpan();
        if (tight <= 0 || row < tight || raw.Length < ((bitmap.Height - 1) * row) + tight)
            return null;

        var pixels = new byte[bitmap.Height * tight];
        if (row == tight)
        {
            raw[..pixels.Length].CopyTo(pixels);
        }
        else
        {
            for (var y = 0; y < bitmap.Height; y++)
                raw.Slice(y * row, tight).CopyTo(pixels.AsSpan(y * tight));
        }

        return new MeshImage(bitmap.Width, bitmap.Height, pixels);
    }

    internal sealed class Cache
    {
        public Dictionary<int, int> Slots { get; } = [];

        public List<MeshImage> Images { get; } = [];

        public int Add(MeshImage image)
        {
            var slot = Images.Count;
            Images.Add(image);
            return slot;
        }

        public int Remember(int imageIndex, int slot)
        {
            Slots[imageIndex] = slot;
            return slot;
        }
    }
}
