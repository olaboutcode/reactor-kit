namespace KitTests.Components;

/// <summary>
/// The sheet header: a glaze circle with the X, a centered uppercase title, and a matching trailing slot.
/// </summary>
public sealed partial class NavigationBar : Component
{
    static readonly Color Ink = Color.FromArgb("#1C1C1E");

    [Prop] private string? _title = null;
    [Prop] private Color? _textColor = null;
    [Prop] private Action? _onClose = null;
    [Prop] private VisualNode? _trailing = null;

    public override VisualNode Render()
        => Grid("*", "auto,*,auto",
            CloseButton()
                .GridColumn(0)
                .VCenter(),
            Label(_title ?? "Page Title")
                .FontSize(17)
                .TextTransform(TextTransform.Uppercase)
                .TextColor(_textColor ?? Ink)
                .Center()
                .GridColumn(1),
            End()
        ).Padding(0,8);

    GlazeView CloseButton()
        => GlazeView(
                Image(HeroIcons.XMark)
                    .Aspect(Aspect.AspectFill)
                    .Height(24)
                    .Width(24)
                    .Center()
            )
            .OnTapped(Close)
            .TintColor(Colors.WhiteSmoke)
            .Width(44)
            .Height(44)
            .CornerRadius(22)
            .Shadow(ShadowElevation.For(1));

    VisualNode End()
    {
        if (_trailing is { } trailing)
            return trailing.GridColumn(2);

        return BoxView()
            .Width(44)
            .Height(44)
            .Color(Colors.Transparent)
            .GridColumn(2);
    }

    public static NavigationBar Create(string title)
        => new NavigationBar().Title(title);

    void Close()
    {
        if (_onClose is { } onClose)
        {
            onClose();
            return;
        }

        _ = Navigation?.PopModalAsync(animated: false);
    }
}
