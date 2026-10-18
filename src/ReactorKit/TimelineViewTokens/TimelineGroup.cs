using MauiReactor;

namespace Reactor.Kit.TimelineViewTokens;

/// <summary>
/// A header and the steps under it. The header breaks the rail of the steps around the group.
/// </summary>
public sealed class TimelineGroup
{
    readonly List<TimelineStep> _steps = [];

    internal string? HeaderText { get; private set; }

    internal VisualNode? HeaderView { get; private set; }

    internal double? HeaderFont { get; private set; }

    internal IReadOnlyList<TimelineStep> Steps => _steps;

    internal bool HasHeader => HeaderText != null || HeaderView != null;

    /// <summary>
    /// Puts the text in a wrapping bold label and leaves the text color to the theme.
    /// The size is the timeline <c>HeaderFontSize</c> unless this group sets one.
    /// A view passed to the other overload replaces it and keeps its own font.
    /// </summary>
    public TimelineGroup Header(string text)
    {
        HeaderText = text;
        HeaderView = null;
        return this;
    }

    /// <summary>
    /// Shows <paramref name="view"/> across the full width. Null clears the header.
    /// A later call replaces an earlier one.
    /// </summary>
    public TimelineGroup Header(VisualNode? view)
    {
        HeaderView = view;
        HeaderText = null;
        return this;
    }

    /// <summary>
    /// Appends a step under the header. A null action appends a default step.
    /// </summary>
    public TimelineGroup Step(Action<TimelineStep>? configure = null)
    {
        var step = new TimelineStep();
        configure?.Invoke(step);
        _steps.Add(step);
        return this;
    }

    /// <summary>
    /// Sets this group's string-header size. A negative value is treated as 0.
    /// A custom header view keeps its own font.
    /// </summary>
    public TimelineGroup HeaderFontSize(double size)
    {
        HeaderFont = size;
        return this;
    }
}
