using Reactor.Kit.SheetTokens;

namespace Reactor.Kit.Internals.Sheet;

/// <summary>
/// Presents one sheet, snaps drags, and raises dismiss once.
/// The page owns <see cref="SheetProps.IsOpen"/> and the selected detent. This type does not call SetState.
/// </summary>
sealed class SheetSession
{
    const int MaxAttempts = 8;
    const string AnimationName = "ReactorKit.Sheet";

    readonly VisualElement _anchor;
    readonly ISheetLayer _layer = SheetLayers.Create();

    SheetProps _props = new();
    View? _host;
    Page? _page;
    object? _root;
    SheetMetrics _metrics = new();
    SheetLayout? _layout;
    SheetFrame _shown = new();
    string? _visibleId;
    double _visibleHeight;
    string? _pageDetent;
    string? _programmaticId;
    int _ticket;
    int _attempts;
    bool _scheduled;
    bool _attached;
    bool _wantOpen;
    bool _raisedDismiss;
    bool _programmaticClose;
    bool _closing;
    bool _dragging;
    bool _arming;
    bool _armedInGrabber;
    double _dragHeight;
    double _dragY;
    double _armY;

    public SheetSession(VisualElement anchor)
    {
        _anchor = anchor;
    }

    public void Apply(SheetProps props, View host)
    {
        var detentChanged = !string.Equals(_pageDetent, props.SelectedDetent, StringComparison.Ordinal);
        _pageDetent = props.SelectedDetent;
        if (_wantOpen && detentChanged)
            _programmaticId = props.SelectedDetent;

        _props = props;
        _host = host;
        if (props.IsOpen)
        {
            _wantOpen = true;
            _attempts = 0;
            Present();
            return;
        }

        _wantOpen = false;
        if (_attached)
            BeginClose(raise: false);
    }

    public void CloseWithoutDismiss()
    {
        _wantOpen = false;
        if (_attached)
            BeginClose(raise: false);
    }

    public void Pointer(SheetPointer pointer)
    {
        if (_layout == null || _root == null || !SheetStack.IsTop(_root, this))
            return;

        if (pointer.Down)
        {
            _arming = true;
            _dragging = false;
            _armedInGrabber = pointer.InGrabber;
            _armY = pointer.Y;
            _dragHeight = _shown.Card.Height;
            _dragY = _shown.Card.Y;
            return;
        }

        if (_arming && !pointer.Up)
        {
            var dy = pointer.Y - _armY;
            if (Math.Abs(dy) < 2)
                return;

            _arming = false;
            _dragging = Capture(pointer, dy);
            if (!_dragging)
                return;
        }

        if (!_dragging)
            return;

        if (!pointer.Up)
        {
            Track(pointer.Y - _armY);
            _armY = pointer.Y;
            return;
        }

        _dragging = false;
        Release(pointer.VelocityY);
    }

    public void NativeDetent(string? id)
    {
        if (id is null)
            return;

        if (_programmaticId != null && string.Equals(_programmaticId, id, StringComparison.Ordinal))
        {
            _programmaticId = null;
            _visibleId = id;
            return;
        }

        if (string.Equals(_visibleId, id, StringComparison.Ordinal))
            return;

        _visibleId = id;
        _props.OnDetentChanged?.Invoke(id);
    }

    public void NativeDismissed()
    {
        if (!_attached)
            return;

        Finish(raise: !_programmaticClose && !_raisedDismiss);
    }

    public void RequestDismiss()
    {
        if (_props.DismissDisabled || !_wantOpen)
            return;

        BeginClose(raise: true);
    }

    void Present()
    {
        if (!_wantOpen || _host == null)
            return;

        if (_closing)
        {
            _closing = false;
            _ticket++;
        }

        var page = FindPage();
        if (page?.Handler?.MauiContext == null)
        {
            Schedule();
            return;
        }

        if (!_attached)
        {
            if (!_layer.TryAttach(page, _host))
            {
                Schedule();
                return;
            }

            _page = page;
            _root = page.Window ?? (object)page;
            _attached = true;
            _raisedDismiss = false;
            _programmaticClose = false;
            SheetStack.Push(_root, this);
            _layer.Watch(OnMetrics, RequestDismiss, RequestDismiss);
        }

        if (!_layer.TryMeasure(page, out var metrics) || metrics.Width <= 0 || metrics.Height <= 0)
        {
            Schedule();
            return;
        }

        _attempts = 0;
        _metrics = metrics;
        var layout = Resolve(metrics);
        _layout = layout;
        var native = Native(layout);
        if (_shown.Card.Height <= 0 && _shown.Card.Width <= 0)
        {
            _layer.PresentNative(native);
            var hidden = Hidden(layout.Frame);
            _shown = hidden;
            Animate(hidden, layout.Frame, _ticket, () => Settle(layout));
            return;
        }

        _layer.UpdateNative(native);
        Animate(_shown, layout.Frame, _ticket, () => Settle(layout));
    }

    void OnMetrics()
    {
        if (_wantOpen)
            Present();
    }

