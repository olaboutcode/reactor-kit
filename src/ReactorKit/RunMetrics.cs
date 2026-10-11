namespace Reactor.Kit;

/// <summary>
/// How one word is drawn.
/// The default keeps the page size, the page color, and upright text.
/// </summary>
public readonly record struct RunMetrics
{
    /// <summary>Multiplier of the page font size. A missing or non-positive value stays at 1.</summary>
    public float? Scale { get; init; }

    /// <summary>Fraction of the page font size. Positive raises the word.</summary>
    public float BaselineShift { get; init; }

    /// <summary>Glyph color. A missing value uses the page color.</summary>
    public Color? Color { get; init; }

    public bool Italic { get; init; }

    public bool Bold { get; init; }
}
