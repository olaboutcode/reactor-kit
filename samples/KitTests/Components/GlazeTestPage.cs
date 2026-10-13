using MauiReactor.Shapes;

namespace KitTests.Components;

sealed class GlazeTestPage : Component<GlazeTestPage.PageState>
{
    internal sealed class PageState
    {
        public bool Dark { get; set; }
    }

    static readonly Color Ink = Color.FromArgb("#1C1C1E");
    static readonly Color Paper = Color.FromArgb("#F7F5F2");
    static readonly Color Night = Color.FromArgb("#111110");

    public override VisualNode Render()
    {
        var ink = State.Dark ? Paper : Ink;
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
                ThemeButton(),
                TextButton(),
                MarkdownButton(),
                ReadingButton(),
                ObjectButton(),
                ModelButton()
                //,BottomSearchBar().VEnd()
            )
        )
        .BackgroundColor(State.Dark ? Night : Paper);
    }

    protected override void OnMounted()
    {
        ApplyTheme(dark: false);
        base.OnMounted();
    }

    MauiReactor.Button ThemeButton()
        => Button(State.Dark ? "Light" : "Dark", Toggle)
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
        => _ = Navigation?.PushModalAsync<RichTextTestPage>();

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
        => _ = Navigation?.PushModalAsync<MarkdownTestPage>();

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
        => _ = Navigation?.PushModalAsync<ReadingTestPage>();

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
        => _ = Navigation?.PushModalAsync<ObjectTestPage>();

    MauiReactor.Button ModelButton()
        => Button("Model", OpenModel)
            .TextColor(Paper)
            .BackgroundColor(Ink)
            .FontSize(15)
            .CornerRadius(16)
            .Height(36)
            .Padding(14, 0)
            .HEnd()
            .VStart()
            .Margin(16, 238);

    void OpenModel()
        => _ = Navigation?.PushModalAsync<ModelTestPage>();

    void Toggle()
        => SetState(s =>
        {
            s.Dark = !s.Dark;
            ApplyTheme(s.Dark);
        });

    static void ApplyTheme(bool dark)
    {
        if (Application.Current is { } app)
            app.UserAppTheme = dark ? AppTheme.Dark : AppTheme.Light;
    }

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
        .Shadow(new MauiReactor.Shadow()
            .Brush(Brush.Black)
            .Offset(0.3, 0.3)
            .Radius(4)
            .Opacity(0.28f)
        );

    static GlazeView MochaButton()
        => GlazeView(
            Grid(
                Button().Height(44).BackgroundColor(Colors.Transparent),
                Label("Mica Button!")
                    .TextColor(Ink)
                    .FontSize(16)
                    .Center()
            )
        )
        .CornerRadius(12)
        .Shadow(
            new MauiReactor.Shadow()
            .Brush(Brush.Black)
            .Offset(0.3, 0.3)
            .Radius(4)
            .Opacity(0.28f)
        );

    static GlazeView BottomSearchBar()
        => GlazeView(
            Grid("auto,auto", "",
                Entry()
                    .Placeholder("Enter text")
                    .TextColor(Ink)
                    .PlaceholderColor(Colors.Grey.WithAlpha(0.7f))
                    .BackgroundColor(Colors.Transparent),
                HStack(
                    
                )
                .GridRow(1)
                .Height(44)
                .BackgroundColor(Colors.LightBlue)
            )
            .RowSpacing(8)
            .Padding(8)
        )
        .Margin(16,0)
        .CornerRadius(12)
        .Shadow(
            new MauiReactor.Shadow()
            .Brush(Brush.Black)
            .Offset(0.3, 0.3)
            .Radius(4)
            .Opacity(0.28f)
        );

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
