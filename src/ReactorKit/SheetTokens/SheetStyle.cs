namespace Reactor.Kit.SheetTokens;

/// <summary>
/// Which sheet the factories start from. A bottom sheet is either style with more than one detent.
/// </summary>
public enum SheetStyle
{
    /// <summary>A card on the bottom edge. On a regular width it uses a readable width.</summary>
    Page,

    /// <summary>A centered card on a regular width. A page sheet on a compact width.</summary>
    Form,
}
