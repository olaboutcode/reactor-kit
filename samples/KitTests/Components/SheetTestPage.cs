namespace KitTests.Components;

/// <summary>
/// A page sheet, a form sheet, and a player sheet on this page.
/// Form sheet inside the page sheet stacks on top of it.
/// </summary>
sealed class SheetTestPage : Component<SheetTestPage.PageState>
{
    static readonly Color Ink = Color.FromArgb("#1C1C1E");
    static readonly Color Paper = Color.FromArgb("#F7F5F2");

    internal sealed class PageState
    {
        public bool PageOpen { get; set; }

        public bool FormOpen { get; set; }

        public bool PlayerOpen { get; set; }

        public bool EdgeOpen { get; set; }

        public bool MenuOpen { get; set; }

        public string Detent { get; set; } = SheetIds.Medium;
    }

    public override VisualNode Render()
        => ContentPage("Sheet",
            Grid("auto,*", "*",
                NavigationBar.Create("Sheet Examples"),
                VStack(
                    OpenButton("Page", OpenPage),
                    OpenButton("Form", OpenForm),
                    OpenButton("Player", OpenPlayer),
                    OpenButton("Edge", OpenEdge),
                    Label($"Detent {State.Detent}")
                        .TextColor(Ink)
                        .HCenter()
                )
                .Spacing(16)
                .Center()
                .GridRow(1),

                PageSheetExample(),
                FormSheetExample(),
                MultipleDetentsExample(),
                EdgeSheetExample()
            )
            .Padding(16)
        )
        .BackgroundColor(Paper)
        .HasNavigationBar(hasNavigationBar: false);

    SheetView PageSheetExample() =>
        PageSheet(
            Grid("auto,*,auto","*",
                Grid("*", "auto,*,auto",
                    CloseButton(ClosePage)
                        .GridColumn(0),
                    Label("Event Page")
                        .FontSize(17)
                        .TextTransform(TextTransform.Uppercase)
                        .TextColor(Ink)
                        .Center()
                        .GridColumn(1),
                    SheetMenu()
                        .GridColumn(2)
                ),

                ScrollView(
                    VStack(
                        Border(
                            Label("thumbnail")
                                .Center()
                        )
                        .Height(300)
                        .BackgroundColor(Colors.LightGrey.WithAlpha(0.3f))
                        .Stroke(Ink.WithAlpha(0.3f))
                        .StrokeCornerRadius(16)
                        .StrokeThickness(.3f),


                        Label("Event Title")
                            .FontSize(17)
                            .TextColor(Ink),

                        Label(EventDescription)
                            .FontSize(15).HorizontalOptions(LayoutOptions.Fill)
                            .HorizontalTextAlignment(TextAlignment.Start)
                            .TextColor(Ink.WithAlpha(0.5f))
                    )
                    .Spacing(16)
                )
                .VerticalScrollBarVisibility(ScrollBarVisibility.Never)
                .GridRow(1),

                Grid("*", "*,*",
                    GlazeView(
                        Button("Cancel")
                        .BackgroundColor(Colors.Transparent)
                        .TextColor(Ink)
                        .Height(44)
                        .OnClicked(CloseForm)
                    )
                    .OnTapped(CloseForm)
                    .CornerRadius(16)
                    .Shadow(ShadowElevation.For(1)),

                    GlazeView(
                        Button("Reserve Spot")
                        .BackgroundColor(Colors.Transparent)
                        .TextColor(Ink)
                        .Height(44)
                        .OnClicked(OpenFormOnPage)
                    )
                    .OnTapped(OpenFormOnPage)
                    .CornerRadius(16)
                    .Shadow(ShadowElevation.For(1))
                    .GridColumn(1)
                )
                .ColumnSpacing(24)
                .GridRow(2)
            )
            .RowSpacing(16)
            .Padding(16)
        )
        .Grabber(propValue: true)
        .CornerRadius(44)
        .IsOpen(State.PageOpen)
        .OnDismiss(ClosePage);

