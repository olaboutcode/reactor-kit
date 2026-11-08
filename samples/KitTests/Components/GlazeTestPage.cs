using MauiReactor.Shapes;

namespace KitTests.Components;

/// <summary>
/// Lines scroll behind a glaze banner. A round glaze sits on an opaque bar above that scroll.
/// </summary>
sealed class GlazeTestPage : Component
{
    static readonly Color Ink = Color.FromArgb("#1C1C1E");
    static readonly Color Paper = Color.FromArgb("#F7F5F2");
    static readonly Color Night = Color.FromArgb("#111110");

    public override VisualNode Render()
    {
        var ink = Theme.IsDarkTheme ? Paper : Ink;
        return ContentPage("Glaze",
            Grid("auto,*", "*",
                Grid(
                    NavigationBar.Create("Glaze")
                        .TextColor(ink)
                )
                .Padding(16, 16, 16, 0),
                Grid("auto,*", "*",
                    Grid(
                        ScrollView(
                            VStack(Lines(ink))
                            .Padding(18)
                            .Spacing(0)
                        )
                        .VerticalScrollBarVisibility(ScrollBarVisibility.Never)
                        .Margin(0, 0, 0, 16),
                        Samples()
                    )
                    .GridRow(1)
                )
                .GridRow(1)
            )
        )
        .BackgroundColor(Theme.IsDarkTheme ? Night : Paper)
        .HasNavigationBar(hasNavigationBar: false);
    }

    static VStack Samples()
        => VStack(
                CoveredBar(),
                RoundButton(),
                GlazeTextButton(),
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

    // The page color, above the scroll, so a round glaze here must not pick up the lines.
    // An auto row sizes to the button. A star row inside the page's auto row collapses.
    static MauiReactor.Grid CoveredBar()
        => Grid("auto", "*", RoundButton().HStart().VCenter())
            .BackgroundColor(Theme.IsDarkTheme ? Night : Paper)
            .Padding(16, 8);

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

    static GlazeView GlazeTextButton()
        => GlazeView(
            Grid(
                Button()
                    .Height(44)
                    .BackgroundColor(Colors.Transparent),
                Label("Glaze Button!")
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
