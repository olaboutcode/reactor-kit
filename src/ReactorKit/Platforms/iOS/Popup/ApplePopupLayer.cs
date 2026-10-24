using Microsoft.Maui.Platform;
using UIKit;

namespace Reactor.Kit.Internals.Popup;

sealed class ApplePopupLayer : IPopupLayer, IDisposable
{
    UIView? _pageView;
    UIView? _overlayView;
    OutsideRelay? _relay;
    OutsideMonitor? _monitor;
    EscapeMonitor? _escape;

    public void Insert(Page page, View overlay, VisualElement anchor)
    {
        var context = Context(page);
        _pageView = Host(page, anchor);
        _overlayView = overlay.ToPlatform(context);
        _overlayView.Frame = _pageView.Bounds;
        _overlayView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
        _relay = new OutsideRelay
        {
            Frame = _pageView.Bounds,
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
            Panel = PanelView(overlay, context),
        };
        _relay.AddSubview(_overlayView);
        _pageView.AddSubview(_relay);
        _pageView.BringSubviewToFront(_relay);
    }

    public void Remove(View overlay)
    {
        ReleaseOutside();
        ReleaseEscape();
        _overlayView?.RemoveFromSuperview();
        _relay?.RemoveFromSuperview();
        _relay?.Dispose();
        _relay = null;
        _overlayView = null;
        _pageView = null;
        _ = overlay;
    }

    public void Sync(Page page, View overlay)
    {
        if (_pageView == null || _overlayView == null || _relay == null)
            return;

        _relay.Frame = _pageView.Bounds;
        _overlayView.Frame = _relay.Bounds;
        _pageView.BringSubviewToFront(_relay);
        var panel = PanelView(overlay, Context(page));
        _relay.Panel = panel;
        if (_monitor != null)
            _monitor.Panel = panel;

        _ = page;
    }

    public void SetClip(View view, bool clip)
    {
        if (view.Handler?.PlatformView is UIView native)
            native.ClipsToBounds = clip;
    }

    public void ReleaseShadowClip(View panel)
    {
    }

    public Rect PageBounds(Page page, VisualElement anchor)
    {
        var view = _pageView ?? Host(page, anchor);
        return new Rect(0, 0, view.Bounds.Width, view.Bounds.Height);
    }

    public Rect AnchorBounds(VisualElement anchor, Page page)
    {
        var anchorView = anchor.ToPlatform(Context(page));
        var host = _pageView ?? Host(page, anchor);
        var rect = anchorView.ConvertRectToView(anchorView.Bounds, host);
        return new Rect(rect.X, rect.Y, rect.Width, rect.Height);
    }

    public Thickness SafeInsets(Page page, VisualElement anchor)
    {
        var view = _pageView ?? Host(page, anchor);
        var insets = view.SafeAreaInsets;
        return new Thickness(insets.Left, insets.Top, insets.Right, insets.Bottom);
    }

    /// <summary>
    /// The page view, or the presented sheet view when the anchor lives in that sheet.
    /// A sheet is not a subview of the page, so a layer on the page stays behind the card.
    /// </summary>
    static UIView Host(Page page, VisualElement anchor)
    {
        var context = Context(page);
        var pageView = page.ToPlatform(context);
        if (anchor.Handler == null)
            return pageView;

        var anchorView = anchor.ToPlatform(context);
        var sheet = PresentedHost(anchorView, pageView);
        return sheet ?? pageView;
    }

    /// <summary>
    /// The root view of the presented controller that contains <paramref name="anchorView"/>.
    /// A child of the page stays on the page.
    /// </summary>
    static UIView? PresentedHost(UIView anchorView, UIView pageView)
    {
        var anchorController = Controller(anchorView);
        var pageController = Controller(pageView);
        if (anchorController?.View == null || anchorController == pageController)
            return null;

        var presented = anchorController;
        while (presented.ParentViewController is { } parent && parent != pageController)
            presented = parent;

        if (presented.ParentViewController != null || presented.PresentingViewController == null)
            return null;

        return presented.View is { Window: not null } host ? host : null;
    }

    static UIViewController? Controller(UIView view)
    {
        UIResponder? next = view;
        while (next != null)
        {
            if (next is UIViewController found)
                return found;

            next = next.NextResponder;
        }

        return null;
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

    public void CaptureOutside(Action onOutside)
    {
        ReleaseOutside();
        if (_pageView == null || _relay == null)
            return;

        _monitor = new OutsideMonitor
        {
            Outside = onOutside,
            Panel = _relay.Panel,
        };
        _pageView.AddGestureRecognizer(_monitor);
    }

    public void ReleaseOutside()
    {
        if (_pageView != null && _monitor != null)
            _pageView.RemoveGestureRecognizer(_monitor);

        if (_monitor != null)
        {
            _monitor.Outside = null;
            _monitor.Panel = null;
            _monitor.Dispose();
        }

        _monitor = null;
    }

    public void Dispose()
    {
        ReleaseOutside();
        ReleaseEscape();
    }

    static UIView? PanelView(View overlay, IMauiContext context)
    {
        if (overlay is not Layout layout || layout.Children.Count < 2 || layout.Children[1] is not View shell)
            return null;

        return shell.ToPlatform(context);
    }

    static IMauiContext Context(Page page)
        => page.Handler?.MauiContext ?? throw new InvalidOperationException("The page is not mounted.");
}
