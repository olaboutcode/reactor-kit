using MauiReactor.Shapes;

namespace KitTests.Components;

sealed class HomePage : Component
{
    static readonly Color Ink = Color.FromArgb("#1C1C1E");
    static readonly Color Paper = Color.FromArgb("#F7F5F2");
    static readonly Color Night = Color.FromArgb("#111110");

    public override VisualNode Render()
    {
        var ink = Theme.IsDarkTheme ? Paper : Ink;
        return ContentPage("Glaze",
            Grid(
                ScrollView(
                    VStack(Lines(ink))
                    .Padding(18)
                    .Spacing(0)
                )
                .VerticalScrollBarVisibility(ScrollBarVisibility.Never)
                .Margin(0,0,0,16),
                GlazeView(
                    Label("Text scrolls behind")
                    .FontSize(17)
                    .TextColor(Ink)
                    .HCenter()
                    .VCenter()
                )
                .Height(64)
                .VStart(),
                Samples(),
                HomePage.ThemeButton(),
                TextButton(),
                MarkdownButton(),
                ReadingButton(),
                ObjectButton(),
                ModelButton(),
                MenuButton(),
                SheetButton()
            )
        )
        .BackgroundColor(Theme.IsDarkTheme ? Night : Paper)
        .Set(Microsoft.Maui.Controls.Shell.NavBarIsVisibleProperty, false);
    }

    static MauiReactor.Button ThemeButton()
        => Button(Theme.IsDarkTheme ? "Light" : "Dark", AppTheme.ToggleCurrentAppTheme)
            .TextColor(Paper)
            .BackgroundColor(Ink)
            .FontSize(15)
            .CornerRadius(16)
            .Height(36)
            .Padding(14, 0)
            .HEnd()
            .VStart()
            .Margin(16, 18);

    MauiReactor.Button TextButton()
        => Button("Text", OpenText)
            .TextColor(Paper)
            .BackgroundColor(Ink)
            .FontSize(15)
            .CornerRadius(16)
            .Height(36)
            .Padding(14, 0)
            .HEnd()
            .VStart()
            .Margin(16, 62);

    void OpenText()
        => _ = Navigation?.PushModalAsync<RichTextTestPage>(animated: false);

    MauiReactor.Button MarkdownButton()
        => Button("Markdown", OpenMarkdown)
            .TextColor(Paper)
            .BackgroundColor(Ink)
            .FontSize(15)
            .CornerRadius(16)
            .Height(36)
            .Padding(14, 0)
            .HEnd()
            .VStart()
            .Margin(16, 106);

    void OpenMarkdown()
        => _ = Navigation?.PushModalAsync<MarkdownTestPage>(animated: false);

    MauiReactor.Button ReadingButton()
        => Button("Reading", OpenReading)
            .TextColor(Paper)
            .BackgroundColor(Ink)
            .FontSize(15)
            .CornerRadius(16)
            .Height(36)
            .Padding(14, 0)
            .HEnd()
            .VStart()
            .Margin(16, 150);

    void OpenReading()
        => _ = Navigation?.PushModalAsync<ReadingTestPage>(animated: false);

    MauiReactor.Button ObjectButton()
        => Button("Object", OpenObject)
            .TextColor(Paper)
            .BackgroundColor(Ink)
            .FontSize(15)
            .CornerRadius(16)
            .Height(36)
            .Padding(14, 0)
            .HEnd()
            .VStart()
            .Margin(16, 194);

    void OpenObject()
        => _ = Navigation?.PushModalAsync<ObjectTestPage>(animated: false);

    MauiReactor.Button ModelButton()
        => Button("Model", OpenModelTestPage)
            .TextColor(Paper)
            .BackgroundColor(Ink)
            .FontSize(15)
            .CornerRadius(16)
            .Height(36)
            .Padding(14, 0)
            .HEnd()
            .VStart()
            .Margin(16, 238);

    void OpenModelTestPage()
        => _ = Navigation?.PushModalAsync<ModelTestPage>(animated: false);

    MauiReactor.Button MenuButton()
        => Button("Menu", GotoMenuTestPage)
            .TextColor(Paper)
            .BackgroundColor(Ink)
            .FontSize(15)
            .CornerRadius(16)
            .Height(36)
            .Padding(14, 0)
            .HEnd()
            .VStart()
            .Margin(16, 282);

    void GotoMenuTestPage()
        => _ = Navigation?.PushModalAsync<MenuTestPage>(animated: false);

    MauiReactor.Button SheetButton()
        => Button("Sheet", GotoSheetTestPage)
            .TextColor(Paper)
            .BackgroundColor(Ink)
            .FontSize(15)
            .CornerRadius(16)
            .Height(36)
            .Padding(14, 0)
            .HEnd()
            .VStart()
            .Margin(16, 326);

    void GotoSheetTestPage()
        => _ = Navigation?.PushModalAsync<SheetTestPage>(animated: false);

    static VStack Samples()
        => VStack(
                RoundButton(),
                MochaButton(),
                GlazeView(
                    VStack(
                        Entry()
                            .Placeholder("Enter text")
                            .TextColor(Ink)
                            .PlaceholderColor(Colors.Grey.WithAlpha(0.7f))
                            .BackgroundColor(Colors.Transparent),
                        Button("Test Button")
                            .TextColor(Ink)
                            .BackgroundColor(Colors.White))
                    .Spacing(16))
                .Padding(16)
                .CornerRadius(22),
                GlazeView(
                    VStack(
                        Label("Title").FontSize(20).TextColor(Colors.White),
                        Label("Two children, one grid.").TextColor(Colors.White))
                    .Padding(4)
                    .Spacing(16))
                .Padding(16)
                .CornerRadius(22)
                .TintColor(Color.FromArgb("#1C1C1E")))
            .Spacing(16)
            .Padding(8)
            .Center();

    static GlazeView RoundButton()
        => GlazeView(
            new MauiReactor.Shapes.Path()
            .Data("M 12 4.5 V 19.5 M 19.5 12 H 4.5")
            .Stroke(Ink)
            .StrokeThickness(1.5)
            .StrokeLineCap(MauiControls.Shapes.PenLineCap.Round)
            .Width(24)
            .Height(24)
            .HCenter()
            .VCenter()
        )
        .Width(44)
        .Height(44)
        .CornerRadius(22)
        .HCenter()
        .Shadow(ShadowElevation.For(1));

    static GlazeView MochaButton()
        => GlazeView(
            Grid(
                Button()
                    .Height(44)
                    .BackgroundColor(Colors.Transparent),
                Label("Mica Button!")
                    .TextColor(Ink)
                    .FontSize(16)
                    .Center()
            )
        )
        .CornerRadius(12)
        .Shadow(ShadowElevation.For(1));

    static VisualNode[] Lines(Color ink)
    {
        var bands = new[] { "#E85D4C", "#3D7EFF", "#1FA971", "#F0B429" };
        var lines = new VisualNode[32];
        for (var i = 0; i < lines.Length; i++)
        {
            var band = i % 6 == 0;
            var label = Label($"Line {i + 1} The glass stays put while this text moves underneath.")
                .TextColor(band ? Ink : ink)
                .Padding(10);
            lines[i] = band
                ? label.BackgroundColor(Color.FromArgb(bands[(i / 6) % bands.Length]))
                : label;
        }

        return lines;
    }
}
