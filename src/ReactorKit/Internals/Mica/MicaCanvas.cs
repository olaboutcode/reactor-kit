using System.Runtime.InteropServices;
using Reactor.Kit.Internals.Glaze;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace Reactor.Kit.Internals.Mica;

/// <summary>
/// Paints one mica round rect. The canvas does not sample the backdrop and does not take the tap.
/// Lengths on the properties are dp and are multiplied by <c>info.Width / Width</c>.
/// A negative <see cref="ShadeOpacity"/> derives the bottom alpha from the tint.
/// </summary>
internal sealed class MicaCanvas : SKCanvasView
{
    public static readonly BindableProperty TintColorProperty = BindableProperty.Create(
        nameof(TintColor), typeof(Color), typeof(MicaCanvas), MicaTokens.TintColor, propertyChanged: OnChanged);

    public static readonly BindableProperty TintOpacityProperty = BindableProperty.Create(
        nameof(TintOpacity), typeof(double), typeof(MicaCanvas), (double)MicaTokens.TintOpacity, propertyChanged: OnChanged);

    public static readonly BindableProperty ShadeColorProperty = BindableProperty.Create(
        nameof(ShadeColor), typeof(Color), typeof(MicaCanvas), null, propertyChanged: OnChanged);

    public static readonly BindableProperty ShadeOpacityProperty = BindableProperty.Create(
        nameof(ShadeOpacity), typeof(double), typeof(MicaCanvas), MicaTokens.UnsetOpacity, propertyChanged: OnChanged);

    public static readonly BindableProperty NoiseOpacityProperty = BindableProperty.Create(
        nameof(NoiseOpacity), typeof(double), typeof(MicaCanvas), (double)MicaTokens.NoiseOpacity, propertyChanged: OnChanged);

    public static readonly BindableProperty StrokeProperty = BindableProperty.Create(
        nameof(Stroke), typeof(Color), typeof(MicaCanvas), MicaTokens.Stroke, propertyChanged: OnChanged);

    public static readonly BindableProperty StrokeThicknessProperty = BindableProperty.Create(
        nameof(StrokeThickness), typeof(double), typeof(MicaCanvas), 1d, propertyChanged: OnChanged);

    public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(
        nameof(CornerRadius), typeof(double), typeof(MicaCanvas), 0d, propertyChanged: OnChanged);

    static readonly SKBitmap? NoiseBitmap = CreateNoise();

    public MicaCanvas()
    {
        IgnorePixelScaling = false;
        BackgroundColor = Colors.Transparent;
        InputTransparent = true;
        // The bindable default is 1 dp. The hairline replaces it before the page sets a width.
        StrokeThickness = MicaTokens.HairlineThickness;
        PaintSurface += OnPaintSurface;
    }

    public Color TintColor
    {
        get => (Color)GetValue(TintColorProperty);
        set => SetValue(TintColorProperty, value);
    }

    public double TintOpacity
    {
        get => (double)GetValue(TintOpacityProperty);
        set => SetValue(TintOpacityProperty, value);
    }

    public Color? ShadeColor
    {
        get => (Color?)GetValue(ShadeColorProperty);
        set => SetValue(ShadeColorProperty, value);
    }

    /// <summary>Bottom alpha. A negative value uses <see cref="MicaTokens.ShadeAlpha"/> of the tint opacity.</summary>
    public double ShadeOpacity
    {
        get => (double)GetValue(ShadeOpacityProperty);
        set => SetValue(ShadeOpacityProperty, value);
    }

    public double NoiseOpacity
    {
        get => (double)GetValue(NoiseOpacityProperty);
        set => SetValue(NoiseOpacityProperty, value);
    }

