using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace Reactor.Kit.Markdown;

/// <summary>Draws markdown, including pipe tables and flowchart diagrams.</summary>
internal sealed class MarkdownCanvas : SKCanvasView, IDisposable
{
    public static readonly BindableProperty MarkdownProperty = BindableProperty.Create(
        nameof(Markdown), typeof(string), typeof(MarkdownCanvas), string.Empty, propertyChanged: OnLayoutChanged);

    public static readonly BindableProperty FileProperty = BindableProperty.Create(
        nameof(File), typeof(string), typeof(MarkdownCanvas), string.Empty, propertyChanged: OnLayoutChanged);

    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(
        nameof(FontSize), typeof(double), typeof(MarkdownCanvas), 16d, propertyChanged: OnLayoutChanged);

    public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
        nameof(TextColor), typeof(Color), typeof(MarkdownCanvas), Colors.Black, propertyChanged: OnPaintChanged);

    MarkdownLayout? _layout;
    SKPaint? _textPaint;
    SKPaint? _fillPaint;
    SKPaint? _strokePaint;
    string? _cachedPath;
    string? _cachedText;
    string? _laidSource;
    float _laidWidth = -1;
    double _laidSize = -1;
    bool _layoutDirty = true;
    bool _disposed;

    public MarkdownCanvas()
    {
        IgnorePixelScaling = true;
        BackgroundColor = Colors.Transparent;
        PaintSurface += OnPaintSurface;
    }

    public string Markdown
    {
        get => (string)GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    /// <summary>UTF-8 file path. When this is set, it is the source instead of <see cref="Markdown"/>.</summary>
    public string File
    {
        get => (string)GetValue(FileProperty);
        set => SetValue(FileProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        PaintSurface -= OnPaintSurface;
        ReleasePaint();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is null)
            ReleasePaint();
    }

    protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
    {
        var width = double.IsFinite(widthConstraint) && widthConstraint > 0 ? widthConstraint : 10_000d;
        LayoutIfNeeded((float)width);
        var measuredWidth = double.IsFinite(widthConstraint) ? width : Math.Max(1, _layout?.Width ?? 1);
        return new Size(measuredWidth, Math.Max(1, _layout?.Height ?? 1));
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width <= 0)
            return;
        if (_layout is null || Math.Abs(width - _laidWidth) >= 0.5)
        {
            LayoutIfNeeded((float)width);
            InvalidateSurface();
        }

        var content = Math.Max(1d, _layout?.Height ?? 1);
        if (Math.Abs(HeightRequest - content) > 0.5)
            HeightRequest = content;
    }

    static void OnLayoutChanged(BindableObject bindable, object oldValue, object newValue)
        => ((MarkdownCanvas)bindable).InvalidateMarkdown();

    static void OnPaintChanged(BindableObject bindable, object oldValue, object newValue)
        => ((MarkdownCanvas)bindable).InvalidateSurface();

    void InvalidateMarkdown()
    {
        _cachedPath = null;
        _cachedText = null;
        _layoutDirty = true;
        InvalidateMeasure();
        InvalidateSurface();
    }

    void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        EnsurePaints();
        LayoutIfNeeded((float)Math.Max(1, Width));
        var color = ToSk(TextColor);
        _textPaint!.Color = color;
        _fillPaint!.Color = new SKColor(color.Red, color.Green, color.Blue, 28);
        _strokePaint!.Color = new SKColor(color.Red, color.Green, color.Blue, 120);
        _layout?.Draw(canvas, _textPaint, _fillPaint, _strokePaint);
    }

    void LayoutIfNeeded(float width)
    {
        var source = Source();
        var size = FontSize;
        if (!_layoutDirty && _layout is not null && Math.Abs(width - _laidWidth) < 0.5f
            && string.Equals(_laidSource, source, StringComparison.Ordinal)
            && Math.Abs(_laidSize - size) < 0.01)
        {
            return;
        }

        _layout?.Dispose();
        _layout = new MarkdownLayout(source, width, (float)size);
        _laidWidth = width;
        _laidSource = source;
        _laidSize = size;
        _layoutDirty = false;
        var content = Math.Max(1d, _layout.Height);
        if (Math.Abs(HeightRequest - content) > 0.5)
            HeightRequest = content;
    }

    string Source()
    {
        var path = File;
        if (string.IsNullOrWhiteSpace(path))
            return Markdown ?? string.Empty;
        if (string.Equals(_cachedPath, path, StringComparison.Ordinal) && _cachedText is not null)
            return _cachedText;

        _cachedPath = path;
        try
        {
            _cachedText = System.IO.File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            _cachedText = "Cannot read `" + path + "`.";
        }

        return _cachedText;
    }

    void EnsurePaints()
    {
        _textPaint ??= new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
        _fillPaint ??= new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
        _strokePaint ??= new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
    }

    void ReleasePaint()
    {
        _layout?.Dispose();
        _layout = null;
        _textPaint?.Dispose();
        _fillPaint?.Dispose();
        _strokePaint?.Dispose();
        _textPaint = null;
        _fillPaint = null;
        _strokePaint = null;
        _layoutDirty = true;
    }

    static SKColor ToSk(Color color)
        => new(
            (byte)Math.Clamp(color.Red * 255d, 0, 255),
            (byte)Math.Clamp(color.Green * 255d, 0, 255),
            (byte)Math.Clamp(color.Blue * 255d, 0, 255),
            (byte)Math.Clamp(color.Alpha * 255d, 0, 255));
}
