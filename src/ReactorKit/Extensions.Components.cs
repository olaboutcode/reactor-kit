using MauiReactor;

namespace Reactor.Kit;

/// <summary>
/// Registers ReactorKit handlers and the control factories.
/// </summary>
#pragma warning disable MA0048 // Ignore file name & type mismatch
public static partial class Extensions
#pragma warning restore MA0048
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

        // A single VisualNode is itself a non-generic enumerable, so it is not
        // the children collection the content view expects.
        glaze.AddChildren(children.Length == 1
            ? children
            : [new MauiReactor.Grid(children)]);
        return glaze;
    }
}