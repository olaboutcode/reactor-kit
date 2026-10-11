namespace Reactor.Kit;

/// <summary>
/// Page metrics for one rich-text document.
/// An unset size or spacing keeps the view's font size and line spacing.
/// </summary>
public sealed record TextAppearance
{
    public float? FontSize { get; init; }

    public float? LineSpacing { get; init; }

    /// <summary>Extra space before the first line, and before the line that follows a blank line.</summary>
    public float FirstLineIndent { get; init; }

    /// <summary>
    /// A single newline starts a new line.
    /// When this is false, a newline is a space and a blank line starts a paragraph.
    /// </summary>
    public bool PreserveLineBreaks { get; init; } = true;

    /// <summary>
    /// Metrics for the word that begins at the character index.
    /// The default result leaves that word on the page style.
    /// </summary>
    public Func<int, int, RunMetrics>? RunAt { get; init; }
}
