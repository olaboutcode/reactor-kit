using SkiaSharp;

namespace Reactor.Kit.Internals.RichText;

/// <summary>One shaped piece of a word. The blob and any system image are disposed with the run. The font is not.</summary>
internal sealed class GlyphRun : IDisposable
{
    readonly bool _system;
    readonly float _ascent;
    readonly float _descent;
    SKImage? _image;
    SKColor _imageColor;
    bool _drawn;

    public GlyphRun(SKTextBlob? blob, string text, SKFont font, float width, bool system = false, float ascent = 0, float descent = 0)
    {
        Blob = blob;
        Text = text;
        Font = font;
        Width = width;
        _system = system;
        _ascent = ascent;
        _descent = descent;
    }

    public SKTextBlob? Blob { get; }

    public string Text { get; }

    public SKFont Font { get; }

    public float Width { get; }

    public void Draw(SKCanvas canvas, float x, float baseline, SKPaint paint)
    {
        if (_system)
        {
            DrawSystem(canvas, x, baseline, paint);
            return;
        }

        if (Blob is not null)
            canvas.DrawText(Blob, x, baseline, paint);
        else
            canvas.DrawText(Text, x, baseline, SKTextAlign.Left, Font, paint);
    }

    public void Dispose()
    {
        Blob?.Dispose();
        _image?.Dispose();
        _image = null;
    }

    void DrawSystem(SKCanvas canvas, float x, float baseline, SKPaint paint)
    {
        if (!_drawn || _imageColor != paint.Color)
        {
            _image?.Dispose();
            _image = null;
            _drawn = false;
            if (SystemText.Draw is not null
                && SystemText.Draw(Text, Font.Size, paint.Color, out var image, out _, out _, out _))
            {
                _image = image;
                _imageColor = paint.Color;
                _drawn = true;
            }
        }

        if (_image is null)
        {
            canvas.DrawText(Text, x, baseline, SKTextAlign.Left, Font, paint);
            return;
        }

        var top = baseline - _ascent;
        var dest = new SKRect(x, top, x + Width, top + _ascent + _descent);
        canvas.DrawImage(_image, dest, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));
    }
}
