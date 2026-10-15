using Reactor.Kit.PopupMenuTokens;

namespace Reactor.Kit.Internals.Popup;

/// <summary>
/// Shows the panel above the current page, tracks the anchor, and dismisses once.
/// </summary>
internal sealed class PopupSession
{
    const int MaxAttempts = 8;

    readonly PopupSurface _surface;
    readonly IPopupLayer _layer = PopupLayers.Create();
    readonly List<VisualElement> _sized = [];
    readonly List<ScrollView> _scrolls = [];

    Grid? _overlay;
    BoxView? _dismiss;
    ContentView? _shell;
    ScrollView? _scroll;
    ContentView? _panel;

    Phase _phase = Phase.Closed;
    int _ticket;
    int _attempts;
    bool _wantOpen;
    bool _raiseDismiss;
    bool _inserted;
    bool _scheduled;
    bool _inLayout;
    bool _animated;
    bool _seenAnchor;

    public PopupSession(PopupSurface surface)
    {
        _surface = surface;
    }

    public void SetOpen(bool open)
    {
        if (open)
        {
            _wantOpen = true;
            _attempts = 0;
            Present();
            return;
        }

        _wantOpen = false;
        if (_phase is Phase.Open or Phase.Opening)
            BeginClose(raiseDismiss: false);
    }

    public void AttachPanel(ContentView panel)
    {
        if (ReferenceEquals(_panel, panel))
            return;

        UnsubscribePanel();
        _panel = panel;
        panel.SizeChanged += OnPanelSized;
        if (_scroll != null)
            _scroll.Content = panel;

        if (_wantOpen)
            Present();
    }

    public void DetachPanel(ContentView? panel)
    {
        if (panel != null && !ReferenceEquals(_panel, panel))
            return;

        UnsubscribePanel();
        _panel = null;
        if (_scroll != null)
            _scroll.Content = null;

        if (_phase != Phase.Closed)
            Close();
    }

    public void Relayout()
    {
        if (_phase is Phase.Open or Phase.Opening)
            Place();
    }

    public void Close()
    {
        _wantOpen = false;
        _raiseDismiss = false;
        _ticket++;
        TearDown();
        _phase = Phase.Closed;
    }

    void Present()
    {
        if (!_wantOpen || _panel == null)
            return;

        if (_phase == Phase.Closing || _phase == Phase.Closed || !_inserted)
        {
            Open();
            return;
        }

        Place();
    }

    void Open()
    {
        _ticket++;
        _raiseDismiss = false;
        _animated = false;
        _seenAnchor = false;
        _phase = Phase.Opening;
        EnsureOverlay();
        if (_scroll != null && _panel != null)
            _scroll.Content = _panel;

        if (_shell != null)
        {
            _shell.Opacity = 0;
            _shell.ScaleX = OpenScaleX();
            _shell.ScaleY = OpenScaleY();
        }

        var page = FindPage();
        if (page?.Handler?.MauiContext == null || _overlay == null)
        {
            Schedule();
            return;
        }

        if (!_inserted)
        {
            _layer.Insert(page, _overlay);
            _layer.CaptureBack(RequestDismiss);
            _layer.CaptureEscape(RequestDismiss);
            Watch();
            _inserted = true;
        }

        Place();
    }

    void Place()
    {
        if (_inLayout || _overlay == null || _shell == null || _panel == null || _scroll == null)
            return;

        if (_phase is Phase.Closed or Phase.Closing)
            return;

        if (!_inserted)
        {
            Open();
            return;
        }

        var page = FindPage();
        if (page == null)
        {
            Schedule();
            return;
        }

        _inLayout = true;
        try
        {
            if (TryFrame(page, out var frame, out var window))
                Apply(page, frame, window);
        }
        finally
        {
            _inLayout = false;
        }
    }

    bool TryFrame(Page page, out PopupFrame frame, out Rect window)
    {
        frame = default;
        window = _layer.PageBounds(page);
        if (window.Width <= 0 || window.Height <= 0)
        {
            Schedule();
            return false;
        }

        var safe = _layer.SafeInsets(page);
        var gap = PopupPlacement.AtLeastZero(_surface.Gap);
        var maxWidth = Math.Max(0, window.Width - safe.Left - safe.Right - (gap * 2));
        var desired = MeasurePanel(maxWidth);
        if (desired.Width <= 0 && desired.Height <= 0)
        {
            Schedule();
            return false;
        }

        frame = PopupPlacement.Place(
            _layer.AnchorBounds(_surface, page),
            desired,
            window,
            safe,
            _surface.Alignment,
            gap,
            _surface.MinWidth,
            _surface.MinHeight);
        if (frame.AnchorVisible)
            return true;

        if (_seenAnchor)
            RequestDismiss();
        else
            Schedule();

        return false;
    }

