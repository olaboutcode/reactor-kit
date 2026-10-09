using MauiReactor.Shapes;

namespace KitTests.Components;

sealed class GlazeTestPage : Component
{
    static readonly Color Ink = Color.FromArgb("#1C1C1E");

    public override VisualNode Render()
        => ContentPage("Glaze",
            Grid(
                ScrollView(
                    VStack(
                        Lines())
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
                Samples()))
            .BackgroundColor(Color.FromArgb("#F7F5F2"));

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
                    .CornerRadius(22)
                    .Stroke(Color.FromArgb("#CCFFFFFF"))
                    .StrokeThickness(0.7)
                    .HCenter()
                    .Shadow(new MauiReactor.Shadow()
                        .Brush(Brush.Black)
                        .Offset(0.3, 0.3)
                        .Radius(4)
                        .Opacity(0.28f)),
                GlazeView(
                    VStack(
                        Label("One child").FontSize(22).TextColor(Ink),
                        Entry().Placeholder("Stays sharp").TextColor(Ink).PlaceholderColor(Ink),
                        Button("Test Button").TextColor(Ink).BackgroundColor(Colors.White))
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

    static VisualNode[] Lines()
    {
        var bands = new[] { "#E85D4C", "#3D7EFF", "#1FA971", "#F0B429" };
        var lines = new VisualNode[32];
        for (var i = 0; i < lines.Length; i++)
        {
            var label = Label($"Line {i + 1}    The glass stays put while this text moves underneath.")
                .FontSize(18)
                .TextColor(Ink)
                .Padding(12, 10);
            lines[i] = i % 6 == 0
                ? label.BackgroundColor(Color.FromArgb(bands[(i / 6) % bands.Length]))
                : label;
        }

        return lines;
    }
}
