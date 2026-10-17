namespace Reactor.Kit.Internals.Sheet;

/// <summary>The card rectangle and the dim for one set of measurements.</summary>
sealed class SheetFrame
{
    public Rect Card { get; init; }

    public double DimOpacity { get; init; }

    public bool DimHits { get; init; }

    public double CornerRadius { get; init; }

    public bool Centered { get; init; }

    public bool FullScreen { get; init; }

    public bool Grabber { get; init; }

    public string? ActiveId { get; init; }

    public double RestHeight { get; init; }

    /// <summary>How many sheets cover this one. Zero is the front sheet.</summary>
    public double Stack { get; init; }

    /// <summary>Strength of this card's grabber. The front card stays solid. A covered card fades out, then back in as the sheet in front closes.</summary>
    public double GrabberAlpha { get; init; } = 1;
}
