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
    bool _inPresent;
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

            // A snap still running would write frames between the finger samples.
            Hold();
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

    /// <summary>True while the card is in the window, including while it slides away.</summary>
    public bool IsAttached => _attached;

    /// <summary>True while this sheet is open and not sliding away.</summary>
    public bool CoversStack => _attached && !_closing && _wantOpen;

    /// <summary>True when this sheet is a bottom card. A centered card leaves the sheets behind it alone.</summary>
    public bool StacksAsCard => _layout is { Frame.Centered: false, Frame.FullScreen: false };

    /// <summary>Eases this card when a sheet above it opens or closes.</summary>
    public void Restack()
    {
        if (!_attached || _closing || _dragging || _layout == null || !_layer.PositionsCard)
            return;

        var next = Stamp(_layout.Frame);
        Animate(next, () =>
        {
            _visibleId ??= next.ActiveId;
            _visibleHeight = next.RestHeight;
        });
    }

    /// <summary>Opens the sheet. A present that starts while one is already running is ignored.</summary>
    void Present()
    {
        if (_inPresent || !_wantOpen || _host == null)
            return;

        _inPresent = true;
        try
        {
            PresentCore();
        }
        finally
        {
            _inPresent = false;
        }
    }

    /// <summary>Attaches the layer, measures the page, and eases the card in or across to the new frame.</summary>
    void PresentCore()
    {
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
            if (_host is not View host || !_layer.TryAttach(page, host))
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
        Open(layout);
    }

    /// <summary>Eases the card to the measured frame, then eases the other sheets into the stack.</summary>
    void Open(SheetLayout layout)
    {
        var placed = Stamp(layout.Frame);
        var native = Native(layout);
        if (_shown.Card.Height <= 0 && _shown.Card.Width <= 0)
        {
            _layer.PresentNative(native);
            // Start off screen. Presenting at the open rectangle skips the entrance.
            if (_layer.PositionsCard)
                PresentFrame(Hidden(placed));

            Animate(placed, () => Settle(layout));
        }
        else
        {
            _layer.UpdateNative(native);
            Animate(placed, () => Settle(layout));
        }

        if (_root != null)
            SheetStack.RestackOthers(_root, this);
    }

    /// <summary>Presents again when the container size, insets, or keyboard changed.</summary>
    void OnMetrics()
    {
        // The card move lays the window out. That is not a new measurement, and presenting again aborts the animation.
        if (!_wantOpen || _dragging || _closing || _page == null)
            return;

        if (!_layer.TryMeasure(_page, out var metrics) || metrics.Width <= 0 || metrics.Height <= 0)
            return;

        if (_layout != null && SameMetrics(_metrics, metrics))
            return;

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
        _shown = Stamp(layout.Frame);
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
        PresentFrame(_shown);
    }

    void BeginClose(bool raise)
    {
        if (!_attached || _closing)
            return;

        _closing = true;
        if (!raise)
            _programmaticClose = true;
        else
            _raisedDismiss = true;

        _wantOpen = false;
        if (!_layer.PositionsCard)
        {
            _ticket++;
            _layer.DismissNative();
            return;
        }

        if (_root != null)
            SheetStack.RestackOthers(_root, this);

        Animate(Hidden(_shown), () => Finish(raise));
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

    /// <summary>Drops a running move so the finger owns the card.</summary>
    void Hold()
    {
        _ticket++;
        _anchor.AbortAnimation(AnimationName);
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
            PresentFrame(Stamp(Copy(_layout.Frame, new Rect(card.X, _dragY, card.Width, card.Height), _layout.Frame.DimOpacity)));
            return;
        }

        var largest = _layout.Rests[^1].Height;
        _dragHeight = Math.Clamp(_dragHeight - dy, 0, largest);
        PresentFrame(CardAt(_dragHeight));
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

            Animate(_layout.Frame, () => Settle(_layout));
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
        var changed = !string.Equals(rest.Id, _visibleId, StringComparison.Ordinal);
        _visibleHeight = rest.Height;
        // Tell the page after the card arrives. Telling it now renders, and that present fights this snap.
        Animate(frame, () =>
        {
            if (changed)
                RaiseDetent(rest.Id);
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
        return Stamp(new SheetFrame
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
        });
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

    /// <summary>Eases the card from the frame on screen to the target. A layer that does not move the card applies the target at once.</summary>
    void Animate(SheetFrame to, Action done)
    {
        var from = _shown;
        if (!_layer.PositionsCard)
        {
            PresentFrame(to);
            done();
            return;
        }

        var ticket = ++_ticket;
        if (SameFrame(from, to))
        {
            PresentFrame(to);
            done();
            return;
        }

        _anchor.AbortAnimation(AnimationName);
        var animation = new Animation(t =>
        {
            if (ticket != _ticket)
                return;

            PresentFrame(Lerp(from, to, t));
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

                PresentFrame(to);
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

    /// <summary>True when the container measurements agree within half a point.</summary>
    static bool SameMetrics(SheetMetrics left, SheetMetrics right)
        => Nearly(left.Width, right.Width)
            && Nearly(left.Height, right.Height)
            && Nearly(left.Safe.Left, right.Safe.Left)
            && Nearly(left.Safe.Top, right.Safe.Top)
            && Nearly(left.Safe.Right, right.Safe.Right)
            && Nearly(left.Safe.Bottom, right.Safe.Bottom)
            && Nearly(left.KeyboardOverlap, right.KeyboardOverlap)
            && left.CompactWidth == right.CompactWidth
            && left.CompactHeight == right.CompactHeight
            && NullableNearly(left.FocusedBottom, right.FocusedBottom);

    /// <summary>True when the card rectangles and chrome agree within half a point.</summary>
    static bool SameFrame(SheetFrame left, SheetFrame right)
        => Nearly(left.Card.X, right.Card.X)
            && Nearly(left.Card.Y, right.Card.Y)
            && Nearly(left.Card.Width, right.Card.Width)
            && Nearly(left.Card.Height, right.Card.Height)
            && Nearly(left.DimOpacity, right.DimOpacity)
            && left.DimHits == right.DimHits
            && Nearly(left.CornerRadius, right.CornerRadius)
            && left.Grabber == right.Grabber
            && left.Centered == right.Centered
            && left.FullScreen == right.FullScreen
            && Nearly(left.Stack, right.Stack);

    /// <summary>True when both values are absent, or both are present and nearly equal.</summary>
    static bool NullableNearly(double? left, double? right)
        => left is null ? right is null : right is not null && Nearly(left.Value, right.Value);

    /// <summary>True when the two lengths differ by less than half a point.</summary>
    static bool Nearly(double left, double right)
        => Math.Abs(left - right) < 0.5;

    static SheetFrame Lerp(SheetFrame from, SheetFrame to, double t)
    {
        var card = new Rect(
            from.Card.X + ((to.Card.X - from.Card.X) * t),
            from.Card.Y + ((to.Card.Y - from.Card.Y) * t),
            from.Card.Width + ((to.Card.Width - from.Card.Width) * t),
            from.Card.Height + ((to.Card.Height - from.Card.Height) * t));
        var stack = from.Stack + ((to.Stack - from.Stack) * t);
        return Copy(to, card, from.DimOpacity + ((to.DimOpacity - from.DimOpacity) * t), stack);
    }

    static SheetFrame Copy(SheetFrame source, Rect card, double opacity, double? stack = null, double? grabberAlpha = null)
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
            Stack = stack ?? source.Stack,
            GrabberAlpha = grabberAlpha ?? source.GrabberAlpha,
        };

    /// <summary>Puts the card on screen and sets this grabber from how far the card is covered.</summary>
    void PresentFrame(SheetFrame frame)
    {
        if (!_layer.PositionsCard)
        {
            _shown = frame;
            _layer.Apply(frame);
            return;
        }

        var dressed = Dress(frame);
        _shown = dressed;
        _layer.Apply(dressed);
    }

    /// <summary>Keeps the front grabber solid. A covered grabber fades with the stack.</summary>
    SheetFrame Dress(SheetFrame frame)
    {
        if (!_layer.PositionsCard || _root == null)
            return frame;

        var alpha = frame.Grabber ? HandleAlpha(frame) : 0;
        if (Math.Abs(frame.GrabberAlpha - alpha) < 0.02)
            return frame;

        return Copy(frame, frame.Card, frame.DimOpacity, frame.Stack, alpha);
    }

    /// <summary>Front grabber stays fully visible. A covered grabber fades out as the stack settles and back in as the sheet in front closes.</summary>
    double HandleAlpha(SheetFrame frame)
    {
        if (_root == null || !SheetStack.HasCardAbove(_root, this))
            return 1;

        return 1 - Math.Clamp(frame.Stack, 0, 1);
    }

    /// <summary>
    /// Lowers a card that has sheets behind it so their top edge stays visible.
    /// Pass the measured frame. A frame that was already stamped drops twice.
    /// </summary>
    SheetFrame Stamp(SheetFrame frame)
    {
        if (!_layer.PositionsCard || _root == null)
            return frame;

        var (depth, below, stacked) = SheetStack.Place(_root, this);
        var stack = stacked ? depth : 0d;
        var drop = stacked && !frame.Centered && !frame.FullScreen ? SheetStack.Peek * below : 0;
        var card = frame.Card;
        if (drop > 0)
            card = new Rect(card.X, card.Y + drop, card.Width, Math.Max(0, card.Height - drop));

        var opacity = frame.DimOpacity;
        var hits = frame.DimHits;
        if (stacked && below > 0)
        {
            opacity = 0;
            hits = false;
        }

        if (Nearly(frame.Stack, stack)
            && Nearly(card.X, frame.Card.X)
            && Nearly(card.Y, frame.Card.Y)
            && Nearly(card.Width, frame.Card.Width)
            && Nearly(card.Height, frame.Card.Height)
            && Nearly(opacity, frame.DimOpacity)
            && hits == frame.DimHits)
        {
            return frame;
        }

        return new SheetFrame
        {
            Card = card,
            DimOpacity = opacity,
            DimHits = hits,
            CornerRadius = frame.CornerRadius,
            Centered = frame.Centered,
            FullScreen = frame.FullScreen,
            Grabber = frame.Grabber,
            ActiveId = frame.ActiveId,
            RestHeight = frame.RestHeight,
            Stack = stack,
        };
    }

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