    void Apply(Page page, PopupFrame frame, Rect window)
    {
        if (_shell == null || _panel == null || _overlay == null || _scroll == null)
            return;

        _seenAnchor = true;
        _attempts = 0;
        _shell.AnchorX = AnchorX(_surface.Alignment);
        _shell.AnchorY = frame.Above ? 1 : 0;
        _shell.Margin = new Thickness(frame.Panel.X, frame.Panel.Y, 0, 0);
        _shell.WidthRequest = frame.Panel.Width;
        _shell.HeightRequest = frame.Panel.Height;
        _panel.WidthRequest = frame.Panel.Width;
        Arrange(window);
        _layer.Sync(page, _overlay);
        // The shell is the panel's size. A scroll view clips to that rectangle and cuts off a shadow.
        // Clip only when the content is taller than the panel and has to scroll.
        _layer.SetClip(_scroll, frame.Scrolls);
        _layer.SetClip(_shell, frame.Scrolls);
        _layer.SetClip(_panel, false);
        if (_animated)
            return;

        _animated = true;
        Run(AnimateOpen(_ticket));
    }

    void Arrange(Rect window)
    {
        if (_overlay == null)
            return;

        _overlay.WidthRequest = window.Width;
        _overlay.HeightRequest = window.Height;
        var laid = (IView)_overlay;
        laid.Measure(window.Width, window.Height);
        laid.Arrange(new Rect(0, 0, window.Width, window.Height));
    }

    Size MeasurePanel(double maxWidth)
    {
        if (_panel?.Handler == null)
            return Size.Zero;

        _panel.WidthRequest = -1;
        var natural = ((IView)_panel).Measure(double.PositiveInfinity, double.PositiveInfinity);
        var width = Finite(natural.Width);
        var height = Finite(natural.Height);
        if (width <= 0 || height <= 0)
        {
            var bounded = ((IView)_panel).Measure(Math.Max(maxWidth, 1), double.PositiveInfinity);
            if (width <= 0)
                width = Finite(bounded.Width);
            if (height <= 0)
                height = Finite(bounded.Height);
        }

        if (width <= 0 && height <= 0)
            return new Size(PopupPlacement.AtLeastZero(_surface.MinWidth), PopupPlacement.AtLeastZero(_surface.MinHeight));

        if (height <= 0)
            height = PopupPlacement.AtLeastZero(_surface.MinHeight);

        return new Size(width, height);
    }

    void EnsureOverlay()
    {
        if (_overlay != null)
            return;

        _dismiss = new BoxView
        {
            // A fully clear box misses taps on Android. One step of white stays invisible.
            Color = Color.FromRgba(255, 255, 255, 1),
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
        };
        var tap = new TapGestureRecognizer();
        tap.Tapped += OnOutsideTapped;
        _dismiss.GestureRecognizers.Add(tap);

        _scroll = new ScrollView
        {
            Orientation = ScrollOrientation.Vertical,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            BackgroundColor = Colors.Transparent,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
            VerticalScrollBarVisibility = ScrollBarVisibility.Never,
            // A ScrollView defaults to adjusting for the safe area. Placement already
            // keeps the panel inside it, so the content stays at the top of the shell.
            SafeAreaEdges = SafeAreaEdges.None,
        };

        _shell = new ContentView
        {
            BackgroundColor = Colors.Transparent,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start,
            Padding = 0,
            Content = _scroll,
        };

        _overlay = new Grid
        {
            BackgroundColor = Colors.Transparent,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            InputTransparent = false,
            CascadeInputTransparent = false,
            // A Grid defaults to SafeAreaEdges.Container. This overlay covers the page,
            // and the anchor is already in page coordinates, so that padding would drop
            // the panel by the status bar. Placement insets the safe area itself.
            SafeAreaEdges = SafeAreaEdges.None,
        };
        _overlay.Add(_dismiss);
        _overlay.Add(_shell);
    }

    void RequestDismiss()
    {
        if (_phase is Phase.Closed or Phase.Closing)
            return;

        _wantOpen = false;
        BeginClose(raiseDismiss: true);
    }

