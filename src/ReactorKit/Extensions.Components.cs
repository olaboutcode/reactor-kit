using MauiReactor;
using MauiContentView = Microsoft.Maui.Controls.ContentView;

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
        => Create(refAction: null, children);

    /// <summary>
    /// Same children as <see cref="GlazeView(VisualNode?[])"/>.
    /// The callback receives the mounted element, and null when it unmounts.
    /// </summary>
    public static GlazeView GlazeView(Action<MauiContentView?> refAction, params VisualNode?[] children)
        => Create(refAction, children);

    static GlazeView Create(Action<MauiContentView?>? refAction, VisualNode?[]? children)
    {
        var glaze = new GlazeView(refAction);
        if (children is not { Length: > 0 })
            return glaze;
        glaze.AddChildren(children);
        return glaze;
    }
}
