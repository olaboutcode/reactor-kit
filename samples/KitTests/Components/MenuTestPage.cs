using MauiReactor.Shapes;

namespace KitTests.Components;

/// <summary>
/// A pop-up menu and a free-form panel.
/// </summary>
sealed class MenuTestPage : Component<MenuTestPage.PageState>
{
    static readonly Color Ink = Color.FromArgb("#1C1C1E");
    static readonly Color Paper = Color.FromArgb("#F7F5F2");

    internal sealed class PageState
    {
        public bool Open { get; set; }

        public string Repeat { get; set; } = "never";
    }

    public override VisualNode Render()
        => ContentPage("Menu",
            Grid("auto,*", "*",
                NavigationBar.Create("Menu"),
                VStack(
                    Label("The button title follows the choice.")
                        .FontSize(17)
                        .TextColor(Ink)
                        .HCenter(),
                    RepeatMenu(),
                    Label("A panel can hold any content.")
                        .FontSize(17)
                        .TextColor(Ink)
                        .HCenter(),
                    Label("Tap outside the panel to close it.")
                        .FontSize(15)
                        .TextColor(Ink)
                        .HCenter(),
                    PopupView(ShowButton())
                        .Content(Panel())
                        .IsOpen(State.Open)
                        .OnDismiss(Close)
                        .HCenter()
                )
                .Spacing(16)
                .Center()
                .GridRow(1)
            )
            .Padding(16)
        )
        .BackgroundColor(Paper);

    PopupMenu RepeatMenu()
        => PopupMenu()
            .Sections(
            [
                [
                    new PopupMenuItem("never", "Never"),
                    new PopupMenuItem("day", "Every Day"),
                    new PopupMenuItem("week", "Every Week"),
                ],
                [
                    new PopupMenuItem("custom", "Custom",
                        HStack(
                            new Ellipse()
                                .Fill(Ink)
                                .Width(8)
                                .Height(8)
                                .VCenter(),
                            Label("Custom")
                                .FontSize(17)
                                .TextColor(Ink)
                                .VCenter()
                        )
                        .Spacing(8)
                        .VCenter()),
                ],
            ])
            .Selected(State.Repeat)
            .OnSelected(id => SetState(s => s.Repeat = id))
            .TextColor(Paper)
            .BackgroundColor(Ink);

    MauiReactor.Button ShowButton()
        => new MauiReactor.Button("Show", Open)
            .TextColor(Paper)
            .BackgroundColor(Ink)
            .FontSize(15)
            .CornerRadius(16)
            .Height(36)
            .Padding(14, 0);

    static GlazeView Panel()
        => GlazeView(
                Label("Anything")
                    .FontSize(17)
                    .TextColor(Ink)
                    .Center()
            )
            .Padding(16)
            .CornerRadius(14)
            .Shadow(
                new MauiReactor.Shadow()
                .Brush(Brush.Black)
                .Offset(0.3, 0.3)
                .Radius(4)
                .Opacity(0.28f)
            );

    void Open()
        => SetState(s => s.Open = true);

    void Close()
        => SetState(s => s.Open = false);
}
