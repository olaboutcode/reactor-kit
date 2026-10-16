namespace Reactor.Kit.SheetTokens;

/// <summary>How a detent computes its height.</summary>
public enum SheetDetentKind
{
    /// <summary>Half of the large detent. Inactive when the height is compact.</summary>
    Medium,

    /// <summary>The sheet at full expansion, under the status bar.</summary>
    Large,

    /// <summary>A fraction of the large detent.</summary>
    Fraction,

    /// <summary>A height in points, clamped to the large detent.</summary>
    Height,
}