    SheetLayout Resolve(SheetMetrics metrics)
    {
        var first = SheetGeometry.Layout(_props, metrics);
        if (_visibleId is null || Contains(_props.Detents, _visibleId) || first.Rests.Count == 0)
            return first;

        var nearest = Nearest(first.Rests, _visibleHeight);
        if (nearest is null)
            return first;

        var forced = SheetGeometry.Layout(_props, metrics, nearest.Id);
        if (forced.Frame.ActiveId != null && !string.Equals(forced.Frame.ActiveId, _visibleId, StringComparison.Ordinal))
            RaiseDetent(forced.Frame.ActiveId);

        return forced;
    }

    void Settle(SheetLayout layout)
    {
        _shown = layout.Frame;
        var id = layout.Frame.ActiveId;
        if (layout.RaiseInactive && id != null && !string.Equals(id, _props.SelectedDetent, StringComparison.Ordinal))
        {
            if (_programmaticId == null || !string.Equals(_programmaticId, id, StringComparison.Ordinal))
                RaiseDetent(id);
        }

        if (_programmaticId != null && string.Equals(_programmaticId, id, StringComparison.Ordinal))
            _programmaticId = null;

        _visibleId = id;
        _visibleHeight = layout.Frame.RestHeight;
        _layer.Apply(layout.Frame);
    }

    void BeginClose(bool raise)
    {
        if (!_attached || _closing)
            return;

        _closing = true;
        var ticket = ++_ticket;
        if (!raise)
            _programmaticClose = true;
        else
            _raisedDismiss = true;

        _wantOpen = false;
        if (!_layer.PositionsCard)
        {
            _layer.DismissNative();
            return;
        }

        var hidden = Hidden(_shown);
        Animate(_shown, hidden, ticket, () =>
        {
            if (ticket != _ticket)
                return;

            Finish(raise);
        });
    }

    void Finish(bool raise)
    {
        _layer.Watch(null, null, null);
        _layer.Detach();
        if (_root != null)
            SheetStack.Remove(_root, this);

        _attached = false;
        _closing = false;
        _layout = null;
        _shown = new SheetFrame();
        _visibleId = null;
        _root = null;
        _page = null;
        _dragging = false;
        if (raise)
            _props.OnDismiss?.Invoke();

        _programmaticClose = false;
        if (!_wantOpen)
            return;

        // The dismiss completion can land while a second open is already requested.
        var dispatcher = _anchor.Dispatcher ?? Application.Current?.Dispatcher;
        dispatcher?.Dispatch(() =>
        {
            if (_wantOpen)
                Present();
        });
    }

    void RaiseDetent(string id)
    {
        _visibleId = id;
        _props.OnDetentChanged?.Invoke(id);
    }

    bool Capture(SheetPointer pointer, double dy)
    {
        if (_armedInGrabber)
            return true;

        if (!_props.ScrollingExpands || _layout == null)
            return false;

        if (_layout.Frame.Centered)
            return true;

        var largest = _layout.Rests.Count == 0 ? _dragHeight : _layout.Rests[^1].Height;
        var growing = dy < 0;
        if (growing)
            return _dragHeight < largest - 0.5;

        if (!pointer.HasScroll || pointer.ScrollAtTop)
            return true;

        return false;
    }

    void Track(double dy)
    {
        if (_layout == null)
            return;

        if (_layout.Frame.Centered)
        {
            _dragY += dy;
            var card = _layout.Frame.Card;
            _shown = Copy(_layout.Frame, new Rect(card.X, _dragY, card.Width, card.Height), _layout.Frame.DimOpacity);
            _layer.Apply(_shown);
            return;
        }

        var largest = _layout.Rests[^1].Height;
        _dragHeight = Math.Clamp(_dragHeight - dy, 0, largest);
        _shown = CardAt(_dragHeight);
        _layer.Apply(_shown);
    }

    void Release(double velocityY)
    {
        if (_layout == null)
            return;

        if (_layout.Frame.Centered)
        {
            var travel = _dragY - _layout.Frame.Card.Y;
            var flung = velocityY > 1000 && travel > 0;
            if (!_props.DismissDisabled && (travel > _layout.Frame.Card.Height / 2 || flung))
            {
                BeginClose(raise: true);
                return;
            }

            Animate(_shown, _layout.Frame, _ticket, () => Settle(_layout));
            return;
        }

        var id = SheetGeometry.Snap(_layout.Rests, _dragHeight, velocityY, _props.DismissDisabled, out var dismiss);
        if (dismiss)
        {
            BeginClose(raise: true);
            return;
        }

        var rest = FindRest(_layout.Rests, id) ?? _layout.Rests[^1];
        var frame = CardAt(rest.Height);
        if (!string.Equals(rest.Id, _visibleId, StringComparison.Ordinal))
            RaiseDetent(rest.Id);

        _visibleHeight = rest.Height;
        Animate(_shown, frame, _ticket, () =>
        {
            _shown = frame;
            _layer.Apply(frame);
        });
    }

