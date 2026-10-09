namespace KitTests.Components;

sealed class HomePage : Component
{
    public override VisualNode Render()
        => ContentPage("KitTests");
}
