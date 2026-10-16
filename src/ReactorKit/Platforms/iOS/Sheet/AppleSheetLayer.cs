using Microsoft.Maui.Platform;
using UIKit;

namespace Reactor.Kit.Internals.Sheet;

/// <summary>
/// Presents the MAUI body with <see cref="UISheetPresentationController"/>. The kit does not draw a second card.
/// </summary>
sealed class AppleSheetLayer : ISheetLayer
{
    SheetController? _controller;
    Page? _page;
    View? _host;
    Action? _metrics;
    Action? _back;
    Action? _escape;

    public bool PositionsCard => false;

    public bool TryAttach(Page page, View host)
    {
        if (page.Handler?.MauiContext == null || Presenter(page) == null)
            return false;

        _page = page;
        _host = host;
        _controller ??= new SheetController();
        _controller.MetricsChanged += OnMetrics;
        return true;
    }

    public void Detach()
    {
        if (_controller != null)
            _controller.MetricsChanged -= OnMetrics;

        _page = null;
        _host = null;
        _metrics = null;
        _back = null;
        _escape = null;
    }

    public bool TryMeasure(Page page, out SheetMetrics metrics)
    {
        metrics = new SheetMetrics();
        var context = page.Handler?.MauiContext;
        if (context == null)
            return false;

        var view = page.ToPlatform(context);
        if (view.Bounds.Width <= 0 || view.Bounds.Height <= 0)
            return false;

        var traits = (page.Handler as IPlatformViewHandler)?.ViewController?.TraitCollection;
        var insets = view.SafeAreaInsets;
        metrics = new SheetMetrics
        {
            Width = view.Bounds.Width,
            Height = view.Bounds.Height,
            Safe = new Thickness(insets.Left, insets.Top, insets.Right, insets.Bottom),
            CompactWidth = traits?.HorizontalSizeClass == UIUserInterfaceSizeClass.Compact,
            CompactHeight = traits?.VerticalSizeClass == UIUserInterfaceSizeClass.Compact,
        };
        return true;
    }

    public void Apply(SheetFrame frame)
        => _ = frame;

    public void Watch(Action? metricsChanged, Action? back, Action? escape)
    {
        _metrics = metricsChanged;
        _back = back;
        _escape = escape;
    }

    public void PresentNative(SheetNative native)
    {
        if (_controller == null || _page == null)
            return;

        _controller.Configure(native);
        var presenter = Presenter(_page);
        if (presenter == null)
            return;

        _controller.Show(presenter);
        _ = _back;
        _ = _escape;
    }

    public void UpdateNative(SheetNative native)
    {
        if (_controller == null || _page == null)
            return;

        _controller.Configure(native);
        if (_controller.PresentingViewController != null)
            return;

        var presenter = Presenter(_page);
        if (presenter != null)
            _controller.Show(presenter);
    }

    public void DismissNative()
        => _controller?.Close();

    void OnMetrics(object? sender, EventArgs args)
    {
        _ = sender;
        _ = args;
        _metrics?.Invoke();
    }

    static UIViewController? Presenter(Page page)
    {
        var handler = page.Handler as IPlatformViewHandler;
        var controller = handler?.ViewController;
        if (controller != null)
            return controller;

        var context = page.Handler?.MauiContext;
        if (context == null)
            return null;

        var view = page.ToPlatform(context);
        var next = view.NextResponder;
        while (next != null)
        {
            if (next is UIViewController found)
                return found;

            next = next.NextResponder;
        }

        return null;
    }
}
