using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Reactor.Kit.Internals.Popup;

sealed class WindowsPopupLayer : IPopupLayer
{
    Panel? _pageView;
    FrameworkElement? _overlayView;
    Action? _onEscape;

    public void Insert(Page page, View overlay, VisualElement anchor)
    {
        _ = anchor;
        var context = Context(page);
        var nativePage = page.ToPlatform(context);
        _pageView = nativePage as Panel
            ?? throw new InvalidOperationException("The page native root cannot host the panel.");
        _overlayView = overlay.ToPlatform(context) as FrameworkElement
            ?? throw new InvalidOperationException("The panel has no native view.");
        _overlayView.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
        _overlayView.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch;
        _pageView.Children.Add(_overlayView);
    }

    public void Remove(View overlay)
    {
        ReleaseEscape();
        var native = overlay.Handler?.PlatformView as FrameworkElement ?? _overlayView;
        if (_pageView != null && native != null)
            _pageView.Children.Remove(native);

        _overlayView = null;
        _pageView = null;
    }

    public void Sync(Page page, View overlay)
    {
        if (_pageView == null || _overlayView == null)
            return;

        _overlayView.Width = _pageView.ActualWidth;
        _overlayView.Height = _pageView.ActualHeight;
        _ = page;
        _ = overlay;
    }

    public void SetClip(View view, bool clip)
    {
        if (clip || view.Handler?.PlatformView is not UIElement element)
            return;

        element.Clip = null;
    }

    public void ReleaseShadowClip(View panel)
    {
    }

    public Rect PageBounds(Page page, VisualElement anchor)
    {
        _ = anchor;
        var view = ViewOf(page);
        return new Rect(0, 0, view.ActualWidth, view.ActualHeight);
    }

    public Rect AnchorBounds(VisualElement anchor, Page page)
    {
        var context = Context(page);
        var anchorView = anchor.ToPlatform(context) as FrameworkElement;
        var pageView = ViewOf(page);
        if (anchorView == null)
            return Rect.Zero;

        var transform = anchorView.TransformToVisual(pageView);
        if (transform == null)
            return Rect.Zero;

        var point = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
        return new Rect(point.X, point.Y, anchorView.ActualWidth, anchorView.ActualHeight);
    }

    public Thickness SafeInsets(Page page, VisualElement anchor)
    {
        _ = anchor;
        _ = page;
        return new Thickness(0);
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
        if (_pageView == null)
            return;

        _onEscape = onEscape;
        _pageView.PreviewKeyDown += OnKeyDown;
    }

    public void ReleaseEscape()
    {
        if (_pageView != null)
            _pageView.PreviewKeyDown -= OnKeyDown;

        _onEscape = null;
    }

    public void CaptureOutside(Action onOutside)
    {
    }

    public void ReleaseOutside()
    {
    }

    void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != Windows.System.VirtualKey.Escape)
            return;

        e.Handled = true;
        _onEscape?.Invoke();
    }

    FrameworkElement ViewOf(Page page)
        => page.ToPlatform(Context(page)) as FrameworkElement
            ?? throw new InvalidOperationException("The page has no native view.");

    static IMauiContext Context(Page page)
        => page.Handler?.MauiContext ?? throw new InvalidOperationException("The page is not mounted.");
}
