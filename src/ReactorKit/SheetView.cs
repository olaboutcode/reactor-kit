using MauiReactor;
using Reactor.Kit.Internals.Sheet;
using Reactor.Kit.SheetTokens;

namespace Reactor.Kit;

/// <summary>
/// One sheet. <c>PageSheet</c> and <c>FormSheet</c> set the style and the defaults.
/// The page owns <see cref="IsOpen"/> and <see cref="SelectedDetent"/>.
/// </summary>
public sealed partial class SheetView : Component
{
    [Prop] private SheetStyle _style;
    [Prop] private bool _isOpen;
    [Prop] private string? _selectedDetent;
    [Prop] private bool _grabber;
    [Prop] private double? _cornerRadius;
    [Prop] private string? _undimmedDetent;
    [Prop] private bool _backdropDismiss;
    [Prop] private bool _dismissDisabled;
    [Prop] private bool _scrollingExpands = true;
    [Prop] private bool _edgeAttachedInCompactHeight = true;
    [Prop] private bool _widthFollowsPreferredSize;
    [Prop] private bool _pageSizing = true;
    [Prop] private double _preferredWidth = 540;
    [Prop] private double _preferredHeight = 620;
    [Prop] private uint _duration = 350;
    [Prop] private Easing _easing = Microsoft.Maui.Easing.CubicOut;
    [Prop] private Action? _onDismiss;
    [Prop] private Action<string>? _onDetentChanged;

    SheetDetent[] _detents = [];
    VisualNode? _content;

    /// <summary>Resting heights, shortest to tallest. An empty list uses the style default.</summary>
    public SheetView Detents(params SheetDetent[] detents)
    {
        _detents = detents ?? [];
        return this;
    }

    /// <summary>The sheet body. One node.</summary>
    public SheetView Content(VisualNode? content)
    {
        _content = content;
        return this;
    }

    /// <inheritdoc />
    public override VisualNode Render()
        => new SheetAnchor()
            .Style(_style)
            .IsOpen(_isOpen)
            .Detents(_detents)
            .SelectedDetent(_selectedDetent)
            .Grabber(_grabber)
            .CornerRadius(_cornerRadius)
            .UndimmedDetent(_undimmedDetent)
            .BackdropDismiss(_backdropDismiss)
            .DismissDisabled(_dismissDisabled)
            .ScrollingExpands(_scrollingExpands)
            .EdgeAttachedInCompactHeight(_edgeAttachedInCompactHeight)
            .WidthFollowsPreferredSize(_widthFollowsPreferredSize)
            .PageSizing(_pageSizing)
            .PreferredWidth(_preferredWidth)
            .PreferredHeight(_preferredHeight)
            .Duration(_duration)
            .Easing(_easing)
            .OnDismiss(_onDismiss)
            .OnDetentChanged(_onDetentChanged)
            .Body(_content);
}
