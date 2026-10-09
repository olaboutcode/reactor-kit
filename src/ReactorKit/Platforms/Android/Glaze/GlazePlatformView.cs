using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Paint = Android.Graphics.Paint;
using RectF = Android.Graphics.RectF;
using View = Android.Views.View;

namespace Reactor.Kit.Glaze;

/// <summary>
/// Blur, tint, noise, and stroke sit behind the MAUI child.
/// RenderEffect is attached to the childless backdrop, never to this parent.
/// </summary>
sealed class GlazePlatformView : FrameLayout
{
    const int Sample = 2;

    readonly ImageView _backdrop;
    readonly View _tint;
    readonly ImageView _noise;
    readonly StrokeView _stroke;
    readonly BackdropListener _listener;
    View? _content;
    Bitmap? _captured;
    ViewTreeObserver? _observer;
    long _lastCapture;
    bool _capturing;
    bool _canBlur;
    float _radius;
    float _corner;

    public GlazePlatformView(Android.Content.Context context) : base(context)
    {
        SetWillNotDraw(false);
        _backdrop = new ImageView(context) { Clickable = false };
        _tint = new View(context) { Clickable = false };
        _noise = new ImageView(context) { Clickable = false };
        var noise = NoiseBitmap();
        if (noise is not null)
        {
            var tiled = new BitmapDrawable(context.Resources, noise);
            tiled.SetTileModeXY(Shader.TileMode.Repeat, Shader.TileMode.Repeat);
            _noise.SetImageDrawable(tiled);
        }
        _stroke = new StrokeView(context);
        _listener = new BackdropListener(this);

        AddView(_backdrop, Fill());
        AddView(_tint, Fill());
        AddView(_noise, Fill());
        AddView(_stroke, Fill());
    }

    public GlazeView? VirtualView { get; set; }

    public void Apply(GlazeView view)
    {
        _canBlur = OperatingSystem.IsAndroidVersionAtLeast(31);
        var wash = GlazeTokens.WashOpacity(view.TintColor, view.TintOpacity, overSystemMaterial: false);
        var tintOpacity = _canBlur
            ? wash
            : Math.Max(wash, GlazeTokens.FallbackTintOpacity);

        _radius = Context?.ToPixels((float)view.BlurRadius) ?? 0f;
        _corner = Context?.ToPixels((float)view.CornerRadius) ?? 0f;
        var strokeWidth = Context?.ToPixels((float)view.StrokeThickness) ?? 0f;

        _tint.SetBackgroundColor(view.TintColor.WithAlpha((float)tintOpacity).ToPlatform());
        _noise.Alpha = (float)view.NoiseOpacity;
        _stroke.Radius = _corner;
        _stroke.Paint.StrokeWidth = strokeWidth;
        _stroke.Paint.Color = view.Stroke.ToPlatform();
        _stroke.Invalidate();

        OutlineProvider = _corner > 0 ? new RoundOutline(_corner) : null;
        ClipToOutline = _corner > 0;
        ApplyBlurEffect();
        RequestLayout();
    }

    public void SetContent(View? child)
    {
        if (_content == child)
            return;

        if (_content is not null)
            RemoveView(_content);

        _content = child;
        if (child is null)
            return;

        child.RemoveFromParent();
        AddView(child, Fill());
        RequestLayout();
    }

    public void Release()
    {
        RemoveListener();
        _backdrop.SetImageBitmap(null);
        _captured?.Recycle();
        _captured = null;
    }

    public void CaptureBackdrop()
    {
        if (_capturing || !_canBlur || _radius <= 0 || Width <= 0 || Height <= 0 || !IsShown)
            return;

        var now = SystemClock.UptimeMillis();
        if (now - _lastCapture < 32)
            return;

        var root = RootView;
        if (root is null)
            return;

        _lastCapture = now;
        var width = Math.Max(1, Width / Sample);
        var height = Math.Max(1, Height / Sample);
        EnsureBitmap(width, height);
        if (_captured is null)
            return;

        _capturing = true;
        var visibility = Visibility;
        try
        {
            var canvas = new Canvas(_captured);
            var location = new int[2];
            var rootLocation = new int[2];
            GetLocationOnScreen(location);
            root.GetLocationOnScreen(rootLocation);
            canvas.Save();
            canvas.Scale(1f / Sample, 1f / Sample);
            canvas.Translate(rootLocation[0] - location[0], rootLocation[1] - location[1]);
            Visibility = ViewStates.Invisible;
            root.Draw(canvas);
            canvas.Restore();
        }
        finally
        {
            Visibility = visibility;
            _capturing = false;
        }

        _backdrop.SetImageBitmap(_captured);
        ApplyBlurEffect();
    }

