using Microsoft.Maui;
using SkiaSharp;

namespace Reactor.Kit.Internals.RichText;

/// <summary>
/// Opens a font the app registered. Android hands back a file. Apple platforms hand back a family name.
/// </summary>
static class FontFace
{
    static readonly Lock Gate = new();
    static readonly Dictionary<string, SKTypeface> Faces = new(StringComparer.Ordinal);

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
        if (!string.IsNullOrEmpty(registered) && File.Exists(registered))
        {
            var file = SKTypeface.FromFile(registered);
            if (file is not null)
                return file;
        }

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
}
