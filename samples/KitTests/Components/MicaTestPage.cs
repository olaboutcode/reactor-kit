using MauiReactor.Shapes;

namespace KitTests.Components;

/// <summary>
/// Lines scroll behind a mica bar, a dark circular button, and a light panel.
/// </summary>
sealed class MicaTestPage : Component<MicaTestPage.PageState>
{
    static readonly Color Ink = Color.FromArgb("#1C1C1E");
    static readonly Color Paper = Color.FromArgb("#F7F5F2");

    internal sealed class PageState
    {
        public int Count { get; set; }
    }

    public override VisualNode Render()
        => ContentPage("Mica",
            Grid("auto,*", "*",
                NavigationBar.Create("Mica"),
                Grid(
                    ScrollView(
                        VStack(Lines())
                        .Padding(18)
                        .Spacing(0))
                    .VerticalScrollBarVisibility(ScrollBarVisibility.Never)
                    .Margin(0, 0, 0, 16),
                    Bar(),
                    VStack(
                        PlusButton(),
                        Label($"{State.Count}")
                            .FontSize(17)
                            .TextColor(Ink)
                            .HCenter(),
                        LightPanel())
                    .Spacing(24)
                    .Center())
                .GridRow(1))
        )
        .BackgroundColor(Paper)
        .HasNavigationBar(hasNavigationBar: false);

    MauiReactor.Border PlusButton()
        => Border(
            Grid(
                MicaView()
                    .HFill()
                    .VFill()
                    .TintOpacity(0.98)
                    .CornerRadius(22),

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
            .BackgroundColor(Colors.Transparent)
            .OnTapped(() => SetState(s => s.Count++))
        )
        .Width(44)
        .Height(44)
        .StrokeCornerRadius(22)
        .Stroke(Colors.Transparent)
        .BackgroundColor(Colors.Transparent)
        .Shadow(ShadowElevation.For(1));

    static MauiReactor.Grid Bar()
        => Grid(
            MicaView()
                .HFill()
                .VFill(),
            Label("Text scrolls behind")
                .FontSize(17)
                .TextColor(Ink)
                .HCenter()
                .VCenter()
                .InputTransparent(inputTransparent: true))
        .Height(64)
        .BackgroundColor(Colors.Transparent)
        .VStart();

    static MauiReactor.Grid LightPanel()
        => Grid(
            MicaView()
                .HFill()
                .VFill()
                .TintOpacity(0.78)
                .ShadeOpacity(0.69)
                .TintColor(Color.FromArgb("#38383A"))
                .Stroke(Color.FromArgb("#2e2e30"))
                .CornerRadius(16),
            Label("Light mica")
                .TextColor(Colors.WhiteSmoke)
                .HCenter()
                .VCenter()
                .InputTransparent(inputTransparent: true))
        .WidthRequest(240)
        .HeightRequest(72)
        .HCenter();

    static VisualNode[] Lines()
    {
        var bands = new[] { "#E85D4C", "#3D7EFF", "#1FA971", "#F0B429" };
        var lines = new VisualNode[32];
        for (var i = 0; i < lines.Length; i++)
        {
            var band = i % 6 == 0;
            var label = Label($"Line {i + 1} The mica stays put while this text moves underneath.")
                .TextColor(Ink)
                .Padding(10);
            lines[i] = band
                ? label.BackgroundColor(Color.FromArgb(bands[(i / 6) % bands.Length]))
                : label;
        }

        return lines;
    }
}