    SheetView FormSheetExample() =>
        FormSheet(
            Grid("auto,*,auto","*",
                Grid(
                    CloseButton(CloseForm)
                        .HStart(),
                    Label("New Event")
                        .FontSize(17)
                        .TextTransform(TextTransform.Uppercase)
                        .TextColor(Ink)
                        .Center()
                ),

                ScrollView(
                    VStack(
                        Border(
                            VStack(
                                Label("Event Title"),
                                Entry().Placeholder("your event title...")
                            )
                            .Spacing(5)
                        )
                        .Padding(16)
                        .BackgroundColor(Colors.LightGrey.WithAlpha(0.3f))
                        .Stroke(Ink.WithAlpha(0.3f))
                        .StrokeCornerRadius(16)
                        .StrokeThickness(.3f),

                        Border(
                            VStack(
                                Label("Event Notes"),
                                Editor()
                                    .Placeholder("your event notes...")
                                    .BackgroundColor(Colors.Transparent)
                                    .Height(160)
                            )
                            .Spacing(5)
                        )
                        .Padding(16)
                        .BackgroundColor(Colors.LightGrey.WithAlpha(0.3f))
                        .Stroke(Ink.WithAlpha(0.3f))
                        .StrokeCornerRadius(16)
                        .StrokeThickness(.3f)
                    )
                    .Spacing(12)
                )
                .GridRow(1),

                Grid("*", "*,*",
                    GlazeView(
                        Button("Cancel")
                        .BackgroundColor(Colors.Transparent)
                        .TextColor(Ink)
                        .Height(44)
                        .OnClicked(CloseForm)
                    )
                    .OnTapped(CloseForm)
                    .CornerRadius(16)
                    .Shadow(ShadowElevation.For(1)),

                    GlazeView(
                        Button("Create Event")
                        .BackgroundColor(Colors.Transparent)
                        .TextColor(Ink)
                        .Height(44)
                        .OnClicked(CloseForm)
                    )
                    .OnTapped(CloseForm)
                    .CornerRadius(16)
                    .Shadow(ShadowElevation.For(1))
                    .GridColumn(1)
                )
                .ColumnSpacing(24)
                .GridRow(2)
            )
            .RowSpacing(16)
            .Padding(16)
        )
        .Grabber(propValue: true)
        .CornerRadius(44)
        .IsOpen(State.FormOpen)
        .OnDismiss(CloseForm);

    SheetView MultipleDetentsExample() =>
        PageSheet(Player())
        .Detents(
            SheetDetent.Fraction("peek", 0.3),
            SheetDetent.Medium,
            SheetDetent.Large)
        .SelectedDetent(State.Detent)
        .OnDetentChanged(id => SetState(s => s.Detent = id))
        .Grabber(propValue: true)
        .UndimmedDetent(SheetIds.Medium)
        .CornerRadius(42)
        .IsOpen(State.PlayerOpen)
        .OnDismiss(ClosePlayer);

    MauiReactor.Grid Player()
        => Grid("auto,*","*",
            Grid(
                CloseButton(ClosePlayer)
                    .HStart(),
                Label("Multiple Detents")
                    .FontSize(17)
                    .TextTransform(TextTransform.Uppercase)
                    .TextColor(Ink)
                    .Center()
            ),
            ScrollView(
                VStack(
                    Label("Drag the grabber. Medium leaves this page interactive.")
                        .FontSize(15)
                        .TextColor(Ink)
                )
                .Spacing(12)
            )
            .GridRow(1)
        )
        .RowSpacing(16)
        .Padding(16);

    SheetView EdgeSheetExample() =>
        PageSheet(
            Grid("auto,*", "*",
                Grid("*", "auto,*,auto",
                    CloseButton(CloseEdge)
                        .GridColumn(0),
                    Label("Bottom edge")
                        .FontSize(17)
                        .TextTransform(TextTransform.Uppercase)
                        .TextColor(Paper)
                        .Center()
                        .GridColumn(1)
                ),

                Label("This card covers the bottom edge.")
                    .FontSize(15)
                    .TextColor(Paper)
                    .HCenter()
                    .VEnd()
                    .GridRow(1)
            )
            .Padding(16)
            .BackgroundColor(Ink)
            .SafeAreaEdges(new SafeAreaEdges(
                SafeAreaRegions.None,
                SafeAreaRegions.None,
                SafeAreaRegions.None,
                SafeAreaRegions.Container))
        )
        .Grabber(propValue: true)
        .CornerRadius(36)
        .IsOpen(State.EdgeOpen)
        .OnDismiss(CloseEdge);

