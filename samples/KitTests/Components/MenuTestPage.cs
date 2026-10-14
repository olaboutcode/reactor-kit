namespace KitTests.Components;

/// <summary>
/// A free-form panel anchored to a button. The menus come later.
/// </summary>
sealed class MenuTestPage : Component<MenuTestPage.PageState>
{
    static readonly Color Ink = Color.FromArgb("#1C1C1E");
    static readonly Color Paper = Color.FromArgb("#F7F5F2");

    internal sealed class PageState
    {
        public bool Open { get; set; }
    }

    public override VisualNode Render()
        => ContentPage("Menu",
            Grid("auto,*", "*",
                NavigationBar.Create("Menu"),
                VStack(
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
