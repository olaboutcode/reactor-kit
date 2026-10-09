using MauiReactor;

namespace Reactor.Kit;

/// <summary>Acrylic container. The child is a normal MAUI view and is never blurred.</summary>
[Scaffold(typeof(Glaze.GlazeView))]
public partial class GlazeView
{
}

/// <summary>Factory for <see cref="GlazeView"/>.</summary>
public static class GlazeComponent
{
    /// <summary>
    /// One child becomes the content. Several children are laid out in a grid,
    /// because a content view keeps a single child.
    /// </summary>
    public static GlazeView GlazeView(params VisualNode?[] children)
    {
        var glaze = new GlazeView();
        if (children is not { Length: > 0 })
            return glaze;

        // The scaffold constructor takes IEnumerable<VisualNode?>, and a single
        // VisualNode is itself a non-generic enumerable, so it is not that argument.
        glaze.AddChildren(children.Length == 1
            ? children
            : [new MauiReactor.Grid(children)]);
        return glaze;
    }
}
