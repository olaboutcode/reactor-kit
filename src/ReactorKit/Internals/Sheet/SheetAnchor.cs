using MauiReactor;
using Reactor.Kit.SheetTokens;

namespace Reactor.Kit.Internals.Sheet;

/// <summary>
/// A zero-size host in the page. The sheet body is a logical child of this anchor, so it stays out of the page layout. <see cref="SheetSession"/> presents it.
/// </summary>
sealed class SheetAnchor : MauiReactor.ContentView
{
    readonly SheetPanelHost _host = new();
    SheetSession? _session;

    SheetStyle _style;
    bool _isOpen;
    SheetDetent[] _detents = [];
    string? _selectedDetent;
    bool _grabber;
    double? _cornerRadius;
    string? _undimmedDetent;
    bool _backdropDismiss;
    bool _dismissDisabled;
    bool _scrollingExpands = true;
    bool _edgeAttached = true;
    bool _widthFollows;
    bool _pageSizing = true;
    double _preferredWidth = 540;
    double _preferredHeight = 620;
    uint _duration = 350;
    Easing _easing = Microsoft.Maui.Easing.CubicOut;
    Action? _onDismiss;
    Action<string>? _onDetentChanged;

    public SheetAnchor()
    {
        this.WidthRequest(0);
        this.HeightRequest(0);
        this.MinimumWidthRequest(0);
        this.MinimumHeightRequest(0);
        this.InputTransparent(true);
        // The body is a logical child. Cascading would make the whole sheet ignore taps.
        this.CascadeInputTransparent(false);
        this.BackgroundColor(Colors.Transparent);
    }

    public SheetAnchor Style(SheetStyle style)
    {
        _style = style;
        return this;
    }

    public SheetAnchor IsOpen(bool open)
    {
        _isOpen = open;
        return this;
    }

    public SheetAnchor Detents(IReadOnlyList<SheetDetent> detents)
    {
        _detents = detents as SheetDetent[] ?? [.. detents];
        return this;
    }

    public SheetAnchor SelectedDetent(string? id)
    {
        _selectedDetent = id;
        return this;
    }

    public SheetAnchor Grabber(bool grabber)
    {
        _grabber = grabber;
        return this;
    }

    public SheetAnchor CornerRadius(double? radius)
    {
        _cornerRadius = radius;
        return this;
    }

    public SheetAnchor UndimmedDetent(string? id)
    {
        _undimmedDetent = id;
        return this;
    }

    public SheetAnchor BackdropDismiss(bool dismiss)
    {
        _backdropDismiss = dismiss;
        return this;
    }

    public SheetAnchor DismissDisabled(bool disabled)
    {
        _dismissDisabled = disabled;
        return this;
    }

    public SheetAnchor ScrollingExpands(bool expands)
    {
        _scrollingExpands = expands;
        return this;
    }

    public SheetAnchor EdgeAttachedInCompactHeight(bool attached)
    {
        _edgeAttached = attached;
        return this;
    }

    public SheetAnchor WidthFollowsPreferredSize(bool follows)
    {
        _widthFollows = follows;
        return this;
    }

    public SheetAnchor PageSizing(bool sizing)
    {
        _pageSizing = sizing;
        return this;
    }

    public SheetAnchor PreferredWidth(double width)
    {
        _preferredWidth = width;
        return this;
    }

    public SheetAnchor PreferredHeight(double height)
    {
        _preferredHeight = height;
        return this;
    }

    public SheetAnchor Duration(uint duration)
    {
        _duration = duration;
        return this;
    }

    public SheetAnchor Easing(Easing easing)
    {
        _easing = easing;
        return this;
    }

    public SheetAnchor OnDismiss(Action? callback)
    {
        _onDismiss = callback;
        return this;
    }

    public SheetAnchor OnDetentChanged(Action<string>? callback)
    {
        _onDetentChanged = callback;
        return this;
    }

    public SheetAnchor Body(VisualNode? body)
    {
        _host.Root = body;
        return this;
    }

    protected override void OnMigrating(VisualNode newNode)
    {
        if (newNode is SheetAnchor next)
        {
            next._session = _session;
            _session = null;
        }

        base.OnMigrating(newNode);
    }

    protected override void OnMount()
    {
        base.OnMount();
        if (NativeControl is Microsoft.Maui.Controls.ContentView view)
        {
            view.WidthRequest = 0;
            view.HeightRequest = 0;
            view.MinimumWidthRequest = 0;
            view.MinimumHeightRequest = 0;
            view.InputTransparent = true;
            view.CascadeInputTransparent = false;
            view.BackgroundColor = Colors.Transparent;
        }

        _session ??= new SheetSession(NativeControl ?? throw new InvalidOperationException("The sheet anchor is not mounted."));
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();
        Push();
    }

    void Push()
    {
        if (_session == null)
            return;

        if (_host.Element is not View host)
        {
            NativeControl?.Dispatcher.Dispatch(Push);
            return;
        }

        _session.Apply(Props(), host);
    }

    protected override void OnUnmount()
    {
        _session?.CloseWithoutDismiss();
        _session = null;
        base.OnUnmount();
    }

    protected override void OnAddChild(VisualNode widget, BindableObject childControl)
    {
        // The body is not the anchor's content, so the page layout does not measure it.
        // It still needs a parent. A popup walks Parent to find the page, and stops when this body has none.
        if (widget is SheetPanelHost && childControl is Element element && NativeControl != null)
        {
            if (!ReferenceEquals(element.Parent, NativeControl))
                NativeControl.AddLogicalChild(element);

            return;
        }

        base.OnAddChild(widget, childControl);
    }

    protected override void OnRemoveChild(VisualNode widget, BindableObject childControl)
    {
        if (widget is SheetPanelHost && childControl is Element element && NativeControl != null)
        {
            NativeControl.RemoveLogicalChild(element);
            return;
        }

        base.OnRemoveChild(widget, childControl);
    }

    protected override IEnumerable<VisualNode> RenderChildren()
    {
        yield return _host;
    }

    SheetProps Props()
        => new()
        {
            Style = _style,
            IsOpen = _isOpen,
            Detents = _detents,
            SelectedDetent = _selectedDetent,
            Grabber = _grabber,
            CornerRadius = _cornerRadius,
            UndimmedDetent = _undimmedDetent,
            BackdropDismiss = _backdropDismiss,
            DismissDisabled = _dismissDisabled,
            ScrollingExpands = _scrollingExpands,
            EdgeAttachedInCompactHeight = _edgeAttached,
            WidthFollowsPreferredSize = _widthFollows,
            PageSizing = _pageSizing,
            PreferredWidth = _preferredWidth,
            PreferredHeight = _preferredHeight,
            Duration = _duration,
            Easing = _easing,
            OnDismiss = _onDismiss,
            OnDetentChanged = _onDetentChanged,
        };
}
