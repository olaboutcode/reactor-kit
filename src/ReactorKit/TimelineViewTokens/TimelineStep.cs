using MauiReactor;
using TimelineLineKind = Reactor.Kit.TimelineView.TimelineLineKind;
using TimelineMarkerKind = Reactor.Kit.TimelineView.TimelineMarkerKind;

namespace Reactor.Kit.TimelineViewTokens;

/// <summary>
/// One step on a <see cref="Reactor.Kit.TimelineView"/>. The label, the marker, and the content are optional.
/// </summary>
public sealed class TimelineStep
{
    internal string? LabelText { get; private set; }

    internal VisualNode? LabelView { get; private set; }

    internal string? ContentText { get; private set; }

    internal VisualNode? ContentView { get; private set; }

    internal int SpanRows { get; private set; } = 1;

    internal TimelineMarkerKind? MarkerKind { get; private set; }

    internal VisualNode? MarkerView { get; private set; }

    internal Color? MarkerTint { get; private set; }

    internal double? MarkerDiameter { get; private set; }

    internal TimelineLineKind? LineKind { get; private set; }

    internal Color? LineTint { get; private set; }

    internal double? LineWeight { get; private set; }

    internal double? LabelFont { get; private set; }

    internal double? ContentFont { get; private set; }

    internal bool HasLabel => LabelText != null || LabelView != null;

    internal bool HasContent => ContentText != null || ContentView != null;

    /// <summary>
    /// Puts the text in a wrapping label and leaves the text color to the theme.
    /// The size is the timeline <c>LabelFontSize</c> unless this step sets one.
    /// A view passed to the other overload replaces it and keeps its own font.
    /// </summary>
    public TimelineStep Label(string text)
    {
        LabelText = text;
        LabelView = null;
        return this;
    }

    /// <summary>
    /// Shows <paramref name="view"/> as the label. Null clears it.
    /// A later call replaces an earlier one.
    /// </summary>
    public TimelineStep Label(VisualNode? view)
    {
        LabelView = view;
        LabelText = null;
        return this;
    }

    /// <summary>
    /// Puts the text in a wrapping label and leaves the text color to the theme.
    /// The size is the timeline <c>ContentFontSize</c> unless this step sets one.
    /// A view passed to the other overload replaces it and keeps its own font.
    /// </summary>
    public TimelineStep Content(string text)
    {
        ContentText = text;
        ContentView = null;
        return this;
    }

    /// <summary>
    /// Shows <paramref name="view"/> as the content. Null clears it.
    /// A later call replaces an earlier one.
    /// </summary>
    public TimelineStep Content(VisualNode? view)
    {
        ContentView = view;
        ContentText = null;
        return this;
    }

    /// <summary>
    /// How many step rows the content occupies, including this step.
    /// A value below 1 is treated as 1. The timeline clamps it to the current run.
    /// </summary>
    public TimelineStep Span(int rows)
    {
        SpanRows = rows < 1 ? 1 : rows;
        return this;
    }

    /// <summary>
    /// Draws a filled circle. A later marker call replaces it.
    /// </summary>
    public TimelineStep MarkerFilled()
    {
        MarkerView = null;
        MarkerKind = TimelineMarkerKind.Filled;
        return this;
    }

    /// <summary>
    /// Draws a ring. A later marker call replaces it.
    /// </summary>
    public TimelineStep MarkerRing()
    {
        MarkerView = null;
        MarkerKind = TimelineMarkerKind.Ring;
        return this;
    }

    /// <summary>
    /// Draws <paramref name="view"/> on the rail. Null draws no marker.
    /// A later marker call replaces it.
    /// </summary>
    public TimelineStep Marker(VisualNode? view)
    {
        MarkerView = view;
        MarkerKind = view == null ? TimelineMarkerKind.None : TimelineMarkerKind.Custom;
        return this;
    }

    /// <summary>
    /// Draws no marker. The rail still meets the point where the marker would be.
    /// </summary>
    public TimelineStep NoMarker()
    {
        MarkerView = null;
        MarkerKind = TimelineMarkerKind.None;
        return this;
    }

    /// <summary>
    /// Tints this step's marker. Null is not used: pass the color to show.
    /// </summary>
    public TimelineStep MarkerColor(Color color)
    {
        MarkerTint = color;
        return this;
    }

    /// <summary>
    /// Sets this step's marker diameter. A negative value is treated as 0.
    /// </summary>
    public TimelineStep MarkerSize(double diameter)
    {
        MarkerDiameter = diameter;
        return this;
    }

    /// <summary>
    /// Draws a solid segment from this step to the next. A later line call replaces it.
    /// </summary>
    public TimelineStep Line()
    {
        LineKind = TimelineLineKind.Solid;
        return this;
    }

    /// <summary>
    /// Draws a dashed segment from this step to the next. A later line call replaces it.
    /// </summary>
    public TimelineStep DashedLine()
    {
        LineKind = TimelineLineKind.Dashed;
        return this;
    }

    /// <summary>
    /// Leaves a gap from this step to the next. A later line call replaces it.
    /// </summary>
    public TimelineStep NoLine()
    {
        LineKind = TimelineLineKind.None;
        return this;
    }

    /// <summary>
    /// Tints the segment that leaves this step.
    /// </summary>
    public TimelineStep LineColor(Color color)
    {
        LineTint = color;
        return this;
    }

    /// <summary>
    /// Sets the thickness of the segment that leaves this step.
    /// A value of 0 draws no line. A negative value is treated as 0.
    /// </summary>
    public TimelineStep LineThickness(double thickness)
    {
        LineWeight = thickness;
        return this;
    }

    /// <summary>
    /// Sets this step's string-label size. A negative value is treated as 0.
    /// A custom label view keeps its own font.
    /// </summary>
    public TimelineStep LabelFontSize(double size)
    {
        LabelFont = size;
        return this;
    }

    /// <summary>
    /// Sets this step's string-content size. A negative value is treated as 0.
    /// A custom content view keeps its own font.
    /// </summary>
    public TimelineStep ContentFontSize(double size)
    {
        ContentFont = size;
        return this;
    }
}
