namespace KitTests.Components;

sealed class ObjectTestPage : Component
{
    static readonly Mesh Sphere = Mesh.Sphere(1f, 48, 32);

    public override VisualNode Render()
        => ContentPage("Object",
            ObjectView()
                .Mesh(Sphere)
                .Color(Color.FromArgb("#C8C2B8"))
                .Background(Colors.Transparent)
        )
        .BackgroundColor(Color.FromArgb("#1C1C1E"));
}
