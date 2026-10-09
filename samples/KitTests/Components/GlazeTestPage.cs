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
                    .Padding(20)
                    .CornerRadius(22)
                    .TintColor(Color.FromArgb("#1C1C1E")))
                .Spacing(16)
                .Padding(24)
                .Center()));
}
