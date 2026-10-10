using CoreGraphics;
using Microsoft.Maui.Platform;
using UIKit;

namespace Reactor.Kit.Glaze;

sealed class GlazePlatformView : UIView
{
    readonly UIView _clip;
    readonly UIVisualEffectView _blur;
    readonly UIView _tint;
    readonly UIImageView _noise;
    readonly UIView _stroke;
    UIView? _content;
    static UIImage? _noiseImage;

    public GlazePlatformView()
    {
        // The material is clipped. This view stays unclipped so a shadow can draw outside the circle.
        ClipsToBounds = false;
        Opaque = false;
        BackgroundColor = UIColor.Clear;

        _clip = new UIView
        {
            ClipsToBounds = true,
            BackgroundColor = UIColor.Clear,
        };
        _blur = new UIVisualEffectView
        {
            UserInteractionEnabled = false,
            ClipsToBounds = true,
        };
        _tint = new UIView { UserInteractionEnabled = false };
        _noise = new UIImageView
        {
            UserInteractionEnabled = false,
            ContentMode = UIViewContentMode.ScaleToFill,
            Image = NoiseImage()
        };
        _stroke = new UIView
        {
            UserInteractionEnabled = false,
            BackgroundColor = UIColor.Clear,
        };

        AddSubview(_clip);
        _clip.AddSubview(_blur);
        _clip.AddSubview(_tint);
        _clip.AddSubview(_noise);
        _clip.AddSubview(_stroke);
    }

    public GlazeView? VirtualView { get; set; }

    public void Apply(GlazeView view)
    {
        Layer.CornerRadius = (nfloat)view.CornerRadius;
        _blur.Effect = view.BlurRadius <= 0
            ? null
            : UIBlurEffect.FromStyle(UIBlurEffectStyle.SystemUltraThinMaterial);
        _tint.BackgroundColor = view.TintColor.ToPlatform();
        _tint.Alpha = GlazeTokens.WashOpacity(view.TintColor, view.TintOpacity, GlazeTokens.OverSystemMaterial);
        _noise.Alpha = (nfloat)view.NoiseOpacity;
        _stroke.Layer.BorderColor = view.Stroke.ToPlatform().CGColor;
        _stroke.Layer.BorderWidth = (nfloat)view.StrokeThickness;
        SetNeedsLayout();
    }

    public void SetContent(UIView? child)
    {
        if (_content == child)
            return;

        _content?.RemoveFromSuperview();
        _content = child;
        if (child is null)
            return;

        child.RemoveFromSuperview();
        _clip.AddSubview(child);
        SetNeedsLayout();
    }

    public override CGSize SizeThatFits(CGSize size)
    {
        if (VirtualView is not ICrossPlatformLayout layout)
            return base.SizeThatFits(size);

        var measured = layout.CrossPlatformMeasure(size.Width, size.Height);
        return new CGSize(measured.Width, measured.Height);
    }

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();
        _clip.Frame = Bounds;
        _clip.Layer.CornerRadius = Layer.CornerRadius;
        _blur.Frame = _clip.Bounds;
        _tint.Frame = _clip.Bounds;
        _noise.Frame = _clip.Bounds;
        _stroke.Frame = _clip.Bounds;
        var inset = _stroke.Layer.BorderWidth / 2f;
        _stroke.Layer.CornerRadius = (nfloat)Math.Max(0, Layer.CornerRadius - inset);
        Layer.ShadowPath = Bounds.IsEmpty
            ? null
            : UIBezierPath.FromRoundedRect(Bounds, Layer.CornerRadius).CGPath;

        if (VirtualView is ICrossPlatformLayout layout)
            layout.CrossPlatformArrange(Bounds.ToRectangle());
    }

    static UIImage NoiseImage()
    {
        if (_noiseImage is not null)
            return _noiseImage;

        const int size = NoiseTile.Size;
        var pixels = Premultiply(NoiseTile.Rgba);
        using var colorSpace = CGColorSpace.CreateDeviceRGB();
        using var context = new CGBitmapContext(
            IntPtr.Zero,
            size,
            size,
            8,
            size * 4,
            colorSpace,
            CGImageAlphaInfo.PremultipliedLast);

        if (context?.Data is null || context.Data == IntPtr.Zero)
            return _noiseImage = new UIImage();

        System.Runtime.InteropServices.Marshal.Copy(pixels, 0, context.Data, pixels.Length);
        using var image = context.ToImage();
        if (image is null)
            return _noiseImage = new UIImage();

        var scaled = UIImage.FromImage(image, UIScreen.MainScreen.Scale, UIImageOrientation.Up);
        return _noiseImage = scaled.CreateResizableImage(new UIEdgeInsets(0, 0, 0, 0), UIImageResizingMode.Tile);
    }

    static byte[] Premultiply(byte[] straight)
    {
        var pixels = new byte[straight.Length];
        for (var i = 0; i < straight.Length; i += 4)
        {
            var alpha = straight[i + 3];
            pixels[i] = (byte)(straight[i] * alpha / 255);
            pixels[i + 1] = (byte)(straight[i + 1] * alpha / 255);
            pixels[i + 2] = (byte)(straight[i + 2] * alpha / 255);
            pixels[i + 3] = alpha;
        }

        return pixels;
    }
}
