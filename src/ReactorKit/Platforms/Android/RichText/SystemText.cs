using Android.Content.Res;
using Android.Graphics;
using Android.Text;
using SkiaSharp;

namespace Reactor.Kit.RichText;

internal static partial class SystemText
{
    static partial void Register()
    {
        Measure = MeasureAndroid;
        Draw = DrawAndroid;
    }

    static bool MeasureAndroid(string text, float size, out float width, out float ascent, out float descent)
    {
        using var paint = CreatePaint(size, SKColors.Black);
        return Metrics(paint, text, out width, out ascent, out descent);
    }

    static bool DrawAndroid(string text, float size, SKColor color, out SKImage? image, out float width, out float ascent, out float descent)
    {
        image = null;
        using var paint = CreatePaint(size, color);
        if (!Metrics(paint, text, out width, out ascent, out descent))
            return false;

        var density = Density();
        var pixelWidth = Math.Max(1, (int)Math.Ceiling(width * density));
        var pixelHeight = Math.Max(1, (int)Math.Ceiling((ascent + descent) * density));
        using var bitmap = Bitmap.CreateBitmap(pixelWidth, pixelHeight, Bitmap.Config.Argb8888!);
        if (bitmap is null)
            return false;

        using var canvas = new Canvas(bitmap);
        canvas.DrawText(text, 0, ascent * density, paint);
        using var stream = new MemoryStream();
        if (!bitmap.Compress(Bitmap.CompressFormat.Png!, 100, stream))
            return false;

        image = SKImage.FromEncodedData(stream.ToArray());
        return image is not null;
    }

    static TextPaint CreatePaint(float size, SKColor color)
    {
        var paint = new TextPaint(PaintFlags.AntiAlias) { TextSize = size * Density() };
        paint.Color = Android.Graphics.Color.Argb(color.Alpha, color.Red, color.Green, color.Blue);
        return paint;
    }

    static bool Metrics(TextPaint paint, string text, out float width, out float ascent, out float descent)
    {
        width = 0;
        ascent = 0;
        descent = 0;
        if (text.Length == 0)
            return false;

        var pixels = paint.MeasureText(text);
        var metrics = paint.GetFontMetrics();
        var density = Density();
        if (pixels <= 0 || density <= 0 || metrics is null)
            return false;

        width = pixels / density;
        ascent = -metrics.Ascent / density;
        descent = metrics.Descent / density;
        return ascent + descent > 0;
    }

    static float Density()
    {
        var density = Resources.System?.DisplayMetrics?.Density ?? 1f;
        return density < 1 ? 1 : density;
    }
}
