using Microsoft.Maui;
using Microsoft.Maui.Storage;
using SkiaSharp;

namespace Reactor.Kit.Internals.RichText;

/// <summary>
/// Opens a font the app registered.
/// Android returns the asset file name. Apple platforms return the PostScript name.
/// </summary>
static class FontFace
{
    static readonly Lock Gate = new();
    static readonly Dictionary<string, SKTypeface> Faces = new(StringComparer.Ordinal);

    // Skia may borrow the buffer behind a typeface. Keep each copy for the process.
    static readonly List<SKData> Retained = new();

    static readonly string[] PackageFolders = ["", "Fonts", "fonts"];

    public static SKTypeface Open(string? family)
    {
        if (string.IsNullOrWhiteSpace(family))
            return SKTypeface.Default;

        var ready = IPlatformApplication.Current?.Services is not null;
        lock (Gate)
        {
            if (Faces.TryGetValue(family, out var cached))
                return cached;

            var opened = Resolve(family);
            if (ready || !ReferenceEquals(opened, SKTypeface.Default))
                Faces[family] = opened;
            return opened;
        }
    }

    static SKTypeface Resolve(string family)
    {
        var registered = Registered(family);
        if (TryFile(registered, out var file))
            return file;
        if (OperatingSystem.IsAndroid() && TryPackage(registered, out var package))
            return package;
        if (TryFamily(registered, family, out var named))
            return named;
        if (TryFamily(family, family, out named))
            return named;
        return SKTypeface.Default;
    }

    static string? Registered(string family)
    {
        var services = IPlatformApplication.Current?.Services;
        if (services?.GetService(typeof(IFontRegistrar)) is not IFontRegistrar registrar)
            return null;
        return registrar.GetFont(family);
    }

    static bool TryFile(string? path, out SKTypeface face)
    {
        face = SKTypeface.Default;
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return false;

        try
        {
            return TryData(File.ReadAllBytes(path), out face);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    static bool TryPackage(string? registered, out SKTypeface face)
    {
        face = SKTypeface.Default;
        var name = FontFileName(registered);
        if (!IsFontFile(name))
            return false;

        foreach (var folder in PackageFolders)
        {
            var asset = folder.Length == 0 ? name! : Path.Combine(folder, name!);
            if (TryAsset(asset, out face))
                return true;
        }

        return false;
    }

    static bool TryAsset(string name, out SKTypeface face)
    {
        face = SKTypeface.Default;
        try
        {
            // Android completes this task before the call returns.
            using var stream = FileSystem.OpenAppPackageFileAsync(name).GetAwaiter().GetResult();
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            return TryData(buffer.ToArray(), out face);
        }
        catch (Exception ex) when (ex is FileNotFoundException
            or DirectoryNotFoundException
            or IOException
            or InvalidOperationException
            or UnauthorizedAccessException
            or NotSupportedException
            or PlatformNotSupportedException
            or ArgumentException)
        {
            return false;
        }
    }

    static bool TryData(byte[] bytes, out SKTypeface face)
    {
        face = SKTypeface.Default;
        if (bytes.Length == 0)
            return false;

        var data = SKData.CreateCopy(bytes);
        var opened = SKTypeface.FromData(data);
        if (opened is null || opened.IsEmpty || ReferenceEquals(opened, SKTypeface.Default))
        {
            data.Dispose();
            if (opened is not null && !ReferenceEquals(opened, SKTypeface.Default))
                opened.Dispose();
            return false;
        }

        Retained.Add(data);
        face = opened;
        return true;
    }

    static bool TryFamily(string? query, string family, out SKTypeface face)
    {
        face = SKTypeface.Default;
        if (string.IsNullOrWhiteSpace(query) || File.Exists(query))
            return false;

        var opened = SKTypeface.FromFamilyName(query);
        if (opened is not null && opened.FamilyName.Contains(family, StringComparison.OrdinalIgnoreCase))
        {
            face = opened;
            return true;
        }

        if (opened is not null && !ReferenceEquals(opened, SKTypeface.Default))
            opened.Dispose();
        return false;
    }

    static string? FontFileName(string? registered)
    {
        if (string.IsNullOrWhiteSpace(registered))
            return null;

        var hash = registered.IndexOf('#', StringComparison.Ordinal);
        return hash >= 0 ? registered[..hash] : registered;
    }

    static bool IsFontFile(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || File.Exists(name))
            return false;

        return name.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".otf", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".ttc", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".otc", StringComparison.OrdinalIgnoreCase);
    }
}
