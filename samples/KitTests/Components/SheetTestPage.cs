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

        public string Detent { get; set; } = SheetIds.Medium;
    }

    public override VisualNode Render()
        => ContentPage("Sheet",
            Grid("auto,*", "*",
                NavigationBar.Create("Sheet"),
                VStack(
                    Label("The sheets stay on this page.")
                        .TextColor(Ink)
                        .FontSize(15)
                        .HCenter(),
                    OpenButton("Page", OpenPage),
                    OpenButton("Form", OpenForm),
                    OpenButton("Player", OpenPlayer),
                    Label($"Detent {State.Detent}")
                        .TextColor(Ink)
                        .FontSize(15)
                        .HCenter()
                )
                .Spacing(16)
                .Center()
                .GridRow(1),
                PageSheet(
                    VStack(
                        Label("New message")
                            .FontSize(17)
                            .TextColor(Ink),
                        Editor()
                            .Placeholder("Message")
                            .Height(180),
                        OpenButton("Form sheet", OpenFormOnPage),
                        DoneButton(ClosePage)
                    )
                    .Spacing(12)
                    .Padding(20)
                )
                .Grabber(propValue: true)
                .CornerRadius(16)
                .IsOpen(State.PageOpen)
                .OnDismiss(ClosePage),

                FormSheet(
                    VStack(
                        Label("New event").FontSize(17).TextColor(Ink),
                        Entry().Placeholder("Title"),
                        Editor()
                            .Placeholder("Notes")
                            .Height(160),
                        DoneButton(CloseForm)
                    )
                    .Spacing(12)
                    .Padding(20)
                )
                .Grabber(propValue: true)
                .CornerRadius(16)
                .IsOpen(State.FormOpen)
                .OnDismiss(CloseForm),

                PageSheet(Player())
                    .Detents(
                        SheetDetent.Fraction("peek", 0.3),
                        SheetDetent.Medium,
                        SheetDetent.Large)
                    .SelectedDetent(State.Detent)
                    .OnDetentChanged(id => SetState(s => s.Detent = id))
                    .Grabber(propValue: true)
                    .UndimmedDetent(SheetIds.Medium)
                    .CornerRadius(16)
                    .IsOpen(State.PlayerOpen)
                    .OnDismiss(ClosePlayer)
            )
            .Padding(16)
        )
        .BackgroundColor(Paper)
        .HasNavigationBar(hasNavigationBar: false);

    static MauiReactor.ScrollView Player()
        => ScrollView(
            VStack(
                Label("Now playing")
                    .FontSize(17)
                    .TextColor(Ink),
                Label("Drag the grabber. Medium leaves this page interactive.")
                    .FontSize(15)
                    .TextColor(Ink)
            )
            .Spacing(12)
            .Padding(20, 8)
        );

    static MauiReactor.Button OpenButton(string title, Action onClick)
        => new MauiReactor.Button(title, onClick)
            .TextColor(Paper)
            .BackgroundColor(Ink)
            .FontSize(15)
            .CornerRadius(16)
            .Height(36)
            .Padding(14, 0);

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
        });

    void OpenForm()
        => SetState(s =>
        {
            s.PageOpen = false;
            s.FormOpen = true;
            s.PlayerOpen = false;
        });

    void OpenFormOnPage()
        => SetState(s =>
        {
            s.PageOpen = true;
            s.FormOpen = true;
            s.PlayerOpen = false;
        });

    void OpenPlayer()
        => SetState(s =>
        {
            s.PageOpen = false;
            s.FormOpen = false;
            s.PlayerOpen = true;
            s.Detent = SheetIds.Medium;
        });

    void ClosePage() => SetState(s => s.PageOpen = false);

    void CloseForm() => SetState(s => s.FormOpen = false);

    void ClosePlayer() => SetState(s => s.PlayerOpen = false);
}