    protected override void OnAttachedToWindow()
    {
        base.OnAttachedToWindow();
        _observer = ViewTreeObserver;
        _observer?.AddOnPreDrawListener(_listener);
    }

    protected override void OnDetachedFromWindow()
    {
        RemoveListener();
        base.OnDetachedFromWindow();
    }

    protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
    {
        if (VirtualView is not ICrossPlatformLayout layout || Context is null)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
            return;
        }

        var availableWidth = widthMeasureSpec.ToDouble(Context);
        var availableHeight = heightMeasureSpec.ToDouble(Context);
        var measured = layout.CrossPlatformMeasure(availableWidth, availableHeight);
        var widthMode = MeasureSpec.GetMode(widthMeasureSpec);
        var heightMode = MeasureSpec.GetMode(heightMeasureSpec);
        var width = widthMode == MeasureSpecMode.Exactly ? availableWidth : measured.Width;
        var height = heightMode == MeasureSpecMode.Exactly ? availableHeight : measured.Height;
        SetMeasuredDimension((int)Context.ToPixels(width), (int)Context.ToPixels(height));
    }

    protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
    {
        var width = right - left;
        var height = bottom - top;
        _backdrop.Layout(0, 0, width, height);
        _tint.Layout(0, 0, width, height);
        _noise.Layout(0, 0, width, height);
        _stroke.Layout(0, 0, width, height);

        if (VirtualView is ICrossPlatformLayout layout && Context is not null)
            layout.CrossPlatformArrange(new Microsoft.Maui.Graphics.Rect(0, 0, Context.FromPixels(width), Context.FromPixels(height)));
    }

    void EnsureBitmap(int width, int height)
    {
        if (_captured is not null && !_captured.IsRecycled && _captured.Width == width && _captured.Height == height)
            return;

        _backdrop.SetImageBitmap(null);
        _captured?.Recycle();
        _captured = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888!);
    }

    void ApplyBlurEffect()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(31))
            return;

        if (!_canBlur || _radius <= 0)
        {
            _backdrop.SetRenderEffect(null);
            return;
        }

        var radius = _radius / Sample;
        _backdrop.SetRenderEffect(RenderEffect.CreateBlurEffect(radius, radius, Shader.TileMode.Clamp!));
    }

    static LayoutParams Fill()
        => new(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);

    void RemoveListener()
    {
        if (_observer is not null && _observer.IsAlive)
            _observer.RemoveOnPreDrawListener(_listener);

        _observer = null;
    }

    static Bitmap? NoiseBitmap()
    {
        var bitmap = Bitmap.CreateBitmap(NoiseTile.Size, NoiseTile.Size, Bitmap.Config.Argb8888!);
        if (bitmap is null)
            return null;

        var source = NoiseTile.Rgba;
        var pixels = new int[NoiseTile.Size * NoiseTile.Size];
        for (var i = 0; i < pixels.Length; i++)
        {
            var offset = i * 4;
            pixels[i] = (source[offset + 3] << 24)
                | (source[offset] << 16)
                | (source[offset + 1] << 8)
                | source[offset + 2];
        }

        bitmap.SetPixels(pixels, 0, NoiseTile.Size, 0, 0, NoiseTile.Size, NoiseTile.Size);
        return bitmap;
    }

    sealed class BackdropListener(GlazePlatformView owner) : Java.Lang.Object, ViewTreeObserver.IOnPreDrawListener
    {
        public bool OnPreDraw()
        {
            owner.CaptureBackdrop();
            return true;
        }
    }

    sealed class StrokeView : View
    {
        public StrokeView(Android.Content.Context context) : base(context)
        {
            SetWillNotDraw(false);
            Clickable = false;
            Paint.SetStyle(Paint.Style.Stroke);
        }

        public Paint Paint { get; } = new() { AntiAlias = true };

        public float Radius { get; set; }

        protected override void OnDraw(Canvas canvas)
        {
            if (Paint.StrokeWidth <= 0)
                return;

            var inset = Paint.StrokeWidth / 2f;
            canvas.DrawRoundRect(new RectF(inset, inset, Width - inset, Height - inset), Radius, Radius, Paint);
        }
    }

    sealed class RoundOutline(float radius) : ViewOutlineProvider
    {
        public override void GetOutline(View? view, Outline? outline)
        {
            if (view is null || outline is null)
                return;

            outline.SetRoundRect(0, 0, view.Width, view.Height, radius);
        }
    }
}
