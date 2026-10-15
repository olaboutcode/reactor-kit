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

        public bool OpenCustomMenu { get; set; }
    }

    public override VisualNode Render()
        => ContentPage("Menu",
            Grid("auto,*", "*",
                NavigationBar.Create("Menu"),
                VStack(
                    Label("Tap outside the panel to close the menu!"),

                    Label("PopupView: A panel can hold any content.")
                        .TextColor(Ink)
                        .HCenter()
                        .FontSize(15)
                        .TextColor(Ink)
                        .HCenter(),
                    PopupView(ShowButton())
                        .Content(Panel())
                        .IsOpen(State.Open)
                        .OnDismiss(Close)
                        .HCenter(),

                    Label("Popup Menu.")
                        .FontSize(15)
                        .TextColor(Ink)
                        .HCenter(),
                    CustomPopupMenu()
                )
                .Spacing(16)
                .Center()
                .GridRow(1)
            )
            .Padding(16)
        )
        .BackgroundColor(Paper)
        .HasNavigationBar(hasNavigationBar: false);

    PopupMenu CustomPopupMenu() 
        => PopupMenu()
            .Anchor(ShowCustomMenuButton())
            .Items(menu => menu
                .Item("Plain text")
                .Rule()
                .Item(IconRow())
                .Rule()
                .Item(ProfileRow())
                .Rule()
                .Item(CustomRow()))
            .IsOpen(State.OpenCustomMenu)
            .Panel(panel => panel
                .Padding(8)
                .Shadow(ShadowElevation.For(2)))
            .Stack(stack => stack.Spacing(4))
            .Popup(popup => popup
                .OpenScaleX(0.85)
                .OpenScaleY(0.5))
            .OnDismiss(CloseCustomMenu)
            .TitleColor(Ink)
            .DisableAnchorWhenOpen();

    static HStack IconRow()
        => HStack(
                new MauiReactor.Shapes.Path()
                    .Data("M6,3 H14 V17 L10,14 L6,17 Z")
                    .Fill(Ink)
                    .Width(16)
                    .Height(18)
                    .VCenter(),
                Label("With an icon")
                    .FontSize(17)
                    .TextColor(Ink)
                    .VCenter()
            )
            .Spacing(10)
            .Padding(16, 0)
            .VCenter();

    static HStack ProfileRow()
        => HStack(
                Grid(
                    new Ellipse()
                        .Fill(Ink)
                        .Width(36)
                        .Height(36),
                    Label("A")
                        .FontSize(14)
                        .TextColor(Paper)
                        .Center()
                )
                .Width(32)
                .Height(32)
                .VCenter(),
                VStack(
                    Label("Ada Lovelace")
                        .FontSize(14)
                        .TextColor(Ink),
                    Label("Mathematician")
                        .FontSize(12)
                        .TextColor(Color.FromArgb("#6B6B6F"))
                )
                .Spacing(1)
                .VCenter()
            )
            .Spacing(10)
            .Padding(16, 8)
            .VCenter();

    static VStack CustomRow()
        => VStack(
                HStack(
                    Swatch(Ink),
                    Swatch(Color.FromArgb("#C4A484")),
                    Swatch(Paper)
                )
                .Spacing(6),
                Label("A row can be anything.")
                    .FontSize(13)
                    .TextColor(Ink)
            )
            .Spacing(8)
            .Padding(16, 8);

    static Ellipse Swatch(Color color)
        => new Ellipse()
            .Fill(color)
            .Stroke(Ink)
            .StrokeThickness(1)
            .Width(14)
            .Height(14);

    MauiReactor.Button ShowButton()
        => new MauiReactor.Button("Show Popup View", Open)
            .TextColor(Paper)
            .BackgroundColor(Ink)
            .FontSize(15)
            .CornerRadius(16)
            .Height(36)
            .Padding(14, 0);

    MauiReactor.Button ShowCustomMenuButton()
        => new MauiReactor.Button("Show Popup Menu", OpenCustomMenu)
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
                .Offset(0, 1)
                .Radius(4)
                .Opacity(0.28f)
            );

    void Open() => SetState(s => s.Open = true);
    void Close() => SetState(s => s.Open = false);

    void OpenCustomMenu() => SetState(s => s.OpenCustomMenu = true);
    void CloseCustomMenu() => SetState(s => s.OpenCustomMenu = false);
}
