using MauiReactor;
using Reactor.Kit.PopupMenuTokens;
using static Reactor.Kit.Extensions;

namespace Reactor.Kit;

/// <summary>
/// A pop-up menu on a <see cref="PopupView"/>. A row is a title or any view.
/// The page owns <see cref="IsOpen(bool)"/>.
/// </summary>
public sealed partial class PopupMenu : Component
{
    [Prop] private VisualNode? _anchor;
    [Prop] private bool _isOpen;
    [Prop] private Action? _onDismiss;
    [Prop] private IEnumerable<PopupMenuItem>? _items;
    [Prop] private Color _titleColor = Colors.Black;
    [Prop] private Color _ruleColor = RuleInk;
    [Prop] private bool _disableAnchorWhenOpen;

    static readonly Color RuleInk = Color.FromRgba(60, 60, 67, 74);

    Func<GlazeView, GlazeView>? _panel;
    Func<VStack, VStack>? _stack;

    /// <summary>
    /// The rows. Chain <c>Item</c> and <c>Rule</c> on the list.
    /// </summary>
    public PopupMenu Items(Func<PopupMenuItems, PopupMenuItems> build)
    {
        ArgumentNullException.ThrowIfNull(build);
        var items = new PopupMenuItems();
        _items = (build(items) ?? items).Rows;
        return this;
    }

    /// <summary>
    /// Adjusts the glaze panel. The callback receives the panel after its defaults, already holding the row stack.
    /// </summary>
    public PopupMenu Panel(Func<GlazeView, GlazeView> adjust)
    {
        ArgumentNullException.ThrowIfNull(adjust);
        _panel = adjust;
        return this;
    }

    /// <summary>
    /// Adjusts the vertical stack that holds the rows. The callback receives that stack after its default spacing.
    /// </summary>
    public PopupMenu Stack(Func<VStack, VStack> adjust)
    {
        ArgumentNullException.ThrowIfNull(adjust);
        _stack = adjust;
        return this;
    }

    public override VisualNode Render()
    {
        var anchor = PreparedAnchor();
        var popup = anchor is null ? PopupView() : PopupView(anchor);
        return popup
            .Content(Panel())
            .IsOpen(_isOpen)
            .OnDismiss(_onDismiss)
            .HorizontalOptions(LayoutOptions.Center);
    }

    VisualNode? PreparedAnchor()
    {
        if (_anchor is null || !_disableAnchorWhenOpen || !_isOpen)
            return _anchor;

        return ContentView(_anchor)
            .IsEnabled(false)
            .InputTransparent(true)
            .CascadeInputTransparent(true)
            .Opacity(0.4);
    }

    GlazeView Panel()
    {
        var stack = VStack(Rows()).Spacing(8);
        if (_stack is not null)
            stack = _stack(stack) ?? stack;

        var panel = GlazeView(stack)
            .Padding(8)
            .CornerRadius(14)
            .Shadow(
                new MauiReactor.Shadow()
                    .Brush(Brush.Black)
                    .Offset(0.3, 0.3)
                    .Radius(4)
                    .Opacity(0.28f));

        return _panel is null ? panel : _panel(panel) ?? panel;
    }

    VisualNode[] Rows()
    {
        if (_items is null)
            return [];

        var rows = new List<VisualNode>();
        foreach (var item in _items)
        {
            if (item is null)
                continue;

            rows.Add(Row(item));
        }

        return [.. rows];
    }

    VisualNode Row(PopupMenuItem item)
    {
        if (item.IsRule)
            return Rule();

        if (item.Content is { } content)
            return Custom(content, item.Tap);

        return Title(item.Text ?? string.Empty, item.TextTap);
    }

    MauiReactor.Button Title(string text, Action<string>? onTap)
        => new MauiReactor.Button(text)
            .BackgroundColor(Colors.Transparent)
            .CornerRadius(12)
            .HeightRequest(44)
            .TextColor(_titleColor)
            .OnClicked(() =>
            {
                _onDismiss?.Invoke();
                onTap?.Invoke(text);
            });

    MauiReactor.Border Custom(VisualNode view, Action? onTap)
        => Border(view)
            .BackgroundColor(Colors.Transparent)
            .MinimumHeightRequest(44)
            .Stroke(Colors.Transparent)
            .StrokeCornerRadius(12)
            .Padding(4)
            .Margin(0)
            .OnTapped(() =>
            {
                _onDismiss?.Invoke();
                onTap?.Invoke();
            });

    MauiReactor.BoxView Rule()
        => new MauiReactor.BoxView()
            .HeightRequest(1)
            .Color(_ruleColor)
            .Margin(16, 1);
}