    void BeginClose(bool raiseDismiss)
    {
        _raiseDismiss = raiseDismiss;
        _phase = Phase.Closing;
        var ticket = ++_ticket;
        Run(AnimateClose(ticket));
    }

    async Task AnimateOpen(int ticket)
    {
        var shell = _shell;
        if (shell == null)
            return;

        try
        {
            var duration = _surface.Duration;
            var easing = Ease();
            await Task.WhenAll(
                shell.FadeToAsync(1, duration, easing),
                shell.ScaleXToAsync(1, duration, easing),
                shell.ScaleYToAsync(1, duration, easing)
            ).ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (ticket == _ticket && _phase == Phase.Opening)
                _phase = Phase.Open;
        });
    }

    async Task AnimateClose(int ticket)
    {
        var shell = _shell;
        try
        {
            if (shell != null)
            {
                var duration = _surface.Duration;
                var easing = Ease();
                await Task.WhenAll(
                    shell.FadeToAsync(0, duration, easing),
                    shell.ScaleXToAsync(OpenScaleX(), duration, easing),
                    shell.ScaleYToAsync(OpenScaleY(), duration, easing)
                ).ConfigureAwait(false);
            }
        }
        catch (ObjectDisposedException)
        {
        }

        MainThread.BeginInvokeOnMainThread(() => FinishClose(ticket));
    }

    void FinishClose(int ticket)
    {
        if (ticket != _ticket)
            return;

        var raise = _raiseDismiss;
        TearDown();
        _phase = Phase.Closed;
        _raiseDismiss = false;
        if (raise)
            _surface.RaiseDismissed();
    }

    void TearDown()
    {
        Unwatch();
        _layer.ReleaseBack();
        _layer.ReleaseEscape();
        if (_scroll != null)
            _scroll.Content = null;

        if (_panel != null)
            _panel.WidthRequest = -1;

        if (_inserted && _overlay != null)
        {
            _layer.Remove(_overlay);
            _inserted = false;
        }
    }

    void Watch()
    {
        Unwatch();
        Element? current = _surface;
        while (current != null)
        {
            if (current is VisualElement visual)
            {
                visual.SizeChanged += OnMoved;
                _sized.Add(visual);
            }

            if (current is ScrollView scroll)
            {
                scroll.Scrolled += OnScrolled;
                _scrolls.Add(scroll);
            }

            if (current is Page)
                break;

            current = current.Parent;
        }
    }

    void Unwatch()
    {
        foreach (var visual in _sized)
            visual.SizeChanged -= OnMoved;

        _sized.Clear();
        foreach (var scroll in _scrolls)
            scroll.Scrolled -= OnScrolled;

        _scrolls.Clear();
    }

    void UnsubscribePanel()
    {
        if (_panel != null)
            _panel.SizeChanged -= OnPanelSized;
    }

    void OnPanelSized(object? sender, EventArgs e)
        => Relayout();

    void OnMoved(object? sender, EventArgs e)
        => Relayout();

    void OnScrolled(object? sender, ScrolledEventArgs e)
        => Relayout();

    void OnOutsideTapped(object? sender, TappedEventArgs e)
        => RequestDismiss();

    void Schedule()
    {
        if (_scheduled || !_wantOpen || _attempts >= MaxAttempts)
            return;

        var dispatcher = _surface.Dispatcher ?? Application.Current?.Dispatcher;
        if (dispatcher == null)
            return;

        _scheduled = true;
        _attempts++;
        dispatcher.Dispatch(() =>
        {
            _scheduled = false;
            Present();
        });
    }

    Page? FindPage()
    {
        Element? current = _surface;
        while (current != null)
        {
            if (current is Page page)
                return page;

            current = current.Parent;
        }

        return null;
    }

    double OpenScaleX()
        => PopupPlacement.AtLeastZero(_surface.OpenScaleX);

    double OpenScaleY()
        => PopupPlacement.AtLeastZero(_surface.OpenScaleY);

    Easing Ease()
        => _surface.Easing ?? Easing.CubicOut;

    static double AnchorX(MenuAlignment alignment)
        => alignment switch
        {
            MenuAlignment.Leading => 0,
            MenuAlignment.Center => 0.5,
            _ => 1,
        };

    static void Run(Task task)
    {
        _ = task.ContinueWith(
            static finished => _ = finished.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
    }

    static double Finite(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
            return 0;

        return value;
    }

    enum Phase
    {
        Closed,
        Opening,
        Open,
        Closing,
    }
}
