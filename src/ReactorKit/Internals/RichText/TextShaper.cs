using System.Globalization;
using System.Text;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace Reactor.Kit.Internals.RichText;

/// <summary>
/// Breaks a word into shaped runs so missing glyphs use a system font.
/// </summary>
internal sealed class TextShaper : IDisposable
{
    static readonly string[] EmojiFamilies = [
        "Apple Color Emoji",
        "Noto Color Emoji",
        "Segoe UI Emoji",
    ];

    static readonly string[] EmojiFiles =
    [
        "/System/Library/Fonts/Apple Color Emoji.ttc",
        "/system/fonts/NotoColorEmoji.ttf",
        "/system/fonts/NotoColorEmojiLegacy.ttf",
        "C:\\Windows\\Fonts\\seguiemj.ttf",
    ];

    readonly SKTypeface _body;
    readonly Dictionary<int, SKTypeface> _matched = [];
    readonly Dictionary<string, SKTypeface> _owned = [];
    readonly Dictionary<SKTypeface, SKFont> _fonts = [];
    readonly Dictionary<SKTypeface, SKShaper> _shapers = [];
    SKFont? _bodyFont;
    SKTypeface? _emoji;
    bool _emojiLookedUp;

    public TextShaper(SKTypeface? body = null)
    {
        _body = body ?? SKTypeface.Default;
    }

    public void Use(float size)
    {
        if (_bodyFont is not null && Math.Abs(_bodyFont.Size - size) < 0.01f)
            return;

        foreach (var font in _fonts.Values)
            font.Dispose();
        _fonts.Clear();
        _bodyFont?.Dispose();
        _bodyFont = new SKFont(_body, size);
    }

    public GlyphRun[] Shape(string text)
    {
        if (text.Length == 0 || _bodyFont is null)
            return [];

        var runs = new List<GlyphRun>();
        var chunk = new StringBuilder();
        SKTypeface? face = null;
        var system = false;
        var open = false;
        var elements = StringInfo.GetTextElementEnumerator(text);
        while (elements.MoveNext())
        {
            var element = (string)elements.Current;
            var next = FaceFor(element, out var nextSystem);
            if (next is null && !nextSystem)
            {
                if (!open)
                {
                    next = _body;
                }
                else
                {
                    chunk.Append(element);
                    continue;
                }
            }

            if (open && (nextSystem != system || (!nextSystem && !ReferenceEquals(face, next))))
            {
                runs.Add(Emit(chunk, system, face));
                chunk.Clear();
            }

            system = nextSystem;
            face = next;
            open = true;
            chunk.Append(element);
        }

        if (chunk.Length > 0)
            runs.Add(Emit(chunk, system, face));
        return [.. runs];
    }

    GlyphRun Emit(StringBuilder chunk, bool system, SKTypeface? face)
    {
        var value = chunk.ToString();
        return system ? PlatformRun(value) : ShapeRun(value, face ?? _body);
    }

    GlyphRun PlatformRun(string text)
    {
        var font = _bodyFont!;
        if (SystemText.Measure is not null
            && SystemText.Measure(
                text,
                font.Size,
                out var width,
                out var ascent,
                out var descent
            ))
        {
            return new GlyphRun(
                blob: null,
                text,
                font,
                Math.Max(0, width),
                system: true,
                ascent,
                descent);
        }

        return Unshaped(text, font);
    }

    public void Dispose()
    {
        foreach (var font in _fonts.Values)
            font.Dispose();
        _fonts.Clear();
        foreach (var shaper in _shapers.Values)
            shaper.Dispose();
        _shapers.Clear();
        foreach (var face in _owned.Values)
            face.Dispose();
        _owned.Clear();
        _matched.Clear();
        _bodyFont?.Dispose();
        _bodyFont = null;
        _emoji = null;
    }

    SKTypeface? FaceFor(string cluster, out bool system)
    {
        system = false;
        var emoji = false;
        var missing = 0;
        var first = 0;
        var seen = false;
        foreach (var rune in cluster.EnumerateRunes())
        {
            var value = rune.Value;
            if (value is 0x200D or 0xFE0E or 0xFE0F)
            {
                emoji |= value == 0xFE0F;
                continue;
            }

            if (!seen)
            {
                first = value;
                seen = true;
            }

            emoji |= IsEmoji(value);
            if (_bodyFont!.GetGlyph(value) == 0)
                missing = value;
        }

        if (!seen)
            return null;
        if (emoji)
        {
            var face = EmojiFace();
            if (face is not null)
                return face;
        }
        else if (missing == 0)
        {
            return _body;
        }

        if (SystemText.CanDraw)
        {
            system = true;
            return null;
        }

        if (emoji)
            return Match(first);
        var match = Match(missing);
        if (!ReferenceEquals(match, _body) && Contains(match, missing))
            return match;
        return _body;
    }

    static bool IsEmoji(int value)
        => value is >= 0x1F000 and <= 0x1FAFF or >= 0x1F1E6 and <= 0x1F1FF;

