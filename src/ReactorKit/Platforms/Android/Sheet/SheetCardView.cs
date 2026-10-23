using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Reactor.Kit.Internals.Sheet;
using AView = Android.Views.View;
using AViewGroup = Android.Views.ViewGroup;

namespace Reactor.Kit.Internals.Sheet;

/// <summary>The Android card. It tracks a drag and draws the grabber. It does not use BottomSheetBehavior.</summary>
sealed class SheetCardView : AViewGroup
{
    readonly List<(long Time, float Y)> _samples = [];
    readonly AView _grabber;
    readonly AView _scrim;
    readonly int[] _screen = new int[2];
    bool _down;
    bool _grabberHit;
    float _downX;
    float _downY;

    public SheetCardView(Android.Content.Context context)
        : base(context)
    {
        SetClipToPadding(false);
        SetClipChildren(false);
        _scrim = new AView(context);
        _scrim.SetBackgroundColor(Android.Graphics.Color.Black);
        _scrim.Alpha = 0;
        _scrim.Clickable = false;
        _scrim.Focusable = false;
        _scrim.Visibility = ViewStates.Gone;
        _grabber = new AView(context);
        AddView(_scrim);
        AddView(_grabber);
    }

    public Action<SheetPointer>? Pointer { get; set; }

    public Action? Escape { get; set; }

    public bool AcceptPointer { get; set; } = true;

    /// <summary>Detaches the sheet content and leaves the grabber on the card.</summary>
    public void ReleaseContent()
    {
        for (var i = ChildCount - 1; i >= 0; i--)
        {
            var child = GetChildAt(i);
            if (child == null || IsChrome(child))
                continue;

            RemoveViewAt(i);
        }
    }

    public void ApplyChrome(bool grabber, double radiusDip, bool dark)
    {
        var density = Density();
        var fill = dark ? Android.Graphics.Color.ParseColor("#1C1C1E") : Android.Graphics.Color.White;
        Background = new ColorDrawable(fill);
        Elevation = 8 * density;
        OutlineProvider = new RoundOutline(Math.Max(0, (float)(radiusDip * density)));
        ClipToOutline = radiusDip > 0;
        _grabber.Visibility = grabber ? ViewStates.Visible : ViewStates.Gone;
        var ink = dark
            ? Android.Graphics.Color.Argb(77, 255, 255, 255)
            : Android.Graphics.Color.Argb(77, 60, 60, 67);
        var pill = new GradientDrawable();
        pill.SetCornerRadius(2.5f * density);
        pill.SetColor(ink);
        _grabber.Background = pill;
    }

    /// <summary>Fades the grabber. A stacked sheet keeps only one handle solid.</summary>
    public void SetGrabberAlpha(float alpha)
    {
        var shown = alpha > 0.01f;
        _grabber.Alpha = alpha;
        _grabber.Visibility = shown ? ViewStates.Visible : ViewStates.Gone;
    }

    /// <summary>Dims the card while another sheet covers it. Zero leaves the card solid.</summary>
    public void SetCover(float amount)
    {
        var alpha = Math.Clamp(amount, 0f, 1f) * 0.09f;
        _scrim.Alpha = alpha;
        _scrim.Visibility = alpha > 0.004f ? ViewStates.Visible : ViewStates.Gone;
    }

    public override bool DispatchKeyEvent(KeyEvent? e)
    {
        if (e is { KeyCode: Keycode.Escape, Action: KeyEventActions.Down })
        {
            Escape?.Invoke();
            return true;
        }

        return base.DispatchKeyEvent(e);
    }

    public override bool OnInterceptTouchEvent(MotionEvent? ev)
    {
        if (!AcceptPointer || ev == null)
            return false;

        if (ev.Action == MotionEventActions.Down)
        {
            _down = true;
            _downX = ev.GetX();
            _downY = ev.GetY();
            // Header buttons sit in this band. A finger moves a few pixels, and taking that move cancels the click.
            _grabberHit = _downY <= 44 * Density() && !OverControl(ev.GetX(), ev.GetY());
            _samples.Clear();
            Remember(ev);
            Emit(ev, down: true, up: false);
            return false;
        }

        if (!_down)
            return false;

        if (ev.Action is MotionEventActions.Move && _grabberHit && PastSlop(ev))
            return true;

        return false;
    }

    public override bool OnTouchEvent(MotionEvent? ev)
    {
        if (!AcceptPointer || ev == null || !_down)
            return false;

        Remember(ev);
        var up = ev.Action is MotionEventActions.Up or MotionEventActions.Cancel;
        Emit(ev, down: false, up);
        if (up)
            _down = false;

        return true;
    }

    protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
    {
        var width = MeasureSpec.GetSize(widthMeasureSpec);
        var height = MeasureSpec.GetSize(heightMeasureSpec);
        SetMeasuredDimension(width, height);
        var density = Density();
        var grabberW = (int)(36 * density);
        var grabberH = (int)(5 * density);
        _grabber.Measure(
            MeasureSpec.MakeMeasureSpec(grabberW, MeasureSpecMode.Exactly),
            MeasureSpec.MakeMeasureSpec(grabberH, MeasureSpecMode.Exactly));
        for (var i = 0; i < ChildCount; i++)
        {
            var child = GetChildAt(i);
            if (child == null || ReferenceEquals(child, _grabber))
                continue;

            child.Measure(
                MeasureSpec.MakeMeasureSpec(width, MeasureSpecMode.Exactly),
                MeasureSpec.MakeMeasureSpec(height, MeasureSpecMode.Exactly));
        }
    }

    protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
    {
        var width = right - left;
        var height = bottom - top;
        var density = Density();
        var grabberW = (int)(36 * density);
        var grabberH = (int)(5 * density);
        var grabberLeft = (width - grabberW) / 2;
        var grabberTop = (int)(5 * density);
        _grabber.Layout(grabberLeft, grabberTop, grabberLeft + grabberW, grabberTop + grabberH);
        for (var i = 0; i < ChildCount; i++)
        {
            var child = GetChildAt(i);
            if (child == null || ReferenceEquals(child, _grabber))
                continue;

            child.Layout(0, 0, width, height);
        }

        BringChildToFront(_scrim);
        BringChildToFront(_grabber);
        // A popup inside the card sets its own elevation. Keep it above the grabber.
        for (var i = 0; i < ChildCount; i++)
        {
            var child = GetChildAt(i);
            if (child == null || IsChrome(child) || child.Elevation <= 0)
                continue;

            BringChildToFront(child);
            break;
        }
    }

    void Emit(MotionEvent ev, bool down, bool up)
    {
        var density = Density();
        var y = ScreenY(ev) / density;
        var hit = Hit(ev.GetX(), ev.GetY());
        var hasScroll = false;
        var atTop = true;
        var current = hit;
        while (current != null && !ReferenceEquals(current, this))
        {
            if (current.CanScrollVertically(1) || current.CanScrollVertically(-1))
            {
                hasScroll = true;
                atTop = !current.CanScrollVertically(-1);
                break;
            }

            current = current.Parent as AView;
        }

        Pointer?.Invoke(new SheetPointer
        {
            Down = down,
            Up = up,
            Y = y,
            VelocityY = up ? Velocity() / density : 0,
            InGrabber = _grabberHit,
            HasScroll = hasScroll,
            ScrollAtTop = atTop,
        });
    }

    /// <summary>Finger Y in screen pixels. The card moves under the finger, so the view's own Y would step the drag backward.</summary>
    float ScreenY(MotionEvent ev)
    {
        GetLocationOnScreen(_screen);
        return _screen[1] + ev.GetY();
    }

    void Remember(MotionEvent ev)
    {
        var now = SystemClock();
        _samples.Add((now, ScreenY(ev)));
        var cutoff = now - 100;
        while (_samples.Count > 2 && _samples[0].Time < cutoff)
            _samples.RemoveAt(0);
    }

    float Velocity()
    {
        if (_samples.Count < 2)
            return 0;

        var last = _samples[^1];
        var past = _samples[0];
        var dt = (last.Time - past.Time) / 1000f;
        if (dt <= 0)
            return 0;

        return (last.Y - past.Y) / dt;
    }

    AView? Hit(float x, float y)
    {
        for (var i = ChildCount - 1; i >= 0; i--)
        {
            var child = GetChildAt(i);
            if (child == null || IsChrome(child))
                continue;

            if (x < child.Left || x >= child.Right || y < child.Top || y >= child.Bottom)
                continue;

            if (child is AViewGroup group)
            {
                var nested = HitIn(group, x - child.Left, y - child.Top);
                if (nested != null)
                    return nested;
            }

            return child;
        }

        return null;
    }

    static AView? HitIn(AViewGroup group, float x, float y)
    {
        for (var i = group.ChildCount - 1; i >= 0; i--)
        {
            var child = group.GetChildAt(i);
            if (child == null)
                continue;

            if (x < child.Left || x >= child.Right || y < child.Top || y >= child.Bottom)
                continue;

            if (child is AViewGroup nested)
            {
                var found = HitIn(nested, x - child.Left, y - child.Top);
                if (found != null)
                    return found;
            }

            return child;
        }

        return group;
    }

    bool PastSlop(MotionEvent ev)
    {
        var dx = ev.GetX() - _downX;
        var dy = ev.GetY() - _downY;
        var slop = TouchSlop();
        return dx * dx + dy * dy > slop * slop;
    }

    int TouchSlop()
    {
        var slop = ViewConfiguration.Get(Context!)?.ScaledTouchSlop ?? 0;
        return slop > 0 ? slop : (int)(8 * Density());
    }

    bool OverControl(float x, float y)
    {
        var current = Hit(x, y);
        while (current != null && !ReferenceEquals(current, this))
        {
            if (current.Clickable || current.LongClickable)
                return true;

            current = current.Parent as AView;
        }

        return false;
    }

    bool IsChrome(AView child)
        => ReferenceEquals(child, _grabber) || ReferenceEquals(child, _scrim);

    float Density()
    {
        var density = Resources?.DisplayMetrics?.Density ?? 0;
        return density > 0 ? density : 1;
    }

    static long SystemClock()
        => Android.OS.SystemClock.UptimeMillis();

    sealed class RoundOutline : ViewOutlineProvider
    {
        readonly float _radius;

        public RoundOutline(float radius)
            => _radius = radius;

        public override void GetOutline(AView? view, Outline? outline)
        {
            if (view == null || outline == null)
                return;

            outline.SetRoundRect(0, 0, view.Width, view.Height, _radius);
        }
    }
}
