using CoreAnimation;
using CoreGraphics;
using Foundation;
using Microsoft.Maui.Platform;
using UIKit;

namespace Reactor.Kit.Internals.Glaze;

sealed class GlazePlatformView : UIView
{
    readonly UIView _clip;
    readonly UIVisualEffectView _blur;
    readonly UIView _tint;
    readonly UIImageView _noise;
    readonly UIView _stroke;
    readonly CAGradientLayer _gradient = new();
    readonly CAShapeLayer _gradientMask = new()
    {
        FillColor = UIColor.Clear.CGColor,
        StrokeColor = UIColor.White.CGColor,
        LineJoin = CAShapeLayer.JoinRound,
    };
    UIView? _content;
    bool _gradientStroke;
    nfloat _gradientWidth;
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
        _gradient.Mask = _gradientMask;
        _gradient.Hidden = true;
        _stroke.Layer.AddSublayer(_gradient);

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
        ApplyStroke(GlazeStroke.From(view.Stroke), (nfloat)view.StrokeThickness);
        SetNeedsLayout();
    }

    void ApplyStroke(GlazeStroke stroke, nfloat thickness)
    {
        _gradientStroke = stroke.IsGradient && stroke.Stops.Length >= 2 && thickness > 0;
        if (!_gradientStroke)
        {
            _gradient.Hidden = true;
            _gradientWidth = 0;
            _stroke.Layer.BorderWidth = stroke.IsGradient ? 0 : thickness;
            if (!stroke.IsGradient)
                _stroke.Layer.BorderColor = stroke.Solid.ToPlatform().CGColor;
            return;
        }

        _stroke.Layer.BorderWidth = 0;
        _gradientWidth = thickness;
        _gradient.Hidden = false;
        var colors = new CGColor[stroke.Stops.Length];
        var locations = new NSNumber[stroke.Stops.Length];
        for (var i = 0; i < stroke.Stops.Length; i++)
        {
            colors[i] = stroke.Stops[i].Color.ToPlatform().CGColor;
            locations[i] = NSNumber.FromNFloat(stroke.Stops[i].Offset);
        }

        _gradient.Colors = colors;
        _gradient.Locations = locations;
        _gradient.StartPoint = new CGPoint(stroke.Start.X, stroke.Start.Y);
        _gradient.EndPoint = new CGPoint(stroke.End.X, stroke.End.Y);
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
        var width = _gradientStroke ? _gradientWidth : _stroke.Layer.BorderWidth;
        var inset = width / 2f;
        _stroke.Layer.CornerRadius = (nfloat)Math.Max(0, Layer.CornerRadius - inset);
        if (_gradientStroke)
            LayoutGradient(inset);
        Layer.ShadowPath = Bounds.IsEmpty
            ? null
            : UIBezierPath.FromRoundedRect(Bounds, Layer.CornerRadius).CGPath;

        if (VirtualView is ICrossPlatformLayout layout)
            layout.CrossPlatformArrange(Bounds.ToRectangle());
    }

    void LayoutGradient(nfloat inset)
    {
        _gradient.Frame = _stroke.Bounds;
        _gradientMask.Frame = _stroke.Bounds;
        var bounds = _stroke.Bounds;
        var rect = new CGRect(
            bounds.X + inset,
            bounds.Y + inset,
            Math.Max(0, bounds.Width - _gradientWidth),
            Math.Max(0, bounds.Height - _gradientWidth));
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            _gradientMask.Path = null;
            return;
        }

        var radius = (nfloat)Math.Max(0, Layer.CornerRadius - inset);
        _gradientMask.Path = UIBezierPath.FromRoundedRect(rect, radius).CGPath;
        _gradientMask.LineWidth = _gradientWidth;
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