    SKTypeface? EmojiFace()
    {
        if (_emojiLookedUp)
            return _emoji;
        _emojiLookedUp = true;

        foreach (var name in EmojiFamilies)
        {
            var face = SKTypeface.FromFamilyName(name);
            if (face is not null && TakeEmoji(face))
                return _emoji;
        }

        var match = SKFontManager.Default?.MatchCharacter(0x1F64F)
            ?? SKFontManager.Default?.MatchCharacter(0x1F600);
        if (match is not null && TakeEmoji(match))
            return _emoji;

        if (SystemText.CanDraw)
            return null;

        foreach (var path in EmojiFiles)
        {
            if (!File.Exists(path))
                continue;
            var file = SKTypeface.FromFile(path);
            if (file is not null && TakeEmoji(file))
                return _emoji;
        }

        return null;
    }

    bool TakeEmoji(SKTypeface face)
    {
        if (!DrawsColor(face))
        {
            ReleaseProbe(face);
            return false;
        }

        _emoji = Canonical(face);
        return true;
    }

    static bool DrawsColor(SKTypeface face)
    {
        using var font = new SKFont(face, 32);
        var glyph = font.GetGlyph(0x1F64F);
        if (glyph == 0)
            glyph = font.GetGlyph(0x1F600);
        if (glyph == 0)
            return false;

        var info = new SKImageInfo(64, 64, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var bitmap = new SKBitmap(info);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        using var paint = new SKPaint { IsAntialias = true, Color = SKColors.Black };
        using var builder = new SKTextBlobBuilder();
        var buffer = builder.AllocateRawPositionedRun(font, 1, null);
        buffer.Glyphs[0] = (ushort)glyph;
        buffer.Positions[0] = new SKPoint(8, 48);
        using var blob = builder.Build();
        canvas.DrawText(blob, 0, 0, paint);
        canvas.Flush();

        var colored = 0;
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.Alpha < 20)
                    continue;
                var high = Math.Max(pixel.Red, Math.Max(pixel.Green, pixel.Blue));
                var low = Math.Min(pixel.Red, Math.Min(pixel.Green, pixel.Blue));
                if (high - low > 18)
                    colored++;
            }
        }

        return colored >= 12;
    }

    SKTypeface Match(int codePoint)
    {
        if (_matched.TryGetValue(codePoint, out var cached))
            return cached;

        var found = SKFontManager.Default?.MatchCharacter(codePoint);
        if (found is null)
            return _body;
        found = Canonical(found);
        _matched[codePoint] = found;
        return found;
    }

    SKTypeface Canonical(SKTypeface face)
    {
        var name = face.FamilyName ?? string.Empty;
        if (string.Equals(name, _body.FamilyName, StringComparison.Ordinal))
        {
            if (!ReferenceEquals(face, _body))
                face.Dispose();
            return _body;
        }

        if (_owned.TryGetValue(name, out var existing))
        {
            if (!ReferenceEquals(existing, face))
                face.Dispose();
            return existing;
        }

        _owned[name] = face;
        return face;
    }

    void ReleaseProbe(SKTypeface face)
    {
        if (!ReferenceEquals(face, _body)
            && !string.Equals(face.FamilyName, _body.FamilyName, StringComparison.Ordinal))
        {
            face.Dispose();
        }
    }

    bool Contains(SKTypeface face, int codePoint)
    {
        using var font = new SKFont(face, _bodyFont?.Size ?? 16f);
        return font.GetGlyph(codePoint) != 0;
    }

    GlyphRun ShapeRun(string text, SKTypeface face)
    {
        var font = FontFor(face);
        try
        {
            var shaped = ShaperFor(face).Shape(text, font);
            if (shaped.Codepoints.Length == 0)
                return Unshaped(text, font);

            using var builder = new SKTextBlobBuilder();
            var buffer = builder.AllocateRawPositionedRun(font, shaped.Codepoints.Length, null);
            var glyphs = buffer.Glyphs;
            var points = buffer.Positions;
            var drawn = false;
            for (var i = 0; i < shaped.Codepoints.Length; i++)
            {
                drawn |= shaped.Codepoints[i] != 0;
                glyphs[i] = (ushort)shaped.Codepoints[i];
                points[i] = shaped.Points[i];
            }

            if (!drawn && SystemText.CanDraw)
                return PlatformRun(text);

            return new GlyphRun(builder.Build(), text, font, shaped.Width);
        }
        catch (Exception)
        {
            return Unshaped(text, font);
        }
    }

    static GlyphRun Unshaped(string text, SKFont font)
        => new(blob: null, text, font, font.MeasureText(text));

    SKFont FontFor(SKTypeface face)
    {
        if (ReferenceEquals(face, _body))
            return _bodyFont!;
        if (_fonts.TryGetValue(face, out var font))
            return font;
        font = new SKFont(face, _bodyFont!.Size);
        _fonts[face] = font;
        return font;
    }

    SKShaper ShaperFor(SKTypeface face)
    {
        if (_shapers.TryGetValue(face, out var shaper))
            return shaper;
        shaper = new SKShaper(face);
        _shapers[face] = shaper;
        return shaper;
    }
}
