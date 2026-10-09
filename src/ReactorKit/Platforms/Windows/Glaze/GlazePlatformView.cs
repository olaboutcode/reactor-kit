using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;
using WCornerRadius = Microsoft.UI.Xaml.CornerRadius;

namespace Reactor.Kit.Glaze;

sealed class GlazePlatformView : Grid
{
    readonly AcrylicBrush _acrylic = new()
    {
        BackgroundSource = AcrylicBackgroundSource.Backdrop
    };

    readonly Border _panel;
    readonly Image _noise;
    readonly Rectangle _stroke;
    FrameworkElement? _content;
    double _cornerRadius;

    public GlazePlatformView()
    {
        _panel = new Border
        {
            Background = _acrylic,
            IsHitTestVisible = false
        };
        _noise = new Image
        {
            IsHitTestVisible = false,
            Stretch = Stretch.Fill,
            Source = NoiseImage()
        };
        _stroke = new Rectangle { IsHitTestVisible = false };

        Canvas.SetZIndex(_panel, 0);
        Canvas.SetZIndex(_noise, 1);
        Canvas.SetZIndex(_stroke, 2);
        Children.Add(_panel);
        Children.Add(_noise);
        Children.Add(_stroke);
    }

    public GlazeView? VirtualView { get; set; }

    public void Apply(GlazeView view)
    {
        var tint = ToWindowsColor(view.TintColor);
        _acrylic.TintColor = tint;
        _acrylic.TintOpacity = view.TintOpacity;
        _acrylic.FallbackColor = tint;
        _noise.Opacity = view.NoiseOpacity;
        _cornerRadius = view.CornerRadius;
        _panel.CornerRadius = new WCornerRadius(view.CornerRadius);
        _stroke.RadiusX = view.CornerRadius;
        _stroke.RadiusY = view.CornerRadius;
        _stroke.Stroke = new SolidColorBrush(ToWindowsColor(view.Stroke));
        _stroke.StrokeThickness = view.StrokeThickness;
        InvalidateArrange();
    }

    public void SetContent(FrameworkElement? child)
    {
        if (_content == child)
            return;

        if (_content is not null)
            Children.Remove(_content);

        _content = child;
        if (child is null)
            return;

        if (child.Parent is Panel panel)
            panel.Children.Remove(child);

        Canvas.SetZIndex(child, 3);
        Children.Add(child);
        InvalidateArrange();
    }

    protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
    {
        if (VirtualView is not ICrossPlatformLayout layout)
            return new Windows.Foundation.Size(0, 0);

        var measured = layout.CrossPlatformMeasure(availableSize.Width, availableSize.Height);
        return new Windows.Foundation.Size(measured.Width, measured.Height);
    }

    protected override Windows.Foundation.Size ArrangeOverride(Windows.Foundation.Size finalSize)
    {
        var slot = new Windows.Foundation.Rect(0, 0, finalSize.Width, finalSize.Height);
        _panel.Arrange(slot);
        _noise.Arrange(slot);
        _stroke.Arrange(slot);
        Clip = _cornerRadius > 0
            ? new RectangleGeometry
            {
                Rect = slot,
                RadiusX = _cornerRadius,
                RadiusY = _cornerRadius
            }
            : null;

        if (VirtualView is ICrossPlatformLayout layout)
            layout.CrossPlatformArrange(new Microsoft.Maui.Graphics.Rect(0, 0, finalSize.Width, finalSize.Height));

        return finalSize;
    }

    static Color ToWindowsColor(Microsoft.Maui.Graphics.Color color) =>
        Color.FromArgb(
            Channel(color.Alpha),
            Channel(color.Red),
            Channel(color.Green),
            Channel(color.Blue));

    static byte Channel(float component) => (byte)Math.Clamp(Math.Round(component * 255d), 0d, 255d);

    static WriteableBitmap NoiseImage()
    {
        var bitmap = new WriteableBitmap(NoiseTile.Size, NoiseTile.Size);
        using var buffer = bitmap.PixelBuffer.AsStream();
        buffer.Write(ToBgra(NoiseTile.Rgba));
        return bitmap;
    }

    static byte[] ToBgra(byte[] rgba)
    {
        var pixels = new byte[rgba.Length];
        for (var i = 0; i < rgba.Length; i += 4)
        {
            pixels[i] = rgba[i + 2];
            pixels[i + 1] = rgba[i + 1];
            pixels[i + 2] = rgba[i];
            pixels[i + 3] = rgba[i + 3];
        }

        return pixels;
    }
}
