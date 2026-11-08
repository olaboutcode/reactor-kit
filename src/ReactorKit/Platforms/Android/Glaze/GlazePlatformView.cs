using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using Microsoft.Maui.Platform;
using Paint = Android.Graphics.Paint;
using RectF = Android.Graphics.RectF;
using View = Android.Views.View;

namespace Reactor.Kit.Internals.Glaze;

/// <summary>
/// Blur, tint, noise, and stroke sit behind the MAUI child.
/// RenderEffect is attached to the childless backdrop, never to this parent.
/// </summary>
sealed class GlazePlatformView : FrameLayout
{
    const int Sample = 4;
    static int _drawDepth;

    readonly ImageView _backdrop;
    readonly View _material;
    readonly View _tint;
    readonly ImageView _noise;
    readonly StrokeView _stroke;
    readonly BackdropListener _listener;
    readonly List<BackdropLayer> _layers = new();
    readonly Android.Graphics.Rect _glazeRect = new();
    readonly Android.Graphics.Rect _viewRect = new();
    readonly int[] _screen = new int[2];
    readonly Paint _layerPaint = new();
    View? _content;
    Bitmap? _captured;
    ViewTreeObserver? _observer;
    long _lastCapture;
    int _contentStamp;
    bool _capturing;
    bool _skipNextCapture;
    bool _canBlur;
    readonly Paint _shadowMask = new() { AntiAlias = true, Color = Android.Graphics.Color.Black };
    float _radius;
    float _corner;

    public GlazePlatformView(Android.Content.Context context) : base(context)
    {
        SetWillNotDraw(false);
        _shadowMask.SetStyle(Paint.Style.Fill);
        _backdrop = new ImageView(context) { Clickable = false };
        _backdrop.SetScaleType(ImageView.ScaleType.FitXy);
        _material = new View(context) { Clickable = false };
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
        AddView(_material, Fill());
        AddView(_tint, Fill());
        AddView(_noise, Fill());
        AddView(_stroke, Fill());
    }

    public GlazeView? VirtualView { get; set; }

    public void Apply(GlazeView view)
    {
        _canBlur = OperatingSystem.IsAndroidVersionAtLeast(31);
        var wash = GlazeTokens.WashOpacity(view.TintColor, view.TintOpacity, GlazeTokens.OverSystemMaterial);
        var tintOpacity = _canBlur
            ? wash
            : Math.Max(wash, GlazeTokens.FallbackTintOpacity);

        _radius = Context?.ToPixels((float)view.BlurRadius) ?? 0f;
        _corner = Context?.ToPixels((float)view.CornerRadius) ?? 0f;
        var strokeWidth = Context?.ToPixels((float)view.StrokeThickness) ?? 0f;

        ApplyMaterial();
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
        // The shadow bitmap is cached from the first draw. Rebuild it once the corner exists.
        (Parent as View)?.Invalidate();
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
        // A draw started from here must not re-enter. Nested software draws share
        // one child list, and a second pass throws while the first is still reading it.
        if (_drawDepth > 0 || _capturing || !_canBlur || _radius <= 0 || Width <= 0 || Height <= 0 || !IsShown)
            return;

        ScreenRect(this, _glazeRect);
        var plate = Parent is WrapperView wrapper ? wrapper : (View)this;
        _layers.Clear();
        // The wrapper is the shadow plate. It is not a layer behind the glass.
        CollectBackdrop(plate, 0);
        var stamp = BackdropStamp();

        // Setting the backdrop bitmap schedules another frame. Skip that frame
        // when nothing behind the glass moved, so an idle page does not recapture.
        if (_skipNextCapture)
        {
            _skipNextCapture = false;
            if (stamp == _contentStamp)
                return;
        }

        var now = SystemClock.UptimeMillis();
        if (now - _lastCapture < 32)
            return;

        _lastCapture = now;
        ApplyMaterial();
        var width = Math.Max(1, Width / Sample);
        var height = Math.Max(1, Height / Sample);
        EnsureBitmap(width, height);
        if (_captured is null)
            return;

        _capturing = true;
        var captured = _captured;
        try
        {
            captured.EraseColor(Android.Graphics.Color.Transparent);
            var canvas = new Canvas(captured);
            canvas.Save();
            canvas.Scale(1f / Sample, 1f / Sample);
            PaintBackdrop(canvas);
            canvas.Restore();
        }
        finally
        {
            _capturing = false;
        }

        _contentStamp = stamp;
        _skipNextCapture = true;
        _backdrop.SetImageBitmap(_captured);
        ApplyBlurEffect();
    }

