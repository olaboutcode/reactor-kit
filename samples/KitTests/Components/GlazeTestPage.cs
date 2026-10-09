using MauiReactor.Shapes;

namespace KitTests.Components;

sealed class GlazeTestPage : Component
{
    public override VisualNode Render()
        => ContentPage("Glaze",
            Grid(
                Grid(
                    BoxView().Color(Color.FromArgb("#E85D4C")),
                    BoxView().Color(Color.FromArgb("#3D7EFF")).GridColumn(1),
                    BoxView().Color(Color.FromArgb("#1FA971")).GridRow(1),
                    BoxView().Color(Color.FromArgb("#F0B429")).GridRow(1).GridColumn(1))
                .Columns("*,*")
                .Rows("*,*"),
                VStack(
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
                        VStack(
                            Label("One child").FontSize(22).TextColor(Colors.White),
                            Entry().Placeholder("Stays sharp"),
                            Button("Test Button"))
                        .Spacing(8))
                    .Padding(16, 16)
                    .CornerRadius(22),
                    GlazeView(
                        VStack(
                            Label("Title").FontSize(20).TextColor(Colors.White),
                            Label("Two children, one grid.").TextColor(Colors.White)
                        )
                        .Padding(4)
                        .Spacing(16)
                    )
                    .Padding(16)
                    .CornerRadius(22)
                    .TintColor(Color.FromArgb("#1C1C1E")))
                .Spacing(16)
                .Padding(24)
                .Center()));

    /// <summary>Heroicons outline plus. The view box is 24 and the stroke is 1.5 with round caps.</summary>
    static MauiReactor.Shapes.Path Plus()
        => new MauiReactor.Shapes.Path()
            .Data("M 12 4.5 V 19.5 M 19.5 12 H 4.5")
            .Stroke(Colors.White)
            .StrokeThickness(1.5)
            .StrokeLineCap(MauiControls.Shapes.PenLineCap.Round)
            .Width(24)
            .Height(24)
            .HCenter()
            .VCenter();
}
