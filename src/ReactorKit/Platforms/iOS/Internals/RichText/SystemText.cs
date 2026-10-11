using System.Runtime.InteropServices;
using CoreGraphics;
using CoreText;
using Foundation;
using SkiaSharp;
using UIKit;

namespace Reactor.Kit.Internals.RichText;

internal static partial class SystemText
{
    static partial void Register()
    {
        Measure = MeasureCoreText;
        Draw = DrawCoreText;
    }

    static bool MeasureCoreText(string text, float size, out float width, out float ascent, out float descent)
        => WithLine(text, size, SKColors.Black, out width, out ascent, out descent, draw: null);

    static bool DrawCoreText(string text, float size, SKColor color, out SKImage? image, out float width, out float ascent, out float descent)
    {
        SKImage? painted = null;
        var ok = WithLine(text, size, color, out var measuredWidth, out var measuredAscent, out var measuredDescent, (line, lineWidth, lineAscent, lineDescent) =>
        {
            painted = Rasterize(line, lineWidth, lineAscent, lineDescent);
        });
        width = measuredWidth;
        ascent = measuredAscent;
        descent = measuredDescent;
        image = painted;
        return ok && image is not null;
    }

    static bool WithLine(string text, float size, SKColor ink, out float width, out float ascent, out float descent, Action<CTLine, float, float, float>? draw)
    {
        width = 0;
        ascent = 0;
        descent = 0;
        if (text.Length == 0)
            return false;

        using var font = new CTFont(CTFontUIFontType.System, size, null!);
        using var cg = new CGColor(ink.Red / 255f, ink.Green / 255f, ink.Blue / 255f, ink.Alpha / 255f);
        var attrs = new CTStringAttributes { Font = font, ForegroundColor = cg };
        using var attributed = new NSAttributedString(text, attrs);
        using var line = new CTLine(attributed);
        var advance = line.GetTypographicBounds(out var lineAscent, out var lineDescent, out _);
        if (advance <= 0 || lineAscent + lineDescent <= 0)
            return false;

        width = (float)advance;
        ascent = (float)lineAscent;
        descent = (float)lineDescent;
        draw?.Invoke(line, width, ascent, descent);
        return true;
    }

    static SKImage? Rasterize(CTLine line, float width, float ascent, float descent)
    {
        var scale = UIScreen.MainScreen?.Scale ?? (NFloat)2;
        if (scale < 1)
            scale = 1;

        var pixelWidth = Math.Max(1, (int)Math.Ceiling(width * (float)scale));
        var pixelHeight = Math.Max(1, (int)Math.Ceiling((ascent + descent) * (float)scale));
        var stride = pixelWidth * 4;
        var buffer = new byte[stride * pixelHeight];
        var pin = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            using var space = CGColorSpace.CreateDeviceRGB();
            using var context = new CGBitmapContext(
                pin.AddrOfPinnedObject(),
                pixelWidth,
                pixelHeight,
                8,
                stride,
                space,
                CGImageAlphaInfo.PremultipliedLast);
            context.ScaleCTM(scale, scale);
            context.TextPosition = new CGPoint(0, (NFloat)descent);
            line.Draw(context);

            var info = new SKImageInfo(pixelWidth, pixelHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var bitmap = new SKBitmap(info);
            var destination = bitmap.GetPixels();
            var rowBytes = bitmap.RowBytes;
            for (var y = 0; y < pixelHeight; y++)
                Marshal.Copy(buffer, y * stride, destination + (y * rowBytes), stride);

            using var view = SKImage.FromBitmap(bitmap);
            using var data = view.Encode(SKEncodedImageFormat.Png, 100);
            return data is null ? null : SKImage.FromEncodedData(data);
        }
        finally
        {
            pin.Free();
        }
    }
}
