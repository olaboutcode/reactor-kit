namespace Reactor.Kit.Internals.Sheet;

/// <summary>
/// Container measurements in device-independent units.
/// Compact flags come from the platform. Apple uses the trait collection. Android and Windows use 600 and 480.
/// </summary>
sealed class SheetMetrics
{
    public double Width { get; init; }

    public double Height { get; init; }

    public Thickness Safe { get; init; }

    /// <summary>How far the keyboard overlaps the container. Zero when the window already resized.</summary>
    public double KeyboardOverlap { get; init; }

    public bool CompactWidth { get; init; }

    public bool CompactHeight { get; init; }

    /// <summary>Bottom of the focused control in container coordinates. Null when nothing is focused.</summary>
    public double? FocusedBottom { get; init; }
}
