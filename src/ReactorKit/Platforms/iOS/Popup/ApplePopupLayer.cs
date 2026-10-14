using Microsoft.Maui.Platform;
using UIKit;

namespace Reactor.Kit.Internals.Popup;

sealed class ApplePopupLayer : IPopupLayer, IDisposable
{
    UIView? _pageView;
    UIView? _overlayView;
    EscapeMonitor? _escape;

    public void Insert(Page page, View overlay)
    {
        var context = page.Handler?.MauiContext
            ?? throw new InvalidOperationException("The page is not mounted.");
        _pageView = page.ToPlatform(context);
        _overlayView = overlay.ToPlatform(context);
        _overlayView.Frame = _pageView.Bounds;
        _overlayView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
        _pageView.AddSubview(_overlayView);
        _pageView.BringSubviewToFront(_overlayView);
    }

    public void Remove(View overlay)
    {
        ReleaseEscape();
        _overlayView?.RemoveFromSuperview();
        _overlayView = null;
        _pageView = null;
        _ = overlay;
    }

    public void Sync(Page page, View overlay)
    {
        if (_pageView == null || _overlayView == null)
            return;

        _overlayView.Frame = _pageView.Bounds;
        _pageView.BringSubviewToFront(_overlayView);
        _ = page;
        _ = overlay;
    }

    public void SetClip(View view, bool clip)
    {
        if (view.Handler?.PlatformView is UIView native)
            native.ClipsToBounds = clip;
    }

    public Rect PageBounds(Page page)
    {
        var view = page.ToPlatform(Context(page));
        return new Rect(0, 0, view.Bounds.Width, view.Bounds.Height);
    }

    public Rect AnchorBounds(VisualElement anchor, Page page)
    {
        var context = Context(page);
        var anchorView = anchor.ToPlatform(context);
        var pageView = page.ToPlatform(context);
        var rect = anchorView.ConvertRectToView(anchorView.Bounds, pageView);
        return new Rect(rect.X, rect.Y, rect.Width, rect.Height);
    }

    public Thickness SafeInsets(Page page)
    {
        var insets = page.ToPlatform(Context(page)).SafeAreaInsets;
        return new Thickness(insets.Left, insets.Top, insets.Right, insets.Bottom);
    }

    public void CaptureBack(Action onBack)
    {
    }

    public void ReleaseBack()
    {
    }

    public void CaptureEscape(Action onEscape)
    {
        ReleaseEscape();
        if (_overlayView == null)
            return;

        _escape = new EscapeMonitor { Escaped = onEscape };
        _overlayView.AddSubview(_escape);
        _escape.BecomeFirstResponder();
    }

    public void ReleaseEscape()
    {
        var escape = _escape;
        _escape = null;
        if (escape == null)
            return;

        escape.Escaped = null;
        escape.ResignFirstResponder();
        escape.RemoveFromSuperview();
        escape.Dispose();
    }

    public void Dispose()
        => ReleaseEscape();

    static IMauiContext Context(Page page)
        => page.Handler?.MauiContext ?? throw new InvalidOperationException("The page is not mounted.");
}
