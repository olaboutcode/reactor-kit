namespace Reactor.Kit.Internals.Sheet;

/// <summary>A pointer sample. Y and velocity are device-independent. Positive velocity points down.</summary>
sealed class SheetPointer
{
    public bool Down { get; init; }

    public bool Up { get; init; }

    public double Y { get; init; }

    public double VelocityY { get; init; }

    public bool InGrabber { get; init; }

    public bool HasScroll { get; init; }

    public bool ScrollAtTop { get; init; }

    public bool Growing { get; init; }
}
