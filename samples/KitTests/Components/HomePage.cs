using MauiReactor.Shapes;

namespace KitTests.Components;

/// <summary>
/// A list of the component pages, and a last row that switches the theme.
/// </summary>
sealed class HomePage : Component
{
    static readonly Color Ink = Color.FromArgb("#1C1C1E");
    static readonly Color Paper = Color.FromArgb("#F7F5F2");
    static readonly Color Night = Color.FromArgb("#111110");
    static readonly Color Muted = Color.FromArgb("#8E8E93");

    public override VisualNode Render()
    {
        var ink = Theme.IsDarkTheme ? Paper : Ink;
        return ContentPage("KitTests",
            ScrollView(
                VStack(
                    Label("KitTests")
                        .FontSize(22)
                        .TextColor(ink),
                    Label("Glaze, mica, menu, sheet, switch, checkbox, timeline, rich text, reading, markdown, object, and model.")
                        .FontSize(16)
                        .TextColor(Muted)
                        .Margin(0, 8, 0, 0),
                    VStack(Rows(ink))
                        .Spacing(0)
                        .Margin(0, 24, 0, 0),
                    Appearance(ink)
                        .Margin(0, 24, 0, 0)
                )
                .Spacing(0)
                .Padding(16)
            )
            .VerticalScrollBarVisibility(ScrollBarVisibility.Never)
        )
        .BackgroundColor(Theme.IsDarkTheme ? Night : Paper)
        .Set(Microsoft.Maui.Controls.Shell.NavBarIsVisibleProperty, false);
    }

    VisualNode[] Rows(Color ink) =>
    [
        Row("Glaze", () => Open<GlazeTestPage>(), ink),
        Row("Mica", () => Open<MicaTestPage>(), ink),
        Row("Menu", () => Open<MenuTestPage>(), ink),
        Row("Sheet", () => Open<SheetTestPage>(), ink),
        Row("Switch", () => Open<SwitchTestPage>(), ink),
        Row("Checkbox", () => Open<CheckboxTestPage>(), ink),
        Row("Timeline", () => Open<TimelineTestPage>(), ink),
        Row("Rich text", () => Open<RichTextTestPage>(), ink),
        Row("Reading", () => Open<ReadingTestPage>(), ink),
        Row("Markdown", () => Open<MarkdownTestPage>(), ink),
        Row("Object", () => Open<ObjectTestPage>(), ink),
        Row("Model", () => Open<ModelTestPage>(), ink),
    ];

    void Open<T>() where T : Component, new()
        => _ = Navigation?.PushModalAsync<T>(animated: false);

    static MauiReactor.Grid Row(string title, Action open, Color ink)
        => Grid("44", "*,auto",
                Label(title)
                    .FontSize(16)
                    .TextColor(ink)
                    .VCenter(),
                Chevron()
                    .GridColumn(1)
                    .VCenter()
            )
            .HFill()
            .BackgroundColor(Colors.Transparent)
            .OnTapped(open);

    static MauiReactor.Grid Appearance(Color ink)
        => Grid("44", "*",
                Label(Theme.IsDarkTheme ? "Light" : "Dark")
                    .FontSize(16)
                    .TextColor(ink)
                    .VCenter()
            )
            .HFill()
            .BackgroundColor(Colors.Transparent)
            .OnTapped(AppTheme.ToggleCurrentAppTheme);

    // Heroicons chevron, scaled from the 24 point box into 20. The image is a fixed #0F172A.
    static MauiReactor.Shapes.Path Chevron()
        => new MauiReactor.Shapes.Path()
            .Data("M 6.875 3.75 L 13.125 10 L 6.875 16.25")
            .Stroke(Muted)
            .StrokeThickness(1.25)
            .StrokeLineCap(MauiControls.Shapes.PenLineCap.Round)
            .StrokeLineJoin(MauiControls.Shapes.PenLineJoin.Round)
            .Width(20)
            .Height(20)
            .Set(MauiControls.Shapes.Shape.AspectProperty, MauiControls.Stretch.None)
            .InputTransparent(true);
}
