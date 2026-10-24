using Android.Views;
using AView = Android.Views.View;
using AViewGroup = Android.Views.ViewGroup;

namespace Reactor.Kit.Internals.Popup;

/// <summary>
/// Hosts the popup layer. A touch outside the panel closes it and falls through to the sheet.
/// A touch on the panel stays with the panel.
/// </summary>
sealed class OutsideRelay : AViewGroup
{
    public Action? Outside { get; set; }

    public AView? Panel { get; set; }

    public OutsideRelay(Android.Content.Context context)
        : base(context)
    {
        SetClipChildren(false);
        SetClipToPadding(false);
        Clickable = false;
    }

    public override bool DispatchTouchEvent(MotionEvent? e)
    {
        if (e?.ActionMasked == MotionEventActions.Down && !InsidePanel(e))
        {
            Outside?.Invoke();
            return false;
        }

        return base.DispatchTouchEvent(e);
    }

    protected override void OnLayout(bool changed, int l, int t, int r, int b)
    {
        var width = r - l;
        var height = b - t;
        for (var i = 0; i < ChildCount; i++)
            GetChildAt(i)?.Layout(0, 0, width, height);
    }

    bool InsidePanel(MotionEvent e)
    {
        if (Panel == null || !Panel.IsAttachedToWindow)
            return false;

        var rect = new Android.Graphics.Rect();
        if (!Panel.GetGlobalVisibleRect(rect))
            return false;

        return rect.Contains((int)e.RawX, (int)e.RawY);
    }
}
