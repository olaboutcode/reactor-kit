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
    /// Adds every child. ContentView keeps a single Content, so the last child is the one that stays.
    /// </summary>
    public static GlazeView GlazeView(params VisualNode?[] children)
    {
        var glaze = new GlazeView();
        if (children is not { Length: > 0 })
            return glaze;

        // A single VisualNode is itself a non-generic enumerable, so the array
        // is the children collection the content view expects.
        glaze.AddChildren(children);
        return glaze;
    }
}
