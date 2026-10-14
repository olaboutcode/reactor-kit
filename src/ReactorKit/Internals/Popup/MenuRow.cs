using MauiReactor;
using MauiReactor.Shapes;
using Reactor.Kit.PopupMenuTokens;

namespace Reactor.Kit.Internals.Popup;

/// <summary>
/// One pop-up row. The checkmark column stays put so titles line up.
/// </summary>
sealed partial class MenuRow : Component<MenuRow.RowState>
{
    [Prop] private PopupMenuItem? _item;
    [Prop] private bool _marked;
    [Prop] private Color _ink = Colors.Black;
    [Prop] private Action? _chosen;

    public sealed class RowState
    {
        public bool Pressed { get; set; }
    }

    public override VisualNode Render()
    {
        var item = _item;
        if (item is null)
            return ContentView();

        var enabled = item.Enabled;
        var row = Grid($"{MenuChrome.MarkColumn},*", "*", Mark(_marked), Body(item))
            .ColumnSpacing(MenuChrome.MarkGap)
            .Padding(MenuChrome.RowPadding, 0)
            .MinimumHeightRequest(MenuChrome.RowHeight);

        var plate = Border(row)
            .StrokeThickness(0)
            .BackgroundColor(State.Pressed && enabled ? MenuChrome.PressedWash() : Colors.Transparent)
            .StrokeShape(new RoundRectangle().CornerRadius(MenuChrome.PressedRadius))
            .Opacity(enabled ? 1 : 0.4);

        if (!enabled)
            return plate;

        return plate
            .OnPointerPressed((PointerEventArgs _) => Press(true))
            .OnPointerReleased((PointerEventArgs _) => Press(false))
            .OnPointerExited((PointerEventArgs _) => Press(false))
            .OnTapped(Pick);
    }

    VisualNode Mark(bool show)
    {
        if (!show)
            return ContentView().WidthRequest(MenuChrome.MarkColumn).HeightRequest(MenuChrome.MarkColumn);

        return new MauiReactor.Shapes.Path()
            .Data(MenuChrome.Checkmark)
            .Stroke(_ink)
            .StrokeThickness(1.75)
            .StrokeLineCap(Microsoft.Maui.Controls.Shapes.PenLineCap.Round)
            .StrokeLineJoin(Microsoft.Maui.Controls.Shapes.PenLineJoin.Round)
            .Aspect(Stretch.Uniform)
            .WidthRequest(MenuChrome.MarkColumn)
            .HeightRequest(MenuChrome.MarkColumn)
            .VCenter();
    }

    MauiReactor.ContentView Body(PopupMenuItem item)
    {
        VisualNode body = item.Content
            ?? Label(item.Title)
                .FontSize(MenuChrome.TitleSize)
                .TextColor(_ink)
                .VCenter();

        return ContentView(body)
            .VCenter()
            .GridColumn(1);
    }

    void Press(bool pressed)
    {
        if (State.Pressed == pressed)
            return;

        SetState(s => s.Pressed = pressed);
    }

    void Pick()
    {
        Press(false);
        _chosen?.Invoke();
    }
}