    // Walk from this view toward the root. Earlier siblings are in front of the
    // parent fill. An opaque fill that covers the glass ends the walk, so a bar
    // keeps the scroll view out. A sibling that does not cover is drawn whole,
    // clipped to its own bounds, which is what drops scrolled-away lines.
    void CollectBackdrop(View self, int depth)
    {
        if (depth > 32 || self.Parent is not ViewGroup parent)
            return;

        var behind = new List<OrderedChild>();
        var selfIndex = parent.IndexOfChild(self);
        var selfZ = self.GetZ();
        for (var i = 0; i < parent.ChildCount; i++)
        {
            var child = parent.GetChildAt(i);
            if (child is null || !Paints(child))
                continue;

            var childZ = child.GetZ();
            var isBehind = childZ < selfZ || (childZ == selfZ && i < selfIndex);
            if (!isBehind || !IntersectsGlaze(child))
                continue;

            behind.Add(new OrderedChild(child, i));
        }

        behind.Sort(static (a, b) =>
        {
            var z = a.View.GetZ().CompareTo(b.View.GetZ());
            return z != 0 ? z : a.Index.CompareTo(b.Index);
        });

        var cover = -1;
        for (var i = behind.Count - 1; i >= 0; i--)
        {
            if (CoversGlaze(behind[i].View))
            {
                cover = i;
                break;
            }
        }

        if (cover >= 0)
        {
            for (var i = cover; i < behind.Count; i++)
                _layers.Add(new BackdropLayer(behind[i].View, false));

            return;
        }

        var parentCovers = CoversGlaze(parent);
        if (!parentCovers)
            CollectBackdrop(parent, depth + 1);

        if (parentCovers || PaintsBackground(parent))
            _layers.Add(new BackdropLayer(parent, true));

        foreach (var child in behind)
            _layers.Add(new BackdropLayer(child.View, false));
    }

    void PaintBackdrop(Canvas canvas)
    {
        var glazeX = _glazeRect.Left;
        var glazeY = _glazeRect.Top;
        foreach (var layer in _layers)
        {
            if (layer.BackgroundOnly)
                PaintBackground(canvas, layer.View, glazeX, glazeY);
            else
                PaintView(canvas, layer.View, glazeX, glazeY);
        }
    }

    void PaintView(Canvas canvas, View view, int glazeX, int glazeY)
    {
        ScreenRect(view, _viewRect);
        var scrollX = view.ScrollX;
        var scrollY = view.ScrollY;
        var checkpoint = canvas.Save();
        // The parent applies the scroll before draw(). draw() itself keeps
        // children in content coordinates and shifts the fill back by the scroll.
        canvas.Translate(_viewRect.Left - glazeX - scrollX, _viewRect.Top - glazeY - scrollY);
        canvas.ClipRect(scrollX, scrollY, scrollX + view.Width, scrollY + view.Height);
        ApplyLayerAlpha(canvas, view, scrollX, scrollY, scrollX + view.Width, scrollY + view.Height);
        _drawDepth++;
        try
        {
            view.Draw(canvas);
        }
        finally
        {
            _drawDepth--;
            canvas.RestoreToCount(checkpoint);
        }
    }

    void PaintBackground(Canvas canvas, View view, int glazeX, int glazeY)
    {
        var background = view.Background;
        if (background is null)
            return;

        ScreenRect(view, _viewRect);
        var bounds = background.Bounds;
        var left = bounds.Left;
        var top = bounds.Top;
        var right = bounds.Right;
        var bottom = bounds.Bottom;
        var checkpoint = canvas.Save();
        try
        {
            canvas.Translate(_viewRect.Left - glazeX, _viewRect.Top - glazeY);
            canvas.ClipRect(0, 0, view.Width, view.Height);
            ApplyLayerAlpha(canvas, view, 0, 0, view.Width, view.Height);
            background.SetBounds(0, 0, view.Width, view.Height);
            background.Draw(canvas);
        }
        finally
        {
            background.SetBounds(left, top, right, bottom);
            canvas.RestoreToCount(checkpoint);
        }
    }

    void ApplyLayerAlpha(Canvas canvas, View view, int left, int top, int right, int bottom)
    {
        if (view.Alpha >= 0.99f)
            return;

        _layerPaint.Alpha = (int)(view.Alpha * 255f);
        canvas.SaveLayer(left, top, right, bottom, _layerPaint);
    }

