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
                    .Padding(24, 96)
                    .Spacing(0)),
                GlazeView(
                    Label("Text scrolls behind")
                        .FontSize(17)
                        .TextColor(Ink)
                        .HCenter()
                        .VCenter())
                    .Height(72)
                    .VStart(),
                Samples(),
                ThemeButton()))
            .BackgroundColor(State.Dark ? Night : Paper);
    }

    protected override void OnMounted()
    {
        ApplyTheme(false);
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
                GlazeView(Plus())
                    .Width(44)
                    .Height(44)
                    .CornerRadius(22)
                    .HCenter()
                    .Shadow(new MauiReactor.Shadow()
                        .Brush(Brush.Black)
                        .Offset(0.3, 0.3)
                        .Radius(4)
                        .Opacity(0.28f)),
                GlazeView(
                    Label("Liquid")
                        .FontSize(17)
                        .TextColor(Ink)
                        .VCenter())
                    .Height(44)
                    .Padding(22, 0)
                    .CornerRadius(12)
                    .HCenter()
                    .Shadow(new MauiReactor.Shadow()
                        .Brush(Brush.Black)
                        .Offset(0.3, 0.3)
                        .Radius(4)
                        .Opacity(0.28f)),
                GlazeView(
                    VStack(
                        Label("One child")
                            .FontSize(22)
                            .TextColor(Ink),
                        Entry()
                            .Placeholder("Stays sharp")
                            .TextColor(Ink)
                            .PlaceholderColor(Ink)
                            .BackgroundColor(Colors.Transparent),
                        Button("Test Button")
                            .TextColor(Ink)
                            .BackgroundColor(Colors.White))
                    .Spacing(8))
                .Padding(16, 16)
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
            .Padding(24)
            .Center();

    /// <summary>Heroicons outline plus. The view box is 24 and the stroke is 1.5 with round caps.</summary>
    static MauiReactor.Shapes.Path Plus()
        => new MauiReactor.Shapes.Path()
            .Data("M 12 4.5 V 19.5 M 19.5 12 H 4.5")
            .Stroke(Ink)
            .StrokeThickness(1.5)
            .StrokeLineCap(MauiControls.Shapes.PenLineCap.Round)
            .Width(24)
            .Height(24)
            .HCenter()
            .VCenter();

    static VisualNode[] Lines(Color ink)
    {
        var bands = new[] { "#E85D4C", "#3D7EFF", "#1FA971", "#F0B429" };
        var lines = new VisualNode[32];
        for (var i = 0; i < lines.Length; i++)
        {
            var band = i % 6 == 0;
            var label = Label($"Line {i + 1}    The glass stays put while this text moves underneath.")
                .FontSize(16)
                .TextColor(band ? Ink : ink)
                .Padding(12, 10);
            lines[i] = band
                ? label.BackgroundColor(Color.FromArgb(bands[(i / 6) % bands.Length]))
                : label;
        }

        return lines;
    }
}