    public Color Stroke
    {
        get => (Color)GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public double CornerRadius
    {
        get => (double)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    static void OnChanged(BindableObject bindable, object oldValue, object newValue)
        => ((MicaCanvas)bindable).InvalidateSurface();

    void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        InputTransparent = true;

        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (Width <= 0 || Height <= 0 || e.Info.Width <= 0 || e.Info.Height <= 0)
            return;

        var scale = (float)(e.Info.Width / Width);
        var bounds = new SKRect(0, 0, e.Info.Width, e.Info.Height);
        var radius = Math.Min(
            (float)Math.Max(CornerRadius, 0) * scale,
            Math.Min(bounds.Width, bounds.Height) / 2f);

        DrawFill(canvas, bounds, radius);
        DrawNoise(canvas, bounds, radius);
        DrawStroke(canvas, bounds, radius, scale);
    }

    void DrawFill(SKCanvas canvas, SKRect bounds, float radius)
    {
        var tint = TintColor;
        var shade = ShadeColor ?? Darken(tint);
        var shadeOpacity = ShadeOpacity < 0 ? TintOpacity * MicaTokens.ShadeAlpha : ShadeOpacity;

        using var shader = SKShader.CreateLinearGradient(
            new SKPoint(bounds.Left, bounds.Top),
            new SKPoint(bounds.Left, bounds.Bottom),
            [Rgb(tint, TintOpacity), Rgb(shade, shadeOpacity)],
            null,
            SKShaderTileMode.Clamp);
        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Shader = shader,
            IsAntialias = true
        };
        canvas.DrawRoundRect(bounds, radius, radius, paint);
    }

    void DrawNoise(SKCanvas canvas, SKRect bounds, float radius)
    {
        if (NoiseOpacity <= 0 || NoiseBitmap is null)
            return;

        using var shader = SKShader.CreateBitmap(NoiseBitmap, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat);
        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Shader = shader,
            Color = new SKColor(255, 255, 255, Channel(NoiseOpacity)),
            IsAntialias = true
        };
        canvas.DrawRoundRect(bounds, radius, radius, paint);
    }

    void DrawStroke(SKCanvas canvas, SKRect bounds, float radius, float scale)
    {
        if (StrokeThickness <= 0)
            return;

        var strokeWidth = (float)StrokeThickness * scale;
        var half = strokeWidth / 2f;
        var inner = new SKRect(bounds.Left + half, bounds.Top + half, bounds.Right - half, bounds.Bottom - half);
        if (inner.Width <= 0 || inner.Height <= 0)
            return;

        var strokeRadius = Math.Min(
            Math.Max(0, radius - half),
            Math.Min(inner.Width, inner.Height) / 2f);

        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = ToSk(Stroke),
            StrokeWidth = strokeWidth,
            IsAntialias = true
        };
        canvas.DrawRoundRect(inner, strokeRadius, strokeRadius, paint);
    }

    static Color Darken(Color color)
        => Color.FromRgb(
            (float)Math.Clamp(color.Red * MicaTokens.ShadeRgb, 0d, 1d),
            (float)Math.Clamp(color.Green * MicaTokens.ShadeRgb, 0d, 1d),
            (float)Math.Clamp(color.Blue * MicaTokens.ShadeRgb, 0d, 1d));

    static SKColor Rgb(Color color, double opacity)
        => new(Channel(color.Red), Channel(color.Green), Channel(color.Blue), Channel(opacity));

    static SKColor ToSk(Color color)
        => new(Channel(color.Red), Channel(color.Green), Channel(color.Blue), Channel(color.Alpha));

    static byte Channel(double unit)
        => (byte)Math.Clamp(unit * 255d, 0d, 255d);

    static SKBitmap? CreateNoise()
    {
        var info = new SKImageInfo(NoiseTile.Size, NoiseTile.Size, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        var bitmap = new SKBitmap(info);
        var pixels = bitmap.GetPixels();
        if (pixels == IntPtr.Zero)
        {
            bitmap.Dispose();
            return null;
        }

        var tight = NoiseTile.Size * 4;
        var rowBytes = bitmap.RowBytes;
        if (rowBytes == tight)
        {
            Marshal.Copy(NoiseTile.Rgba, 0, pixels, NoiseTile.Rgba.Length);
            return bitmap;
        }

        for (var y = 0; y < NoiseTile.Size; y++)
            Marshal.Copy(NoiseTile.Rgba, y * tight, IntPtr.Add(pixels, y * rowBytes), tight);
        return bitmap;
    }
}