    int BackdropStamp()
    {
        unchecked
        {
            var stamp = _layers.Count;
            foreach (var layer in _layers)
            {
                var view = layer.View;
                ScreenRect(view, _viewRect);
                stamp = (stamp * 31) + _viewRect.Left;
                stamp = (stamp * 31) + _viewRect.Top;
                stamp = (stamp * 31) + _viewRect.Right;
                stamp = (stamp * 31) + _viewRect.Bottom;
                stamp = (stamp * 31) + view.ScrollX;
                stamp = (stamp * 31) + view.ScrollY;
                stamp = (stamp * 31) + (int)(view.Alpha * 255f);
                stamp = (stamp * 31) + (layer.BackgroundOnly ? 1 : 2);
                if (view.Background is ColorDrawable color)
                    stamp = (stamp * 31) + color.Color.ToArgb();

                if (view is ViewGroup group)
                    stamp = (stamp * 31) + group.ChildCount;
            }

            return stamp;
        }
    }

    void ScreenRect(View view, Android.Graphics.Rect dest)
    {
        view.GetLocationOnScreen(_screen);
        dest.Set(_screen[0], _screen[1], _screen[0] + view.Width, _screen[1] + view.Height);
    }

    bool IntersectsGlaze(View view)
    {
        ScreenRect(view, _viewRect);
        return Android.Graphics.Rect.Intersects(_viewRect, _glazeRect);
    }

    bool CoversGlaze(View view)
    {
        if (!Paints(view) || view.Alpha < 0.99f || view.Background is not ColorDrawable color || color.Alpha != 255)
            return false;

        ScreenRect(view, _viewRect);
        return _viewRect.Contains(_glazeRect);
    }

    bool PaintsBackground(View view)
    {
        if (!Paints(view) || view.Background is null)
            return false;

        return IntersectsGlaze(view);
    }

    static bool Paints(View view)
        => view.Visibility == ViewStates.Visible && view.Alpha > 0f && view.Width > 0 && view.Height > 0;

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
        _material.Layout(0, 0, width, height);
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

        _backdrop.SetRenderEffect(RenderEffect.CreateBlurEffect(_radius, _radius, Shader.TileMode.Clamp!));
    }

    static int MaterialAlpha(float opacity)
        => (int)MathF.Round(opacity * 255f);

    void ApplyMaterial()
    {
        if (!_canBlur)
        {
            _material.SetBackgroundColor(Android.Graphics.Color.Transparent);
            return;
        }

        var night = Context?.Resources?.Configuration is { } configuration
            && (configuration.UiMode & UiMode.NightMask) == UiMode.NightYes;
        _material.SetBackgroundColor(night
            ? Android.Graphics.Color.Argb(MaterialAlpha(GlazeTokens.NightMaterialOpacity), 28, 28, 30)
            : Android.Graphics.Color.Argb(MaterialAlpha(GlazeTokens.LightMaterialOpacity), 243, 243, 243));
    }

    // MAUI blurs a software-canvas copy of this view for the shadow.
    // That copy is a solid rounded silhouette, matching the iOS shadow path.
    // The glass stays on the hardware pass.
    // A view group with nothing of its own to draw skips draw() and calls dispatchDraw(),
    // which would snapshot the rectangular children and square the shadow off at the corners.
    public override void Draw(Canvas canvas)
    {
        if (PaintShadowMask(canvas))
            return;

        base.Draw(canvas);
    }

    protected override void DispatchDraw(Canvas canvas)
    {
        if (PaintShadowMask(canvas))
            return;

        base.DispatchDraw(canvas);
    }

    bool PaintShadowMask(Canvas canvas)
    {
        if (canvas.IsHardwareAccelerated || VirtualView?.Shadow is null)
            return false;

        DrawShadowMask(canvas);
        return true;
    }

    void DrawShadowMask(Canvas canvas)
    {
        if (_corner > 0)
            canvas.DrawRoundRect(new RectF(0, 0, Width, Height), _corner, _corner, _shadowMask);
        else
            canvas.DrawRect(0, 0, Width, Height, _shadowMask);
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

    readonly struct BackdropLayer(View view, bool backgroundOnly)
    {
        public View View { get; } = view;
        public bool BackgroundOnly { get; } = backgroundOnly;
    }

    readonly struct OrderedChild(View view, int index)
    {
        public View View { get; } = view;
        public int Index { get; } = index;
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