    PopupMenu SheetMenu()
        => PopupMenu()
            .Anchor(MenuButton())
            .Items(menu => menu
                .Item("Switch Order")
                .Rule()
                .Item("Verse Selection"))
            .IsOpen(State.MenuOpen)
            .OnDismiss(CloseMenu)
            .TitleColor(Ink)
            .Panel(panel => panel
                .Padding(8)
                .Shadow(ShadowElevation.For(2)))
            .Stack(stack => stack.Spacing(4))
            .Popup(popup => popup
                .OpenScaleX(0.85)
                .OpenScaleY(0.5))
            .DisableAnchorWhenOpen();

    MauiReactor.Button MenuButton()
        => new MauiReactor.Button("Menu", OpenMenu)
            .TextColor(Paper)
            .BackgroundColor(Ink)
            .FontSize(15)
            .CornerRadius(16)
            .Height(36)
            .Padding(14, 0);

    static MauiReactor.Button OpenButton(string title, Action onClick)
        => new MauiReactor.Button(title, onClick)
            .TextColor(Paper)
            .BackgroundColor(Ink)
            .FontSize(15)
            .CornerRadius(16)
            .Height(36)
            .Padding(14, 0);

    static GlazeView CloseButton(Action onClick)
        => GlazeView(
                Image(HeroIcons.XMark)
                .Aspect(Aspect.AspectFill)
                .Height(24)
                .Width(24)
                .Center()
            )
            .OnTapped(onClick)
            .TintColor(Colors.WhiteSmoke)
            .Width(44)
            .Height(44)
            .CornerRadius(22)
            .Shadow(ShadowElevation.For(1));

    static MauiReactor.Button DoneButton(Action onClick)
        => new MauiReactor.Button("Done", onClick)
            .TextColor(Paper)
            .BackgroundColor(Ink)
            .FontSize(15)
            .CornerRadius(16)
            .Height(36)
            .Padding(14, 0)
            .HEnd();

    void OpenPage()
        => SetState(s =>
        {
            s.PageOpen = true;
            s.FormOpen = false;
            s.PlayerOpen = false;
            s.EdgeOpen = false;
            s.MenuOpen = false;
        });

    void OpenForm()
        => SetState(s =>
        {
            s.PageOpen = false;
            s.FormOpen = true;
            s.PlayerOpen = false;
            s.EdgeOpen = false;
            s.MenuOpen = false;
        });

    void OpenFormOnPage()
        => SetState(s =>
        {
            s.PageOpen = true;
            s.FormOpen = true;
            s.PlayerOpen = false;
            s.EdgeOpen = false;
            s.MenuOpen = false;
        });

    void OpenPlayer()
        => SetState(s =>
        {
            s.PageOpen = false;
            s.FormOpen = false;
            s.PlayerOpen = true;
            s.EdgeOpen = false;
            s.Detent = SheetIds.Medium;
            s.MenuOpen = false;
        });

    void OpenEdge()
        => SetState(s =>
        {
            s.PageOpen = false;
            s.FormOpen = false;
            s.PlayerOpen = false;
            s.EdgeOpen = true;
            s.MenuOpen = false;
        });

    void OpenMenu()
        => SetState(s => s.MenuOpen = true);

    void CloseMenu()
        => SetState(s => s.MenuOpen = false);

    void ClosePage()
        => SetState(s =>
        {
            s.PageOpen = false;
            s.MenuOpen = false;
        });
    void CloseForm() => SetState(s => s.FormOpen = false);
    void ClosePlayer() => SetState(s => s.PlayerOpen = false);
    void CloseEdge() => SetState(s => s.EdgeOpen = false);

    private string EventDescription { get; } = 
    @"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed mattis ex sit amet velit gravida, sed suscipit arcu egestas. Curabitur feugiat euismod pharetra. Donec ultrices odio tempor venenatis efficitur. Donec euismod sem vitae erat ultricies varius. Mauris libero risus, rutrum ac iaculis in, semper vitae ligula. Curabitur non metus eu metus accumsan porttitor. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed pulvinar, velit non consectetur bibendum, velit lacus iaculis urna, eget gravida quam nunc quis felis. Duis nec odio nec arcu venenatis blandit ac at lorem. Praesent efficitur, risus nec pharetra maximus, urna nunc laoreet nunc, in ornare quam velit quis odio. Suspendisse tempor nulla sit amet malesuada pulvinar. Integer pretium sapien et tellus dictum, porta dictum lacus laoreet.
    ";
}
