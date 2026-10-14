using MauiReactor;
using Reactor.Kit.Internals.Popup;
using Reactor.Kit.PopupMenuTokens;
using static Reactor.Kit.Extensions;

namespace Reactor.Kit;

/// <summary>
/// A pop-up button. The button title is the selected item. The list draws one checkmark.
/// The open flag lives on this component, so a parent render leaves the menu open.
/// </summary>
public sealed partial class PopupMenu : Component<PopupMenu.MenuState>
{
    [Prop] private IEnumerable<IEnumerable<PopupMenuItem>>? _sections;
    [Prop] private string? _selected;
    [Prop] private Action<string>? _onSelected;
    [Prop] private string _placeholder = MenuChrome.Placeholder;
    [Prop] private Color? _textColor;
    [Prop] private Color? _backgroundColor;

    public sealed class MenuState
    {
        public bool Open { get; set; }
    }

    public override VisualNode Render()
    {
        var sections = Rows();
        return PopupView(Anchor(Title(sections)))
            .Content(Panel(sections))
            .IsOpen(State.Open)
            .OnDismiss(Close)
            .HorizontalOptions(LayoutOptions.Center)
            .VerticalOptions(LayoutOptions.Center);
    }

    List<List<PopupMenuItem>> Rows()
    {
        var sections = new List<List<PopupMenuItem>>();
        if (_sections is null)
            return sections;

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var section in _sections)
        {
            if (section is null)
                continue;

            var items = new List<PopupMenuItem>();
            foreach (var item in section)
            {
                if (item is null)
                    throw new InvalidOperationException("A popup menu section contains a missing item.");

                if (!seen.Add(item.Id))
                    throw new InvalidOperationException($"The popup menu item id '{item.Id}' is used more than once.");

                items.Add(item);
            }

            if (items.Count > 0)
                sections.Add(items);
        }

        return sections;
    }

    string Title(List<List<PopupMenuItem>> sections)
    {
        if (_selected is null)
            return _placeholder;

        foreach (var section in sections)
        {
            foreach (var item in section)
            {
                if (string.Equals(item.Id, _selected, StringComparison.Ordinal))
                    return item.Title;
            }
        }

        return _placeholder;
    }

    MauiReactor.Button Anchor(string title)
        => new MauiReactor.Button(title, Open)
            .TextColor(_textColor ?? Colors.Black)
            .BackgroundColor(_backgroundColor ?? Colors.Transparent)
            .FontSize(MenuChrome.TitleSize)
            .CornerRadius((int)MenuChrome.ButtonRadius)
            .HeightRequest(MenuChrome.ButtonHeight)
            .Padding(MenuChrome.ButtonPadding, 0)
            .HorizontalOptions(LayoutOptions.Center);

    GlazeView Panel(List<List<PopupMenuItem>> sections)
    {
        var stack = new List<VisualNode>();
        for (var index = 0; index < sections.Count; index++)
        {
            if (index > 0)
                stack.Add(Rule());

            foreach (var item in sections[index])
            {
                stack.Add(new MenuRow()
                    .Item(item)
                    .Marked(string.Equals(item.Id, _selected, StringComparison.Ordinal))
                    .Chosen(() => Choose(item)));
            }
        }

        return GlazeView(VStack(stack.ToArray()))
            .Padding(MenuChrome.PanelPadding)
            .CornerRadius(MenuChrome.PanelRadius)
            .Shadow(
                new MauiReactor.Shadow()
                    .Brush(Brush.Black)
                    .Offset(0, 8)
                    .Radius(24)
                    .Opacity(0.18f));
    }

    static MauiReactor.BoxView Rule()
        => new MauiReactor.BoxView()
            .HeightRequest(MenuChrome.Separator)
            .Color(MenuChrome.Hairline())
            .Margin(MenuChrome.RowPadding, 4);

    void Open()
        => SetState(s => s.Open = true);

    void Close()
        => SetState(s => s.Open = false);

    void Choose(PopupMenuItem item)
    {
        if (!item.Enabled)
            return;

        _onSelected?.Invoke(item.Id);
        Close();
    }
}
