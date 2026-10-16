using KitTests.Components;

namespace KitTests;

public class AppShell : Component
{
    public override VisualNode Render()
        => Shell(new HomePage())
            .FlyoutBehavior(FlyoutBehavior.Disabled);
}
