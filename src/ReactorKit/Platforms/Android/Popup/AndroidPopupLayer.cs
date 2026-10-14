using Microsoft.Maui.Platform;
using AView = Android.Views.View;
using AViewGroup = Android.Views.ViewGroup;

namespace Reactor.Kit.Internals.Popup;

sealed class AndroidPopupLayer : IPopupLayer, IDisposable
{
    AViewGroup? _pageView;
    AView? _overlayView;
    AndroidBackStop? _back;
    Action? _onEscape;

    public void Insert(Page page, View overlay)
    {
        var context = Context(page);
        _pageView = page.ToPlatform(context) as AViewGroup
            ?? throw new InvalidOperationException("The page has no native view.");
        _overlayView = overlay.ToPlatform(context);
        _overlayView.LayoutParameters = new AViewGroup.LayoutParams(
            AViewGroup.LayoutParams.MatchParent,
            AViewGroup.LayoutParams.MatchParent);
        _pageView.AddView(_overlayView);
        _overlayView.BringToFront();
    }

    public void Remove(View overlay)
    {
        ReleaseBack();
        ReleaseEscape();
        if (_pageView != null && _overlayView != null)
            _pageView.RemoveView(_overlayView);

        _overlayView = null;
        _pageView = null;
        _ = overlay;
    }

    public void Sync(Page page, View overlay)
    {
        if (_pageView == null || _overlayView == null)
            return;

        _overlayView.Layout(0, 0, _pageView.Width, _pageView.Height);
        _overlayView.BringToFront();
        _ = page;
        _ = overlay;
    }

    public void SetClip(View view, bool clip)
    {
        if (view.Handler?.PlatformView is not AViewGroup group)
            return;

        group.SetClipChildren(clip);
        group.SetClipToPadding(clip);
    }

    public Rect PageBounds(Page page)
    {
        var view = page.ToPlatform(Context(page));
        var density = Density(view);
        return new Rect(0, 0, view.Width / density, view.Height / density);
    }

    public Rect AnchorBounds(VisualElement anchor, Page page)
    {
        var context = Context(page);
        var anchorView = anchor.ToPlatform(context);
        var pageView = page.ToPlatform(context);
        if (!anchorView.IsAttachedToWindow || !pageView.IsAttachedToWindow)
            return Rect.Zero;

        var density = Density(pageView);
        var anchorLocation = new int[2];
        var pageLocation = new int[2];
        anchorView.GetLocationOnScreen(anchorLocation);
        pageView.GetLocationOnScreen(pageLocation);
        return new Rect(
            (anchorLocation[0] - pageLocation[0]) / density,
            (anchorLocation[1] - pageLocation[1]) / density,
            anchorView.Width / density,
            anchorView.Height / density);
    }

    public Thickness SafeInsets(Page page)
    {
        var pageView = page.ToPlatform(Context(page));
        var decor = Platform.CurrentActivity?.Window?.DecorView;
        if (decor == null)
            return new Thickness(0);

        var visible = new Android.Graphics.Rect();
        decor.GetWindowVisibleDisplayFrame(visible);
        var location = new int[2];
        pageView.GetLocationOnScreen(location);
        var density = Density(pageView);
        var top = Math.Max(0, visible.Top - location[1]) / density;
        var left = Math.Max(0, visible.Left - location[0]) / density;
        var bottom = Math.Max(0, location[1] + pageView.Height - visible.Bottom) / density;
        var right = Math.Max(0, location[0] + pageView.Width - visible.Right) / density;
        return new Thickness(left, top, right, bottom);
    }

    public void CaptureBack(Action onBack)
    {
        ReleaseBack();
        if (Platform.CurrentActivity is not AndroidX.Activity.ComponentActivity activity)
            return;

        _back = new AndroidBackStop(onBack);
        activity.OnBackPressedDispatcher.AddCallback(_back);
    }

    public void ReleaseBack()
    {
        var back = _back;
        _back = null;
        if (back == null)
            return;

        back.Remove();
        back.Dispose();
    }

    public void Dispose()
        => ReleaseBack();

    public void CaptureEscape(Action onEscape)
    {
        ReleaseEscape();
        if (_overlayView == null)
            return;

        _onEscape = onEscape;
        _overlayView.Focusable = true;
        _overlayView.FocusableInTouchMode = true;
        _overlayView.KeyPress += OnKeyPress;
        _overlayView.RequestFocus();
    }

    public void ReleaseEscape()
    {
        if (_overlayView != null)
            _overlayView.KeyPress -= OnKeyPress;

        _onEscape = null;
    }

    void OnKeyPress(object? sender, AView.KeyEventArgs e)
    {
        if (e.Event?.Action != Android.Views.KeyEventActions.Down || e.KeyCode != Android.Views.Keycode.Escape)
            return;

        e.Handled = true;
        _onEscape?.Invoke();
    }

    static IMauiContext Context(Page page)
        => page.Handler?.MauiContext ?? throw new InvalidOperationException("The page is not mounted.");

    static double Density(AView view)
    {
        var density = view.Resources?.DisplayMetrics?.Density ?? 0;
        if (density > 0)
            return density;

        var display = DeviceDisplay.MainDisplayInfo.Density;
        return display > 0 ? display : 1;
    }
}