    SheetFrame CardAt(double height)
    {
        if (_layout == null)
            return _shown;

        var card = _layout.Frame.Card;
        var top = _layout.Frame.FullScreen ? 0 : Math.Max(0, _metrics.Height - height);
        var rect = new Rect(card.X, top, card.Width, _layout.Frame.FullScreen ? card.Height : height);
        var (opacity, hits) = DimFor(height);
        return new SheetFrame
        {
            Card = rect,
            DimOpacity = opacity,
            DimHits = hits,
            CornerRadius = _layout.Frame.CornerRadius,
            Centered = false,
            FullScreen = _layout.Frame.FullScreen,
            Grabber = _layout.Frame.Grabber,
            ActiveId = _layout.Frame.ActiveId,
            RestHeight = height,
        };
    }

    (double Opacity, bool Hits) DimFor(double height)
    {
        if (_props.UndimmedDetent is null || _layout == null)
            return (SheetGeometry.DimOpacity, true);

        double? gate = null;
        foreach (var rest in _layout.Rests)
        {
            if (string.Equals(rest.Id, _props.UndimmedDetent, StringComparison.Ordinal))
                gate = rest.Height;
        }

        if (gate is not double undimmed || height > undimmed + 0.5)
            return (SheetGeometry.DimOpacity, true);

        return (0, false);
    }

    SheetFrame Hidden(SheetFrame shown)
    {
        var card = shown.Centered
            ? new Rect(shown.Card.X, shown.Card.Y + 24, shown.Card.Width, shown.Card.Height)
            : new Rect(shown.Card.X, _metrics.Height, shown.Card.Width, shown.Card.Height);
        return Copy(shown, card, 0);
    }

    void Animate(SheetFrame from, SheetFrame to, int ticket, Action done)
    {
        if (!_layer.PositionsCard)
        {
            _shown = to;
            _layer.Apply(to);
            done();
            return;
        }

        _anchor.AbortAnimation(AnimationName);
        var animation = new Animation(t =>
        {
            if (ticket != _ticket)
                return;

            _layer.Apply(Lerp(from, to, t));
        });
        var length = _props.Duration == 0 ? 1u : _props.Duration;
        animation.Commit(
            _anchor,
            AnimationName,
            16,
            length,
            _props.Easing,
            (_, _) =>
            {
                if (ticket != _ticket)
                    return;

                _shown = to;
                _layer.Apply(to);
                done();
            });
    }

    SheetNative Native(SheetLayout layout)
        => new()
        {
            Props = _props,
            Layout = layout,
            Page = _page ?? throw new InvalidOperationException("The sheet has no page."),
            Host = _host ?? throw new InvalidOperationException("The sheet has no content."),
            DetentSettled = NativeDetent,
            Dismissed = NativeDismissed,
            Pointer = Pointer,
        };

    void Schedule()
    {
        if (_scheduled || !_wantOpen || _attempts >= MaxAttempts)
            return;

        var dispatcher = _anchor.Dispatcher ?? Application.Current?.Dispatcher;
        if (dispatcher == null)
            return;

        _scheduled = true;
        _attempts++;
        dispatcher.Dispatch(() =>
        {
            _scheduled = false;
            if (_wantOpen)
                Present();
        });
    }

    Page? FindPage()
    {
        Element? current = _anchor;
        while (current != null)
        {
            if (current is Page page)
                return page;

            current = current.Parent;
        }

        return null;
    }

    static SheetFrame Lerp(SheetFrame from, SheetFrame to, double t)
    {
        var card = new Rect(
            from.Card.X + ((to.Card.X - from.Card.X) * t),
            from.Card.Y + ((to.Card.Y - from.Card.Y) * t),
            from.Card.Width + ((to.Card.Width - from.Card.Width) * t),
            from.Card.Height + ((to.Card.Height - from.Card.Height) * t));
        return Copy(to, card, from.DimOpacity + ((to.DimOpacity - from.DimOpacity) * t));
    }

    static SheetFrame Copy(SheetFrame source, Rect card, double opacity)
        => new()
        {
            Card = card,
            DimOpacity = opacity,
            DimHits = source.DimHits && opacity > 0.05,
            CornerRadius = source.CornerRadius,
            Centered = source.Centered,
            FullScreen = source.FullScreen,
            Grabber = source.Grabber,
            ActiveId = source.ActiveId,
            RestHeight = source.RestHeight,
        };

    static bool Contains(IReadOnlyList<SheetDetent> detents, string id)
    {
        foreach (var detent in detents)
        {
            if (detent != null && string.Equals(detent.Id, id, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    static SheetRest? Nearest(IReadOnlyList<SheetRest> rests, double height)
    {
        SheetRest? best = null;
        var bestDiff = double.MaxValue;
        foreach (var rest in rests)
        {
            var diff = Math.Abs(rest.Height - height);
            if (diff < bestDiff - 0.5 || (Math.Abs(diff - bestDiff) <= 0.5 && (best == null || rest.Height > best.Height)))
            {
                best = rest;
                bestDiff = diff;
            }
        }

        return best;
    }

    static SheetRest? FindRest(IReadOnlyList<SheetRest> rests, string? id)
    {
        if (id is null)
            return null;

        foreach (var rest in rests)
        {
            if (string.Equals(rest.Id, id, StringComparison.Ordinal))
                return rest;
        }

        return null;
    }
}
