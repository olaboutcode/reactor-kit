using System.Globalization;
using System.Runtime.InteropServices;
using MauiReactor;
using MauiReactor.Shapes;
using Reactor.Kit.TimelineViewTokens;

namespace Reactor.Kit;

/// <summary>
/// One vertical timeline. Labels, content, and group headers are any view.
/// The rail is a solid line, a dashed line, or a gap.
/// </summary>
public sealed partial class TimelineView : Component
{
    const double SizeSlack = 1;

    [Prop] private Color _lineColor = Color.FromArgb("#C6C6C8");
    [Prop] private double _lineThickness = 2;
    [Prop] private double _dashLength = 4;
    [Prop] private double _dashGap = 4;
    [Prop] private Color _markerColor = Color.FromArgb("#1C1C1E");
    [Prop] private double _markerSize = 10;
    [Prop] private double _markerStrokeThickness = 2;
    [Prop] private double _stepSpacing = 32;
    [Prop] private double _labelGap = 12;
    [Prop] private double _contentGap = 12;
    [Prop] private double? _labelWidth;
    [Prop] private double _groupSpacing = 28;
    [Prop] private double _headerGap = 12;
    [Prop] private double _labelFontSize = 15;
    [Prop] private double _contentFontSize = 16;
    [Prop] private double _headerFontSize = 13;

    readonly TimelineSteps _steps;
    readonly Sizes _sizes = new();
    TimelineLabelSide _labelSide = TimelineLabelSide.Leading;
    TimelineLineKind _lineKind = TimelineLineKind.Solid;
    TimelineMarkerKind _markerKind = TimelineMarkerKind.Filled;

    internal TimelineView(Action<TimelineSteps>? build)
    {
        _steps = new TimelineSteps();
        build?.Invoke(_steps);
    }

    /// <summary>
    /// Puts the label, then the rail, then the content. This is the default.
    /// A later call replaces an earlier one.
    /// </summary>
    public TimelineView LabelsLeading()
    {
        _labelSide = TimelineLabelSide.Leading;
        return this;
    }

    /// <summary>
    /// Puts the content, then the rail, then the label.
    /// A later call replaces an earlier one.
    /// </summary>
    public TimelineView LabelsTrailing()
    {
        _labelSide = TimelineLabelSide.Trailing;
        return this;
    }

    /// <summary>
    /// Puts the rail, then the label, then the content.
    /// A later call replaces an earlier one.
    /// </summary>
    public TimelineView LabelsInside()
    {
        _labelSide = TimelineLabelSide.Inside;
        return this;
    }

    /// <summary>
    /// Draws solid segments. A step can replace the segment that leaves it.
    /// </summary>
    public new TimelineView Line()
    {
        _lineKind = TimelineLineKind.Solid;
        return this;
    }

    /// <summary>
    /// Draws dashed segments. A step can replace the segment that leaves it.
    /// </summary>
    public TimelineView DashedLine()
    {
        _lineKind = TimelineLineKind.Dashed;
        return this;
    }

    /// <summary>
    /// Draws no segments. A step can replace the segment that leaves it.
    /// </summary>
    public TimelineView NoLine()
    {
        _lineKind = TimelineLineKind.None;
        return this;
    }

    /// <summary>
    /// Draws a filled circle on each step that does not set its own marker.
    /// </summary>
    public TimelineView MarkerFilled()
    {
        _markerKind = TimelineMarkerKind.Filled;
        return this;
    }

    /// <summary>
    /// Draws a ring on each step that does not set its own marker.
    /// </summary>
    public TimelineView MarkerRing()
    {
        _markerKind = TimelineMarkerKind.Ring;
        return this;
    }

    /// <summary>
    /// Draws no marker on each step that does not set its own marker.
    /// </summary>
    public TimelineView NoMarker()
    {
        _markerKind = TimelineMarkerKind.None;
        return this;
    }

    public override VisualNode Render()
    {
        _sizes.Refresh = Invalidate;
        if (_steps.Blocks.Count == 0)
            return new MauiReactor.Grid().HeightRequest(0);

        var frame = Measure();
        var parts = new List<VisualNode>(_steps.Blocks.Count * 2);
        for (var i = 0; i < _steps.Blocks.Count; i++)
        {
            if (i > 0 && frame.GroupSpacing > 0)
                parts.Add(Spacer(frame.GroupSpacing));
            parts.Add(BlockView(_steps.Blocks[i], i, frame));
        }

        return Watch(
            VStack([.. parts])
                .Spacing(0)
                .HorizontalOptions(LayoutOptions.Fill)
                .VerticalOptions(LayoutOptions.Start),
            "frame");
    }

    protected override void MergeWith(VisualNode newNode)
    {
        if (newNode is TimelineView next)
        {
            next._sizes.CopyFrom(_sizes);
            _sizes.Refresh = next.Invalidate;
        }

        base.MergeWith(newNode);
    }

    protected override void OnWillUnmount()
    {
        _sizes.Refresh = null;
        base.OnWillUnmount();
    }

    VisualNode BlockView(TimelineSteps.Block block, int index, Frame frame)
    {
        if (block is TimelineSteps.Run run)
            return RunView(run.Steps, index, frame);

        var group = ((TimelineSteps.Grouped)block).Group;
        var parts = new List<VisualNode>(3);
        if (HeaderNode(group, index) is VisualNode header)
            parts.Add(header);
        if (group.Steps.Count > 0)
        {
            if (parts.Count > 0 && frame.HeaderGap > 0)
                parts.Add(Spacer(frame.HeaderGap));
            parts.Add(RunView(group.Steps, index, frame));
        }

        if (parts.Count == 0)
            return Spacer(0);
        if (parts.Count == 1)
            return parts[0];
        return VStack([.. parts]).Spacing(0);
    }

    VisualNode? HeaderNode(TimelineGroup group, int index)
    {
        if (group.HeaderView != null)
            return FillWidth(Watch(group.HeaderView, Slot(index, -1, "header")));
        if (group.HeaderText == null)
            return null;

        return new MauiReactor.Label(group.HeaderText)
            .FontSize(TextSize(group.HeaderFont ?? _headerFontSize))
            .FontAttributes(FontAttributes.Bold)
            .HorizontalTextAlignment(TextAlignment.Start)
            .LineBreakMode(LineBreakMode.WordWrap)
            .MinimumWidthRequest(0)
            .HFill();
    }

    VisualNode RunView(IReadOnlyList<TimelineStep> steps, int block, Frame frame)
    {
        var count = steps.Count;
        if (count == 0)
            return Spacer(0);

        var spans = Spans(steps);
        var rows = RowHeights(steps, spans, block, frame);
        var columns = Columns(frame);
        var grid = new MauiReactor.Grid()
            .Rows(string.Join(',', rows.Select(Format)))
            .Columns(columns.Spec)
            .RowSpacing(0)
            .ColumnSpacing(0)
            .HorizontalOptions(LayoutOptions.Fill)
            .VerticalOptions(LayoutOptions.Start);

        if (columns.Rail is int rail && count > 1)
            grid.Add(Rail(steps, rows, block, rail, frame));

        for (var i = 0; i < count; i++)
        {
            var step = steps[i];
            var anchor = MarkerAt(step, block, i, frame);
            if (columns.Label is int label && LabelNode(step, block, i, frame, anchor) is VisualNode labelNode)
                grid.Add(Cell(labelNode, label, i));
            if (columns.Rail is int markerColumn && MarkerNode(step, block, i, frame, anchor) is VisualNode marker)
                grid.Add(Cell(marker, markerColumn, i));
            if (columns.Content is int content && spans[i] > 0 && ContentNode(step, block, i) is VisualNode contentNode)
                grid.Add(Cell(contentNode, content, i, spans[i]));
        }

        return grid;
    }

    MauiReactor.Grid Rail(IReadOnlyList<TimelineStep> steps, double[] rows, int block, int column, Frame frame)
    {
        var total = 0d;
        var anchors = new MarkerPoint[steps.Count];
        for (var i = 0; i < steps.Count; i++)
        {
            anchors[i] = MarkerAt(steps[i], block, i, frame);
            total += rows[i];
        }

        var overlay = new MauiReactor.Grid()
            .HeightRequest(total)
            .WidthRequest(Math.Max(frame.RailWidth, 0))
            .HorizontalOptions(LayoutOptions.Fill)
            .VerticalOptions(LayoutOptions.Start)
            .InputTransparent(true)
            .ZIndex(0);
        At(overlay, column, 0, steps.Count);

        var top = 0d;
        for (var i = 0; i < steps.Count - 1; i++)
        {
            var start = top + anchors[i].Center + anchors[i].Radius;
            top += rows[i];
            var end = top + anchors[i + 1].Center - anchors[i + 1].Radius;
            if (Segment(steps[i], frame) is not SegmentStyle segment)
                continue;
            if (end - start < 0.5)
                continue;
            overlay.Add(Line(segment, start, end - start, frame));
        }

        return overlay;
    }

    VisualNode? LabelNode(TimelineStep step, int block, int index, Frame frame, MarkerPoint anchor)
    {
        VisualNode? node = step.LabelView;
        if (step.LabelText != null)
        {
            var text = new MauiReactor.Label(step.LabelText)
                .FontSize(TextSize(step.LabelFont ?? _labelFontSize))
                .HorizontalTextAlignment(TextAlignment.Start)
                .LineBreakMode(LineBreakMode.WordWrap)
                .MinimumWidthRequest(0)
                .VerticalOptions(LayoutOptions.Start);
            if (frame.LabelFixed)
            {
                text.HFill();
            }
            else
            {
                text.HStart();
                if (frame.LabelCap > 0)
                {
                    text.MaximumWidthRequest(frame.LabelCap);
                }
            }

            node = text;
        }

        if (node == null)
            return null;

        node = Watch(node, Slot(block, index, "label"));
        return Start(node, anchor.LabelTop);
    }

    VisualNode? ContentNode(TimelineStep step, int block, int index)
    {
        if (step.ContentText != null)
        {
            return Watch(
                new MauiReactor.Label(step.ContentText)
                    .FontSize(TextSize(step.ContentFont ?? _contentFontSize))
                    .HorizontalTextAlignment(TextAlignment.Start)
                    .LineBreakMode(LineBreakMode.WordWrap)
                    .MinimumWidthRequest(0)
                    .HFill()
                    .VStart(),
                Slot(block, index, "content"));
        }

        if (step.ContentView == null)
            return null;
        return Watch(step.ContentView, Slot(block, index, "content"));
    }

    VisualNode? MarkerNode(TimelineStep step, int block, int index, Frame frame, MarkerPoint anchor)
    {
        var kind = step.MarkerKind ?? _markerKind;
        if (kind == TimelineMarkerKind.None)
            return null;

        var color = step.MarkerTint ?? _markerColor;
        var size = Math.Max(0, step.MarkerDiameter ?? frame.MarkerSize);
        VisualNode node = kind switch
        {
            TimelineMarkerKind.Custom when step.MarkerView != null => step.MarkerView,
            TimelineMarkerKind.Ring => Disc(size, Colors.Transparent, color, frame.MarkerStroke),
            _ => Disc(size, color, Colors.Transparent, 0),
        };

        if (kind == TimelineMarkerKind.Custom)
            node = Watch(node, Slot(block, index, "marker"));

        return MarkerHost(node, Math.Max(0, anchor.Center - anchor.Radius));
    }

    static MauiReactor.Border Disc(double size, Color fill, Color stroke, double thickness)
        => new MauiReactor.Border()
            .BackgroundColor(fill)
            .Stroke(thickness <= 0 ? Colors.Transparent : stroke)
            .StrokeThickness(Math.Max(0, thickness))
            .StrokeCornerRadius(size / 2)
            .WidthRequest(size)
            .HeightRequest(size)
            .InputTransparent(inputTransparent: true);

    static MauiReactor.Shapes.Line Line(SegmentStyle segment, double top, double length, Frame frame)
    {
        var thickness = segment.Thickness;
        var line = new MauiReactor.Shapes.Line()
            .X1(thickness / 2)
            .Y1(0)
            .X2(thickness / 2)
            .Y2(length)
            .Stroke(segment.Color)
            .StrokeThickness(thickness)
            .StrokeLineCap(Microsoft.Maui.Controls.Shapes.PenLineCap.Flat)
            .WidthRequest(thickness)
            .HeightRequest(length)
            .Margin(0, top, 0, 0)
            .HCenter()
            .VStart()
            .InputTransparent(true)
            .ZIndex(0);

        if (segment.Dashed && frame.DashLength > 0 && frame.DashGap > 0 && thickness > 0)
        {
            var dashes = new Microsoft.Maui.Controls.DoubleCollection
            {
                frame.DashLength / thickness,
                frame.DashGap / thickness,
            };
            line.StrokeDashArray(dashes);
        }

        return line;
    }

    static int[] Spans(IReadOnlyList<TimelineStep> steps)
    {
        var spans = new int[steps.Count];
        for (var i = 0; i < steps.Count; i++)
        {
            if (!steps[i].HasContent)
            {
                spans[i] = 0;
                continue;
            }

            var span = 1;
            var requested = steps[i].SpanRows;
            for (var j = i + 1; j < steps.Count && span < requested; j++)
            {
                if (steps[j].HasContent)
                    break;
                span++;
            }

            spans[i] = span;
        }

        return spans;
    }

    double[] RowHeights(IReadOnlyList<TimelineStep> steps, int[] spans, int block, Frame frame)
    {
        var rows = new double[steps.Count];
        for (var i = 0; i < steps.Count; i++)
        {
            var anchor = MarkerAt(steps[i], block, i, frame);
            rows[i] = Math.Max(frame.StepSpacing, anchor.Head);
        }

        for (var i = 0; i < steps.Count; i++)
        {
            var span = spans[i];
            if (span < 1 || !TrySize(Slot(block, i, "content"), out _, out var content))
                continue;

            var sum = 0d;
            for (var k = 0; k < span; k++)
                sum += rows[i + k];
            if (content <= sum)
                continue;

            var extra = (content - sum) / span;
            for (var k = 0; k < span; k++)
                rows[i + k] += extra;
        }

        return rows;
    }

    MarkerPoint MarkerAt(TimelineStep step, int block, int index, Frame frame)
    {
        var kind = step.MarkerKind ?? _markerKind;
        var size = Math.Max(0, step.MarkerDiameter ?? frame.MarkerSize);
        var label = 0d;
        if (step.HasLabel && TrySize(Slot(block, index, "label"), out _, out var labelHeight))
            label = labelHeight;

        var marker = kind switch
        {
            TimelineMarkerKind.None => 0,
            TimelineMarkerKind.Custom when TrySize(Slot(block, index, "marker"), out _, out var markerHeight) => markerHeight,
            TimelineMarkerKind.Custom => size,
            _ => size,
        };

        double center;
        double head;
        if (label <= 0 && marker <= 0)
        {
            center = frame.MarkerSize / 2;
            head = frame.MarkerSize;
        }
        else if (label <= 0)
        {
            center = marker / 2;
            head = marker;
        }
        else
        {
            head = Math.Max(label, marker);
            center = head / 2;
        }

        var radius = kind == TimelineMarkerKind.None ? 0 : marker / 2;
        var labelTop = label <= 0 ? 0 : Math.Max(0, center - (label / 2));
        return new MarkerPoint(center, radius, head, labelTop);
    }

    SegmentStyle? Segment(TimelineStep step, Frame frame)
    {
        var kind = step.LineKind ?? _lineKind;
        var thickness = Math.Max(0, step.LineWeight ?? frame.LineThickness);
        if (kind == TimelineLineKind.None || thickness <= 0)
            return null;
        if (kind == TimelineLineKind.Dashed && (frame.DashLength <= 0 || frame.DashGap <= 0))
            kind = TimelineLineKind.Solid;
        return new SegmentStyle(step.LineTint ?? _lineColor, thickness, kind == TimelineLineKind.Dashed);
    }

    ColumnMap Columns(Frame frame)
    {
        var parts = new List<string>(5);
        int? label = null;
        int? rail = null;
        int? content = null;
        var next = 0;

        void Add(string size, ref int? slot)
        {
            parts.Add(size);
            slot = next;
            next++;
        }

        void Gap(double size)
        {
            if (size <= 0)
                return;
            parts.Add(Format(size));
            next++;
        }

        void AddLabel()
        {
            if (!frame.ShowLabel)
                return;
            var slot = label;
            Add(frame.LabelKnown ? Format(frame.LabelWidth) : "auto", ref slot);
            label = slot;
        }

        void AddRail()
        {
            var slot = rail;
            Add(frame.RailKnown ? Format(frame.RailWidth) : "auto", ref slot);
            rail = slot;
        }

        void AddContent()
        {
            if (!frame.ShowContent)
                return;
            var slot = content;
            Add("*", ref slot);
            content = slot;
        }

        switch (_labelSide)
        {
            case TimelineLabelSide.Trailing:
                AddContent();
                if (frame.ShowContent)
                    Gap(frame.ContentGap);
                AddRail();
                if (frame.ShowLabel)
                    Gap(frame.LabelGap);
                AddLabel();
                break;
            case TimelineLabelSide.Inside:
                AddRail();
                if (frame.ShowLabel)
                    Gap(frame.LabelGap);
                AddLabel();
                if (frame.ShowContent)
                    Gap(frame.ContentGap);
                AddContent();
                break;
            default:
                AddLabel();
                if (frame.ShowLabel)
                    Gap(frame.LabelGap);
                AddRail();
                if (frame.ShowContent)
                    Gap(frame.ContentGap);
                AddContent();
                break;
        }

        return new ColumnMap(string.Join(',', parts), label, rail, content);
    }

    Frame Measure()
    {
        var showLabel = false;
        var showContent = false;
        var labelsKnown = _labelWidth != null;
        var labelWidth = Math.Max(0, _labelWidth ?? 0);
        var stringLabelWidth = 0d;
        var customLabelWidth = 0d;
        var railKnown = true;
        var railWidth = Math.Max(0, _lineThickness);
        var markerSize = Math.Max(0, _markerSize);
        var pendingLabels = 0;
        var pendingMarkers = 0;

        foreach (var (block, steps) in Enumerate())
        {
            for (var i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                if (step.HasLabel)
                {
                    showLabel = true;
                    if (!labelsKnown)
                    {
                        if (TrySize(Slot(block, i, "label"), out var width, out _))
                        {
                            if (step.LabelText != null)
                                stringLabelWidth = Math.Max(stringLabelWidth, width);
                            else
                                customLabelWidth = Math.Max(customLabelWidth, width);
                        }
                        else
                        {
                            pendingLabels++;
                        }
                    }
                }

                if (step.HasContent)
                    showContent = true;

                railWidth = Math.Max(railWidth, Math.Max(0, step.LineWeight ?? _lineThickness));

                var kind = step.MarkerKind ?? _markerKind;
                var size = Math.Max(0, step.MarkerDiameter ?? markerSize);
                if (kind is TimelineMarkerKind.Filled or TimelineMarkerKind.Ring)
                {
                    railWidth = Math.Max(railWidth, size);
                }
                else if (kind == TimelineMarkerKind.Custom)
                {
                    railKnown = false;
                    if (TrySize(Slot(block, i, "marker"), out var width, out _))
                        railWidth = Math.Max(railWidth, width);
                    else
                        pendingMarkers++;
                }
            }
        }

        var labelCap = 0d;
        if (!labelsKnown)
        {
            labelsKnown = showLabel && pendingLabels == 0;
            if (TrySize("frame", out var timelineWidth, out _) && timelineWidth > 0)
            {
                var reserved = railWidth;
                if (showLabel)
                    reserved += Math.Max(0, _labelGap);
                if (showContent)
                    reserved += Math.Max(0, _contentGap);
                var free = Math.Max(0, timelineWidth - reserved);
                labelCap = showContent ? free / 2 : free;
                if (stringLabelWidth > labelCap)
                    stringLabelWidth = labelCap;
            }

            labelWidth = Math.Max(stringLabelWidth, customLabelWidth);
        }

        if (!railKnown)
        {
            railKnown = pendingMarkers == 0;
            if (!railKnown)
                railWidth = Math.Max(railWidth, markerSize);
        }

        return new Frame(
            showLabel,
            showContent,
            labelsKnown,
            labelWidth,
            _labelWidth != null,
            labelCap,
            railKnown,
            railWidth,
            Math.Max(0, _labelGap),
            Math.Max(0, _contentGap),
            Math.Max(0, _stepSpacing),
            Math.Max(0, _groupSpacing),
            Math.Max(0, _headerGap),
            markerSize,
            Math.Max(0, _markerStrokeThickness),
            Math.Max(0, _lineThickness),
            Math.Max(0, _dashLength),
            Math.Max(0, _dashGap));
    }

    IEnumerable<(int Block, IReadOnlyList<TimelineStep> Steps)> Enumerate()
    {
        for (var i = 0; i < _steps.Blocks.Count; i++)
        {
            var block = _steps.Blocks[i];
            if (block is TimelineSteps.Run run)
                yield return (i, run.Steps);
            else
                yield return (i, ((TimelineSteps.Grouped)block).Group.Steps);
        }
    }

    VisualNode Watch(VisualNode node, string key)
    {
        if (node is MauiReactor.IVisualElement visual)
        {
            visual.OnSizeChanged((sender, _) => Note(key, sender));
            return node;
        }

        return new MauiReactor.Grid(node)
            .OnSizeChanged((sender, _) => Note(key, sender));
    }

    void Note(string key, object? sender)
    {
        if (_sizes.Reading || sender is not Microsoft.Maui.Controls.VisualElement element)
            return;

        var width = element.Width;
        var height = element.Height;
        if (element.Width > 0 && IsTextSlot(key))
        {
            _sizes.Reading = true;
            var desired = element.Measure(element.Width, double.PositiveInfinity);
            _sizes.Reading = false;
            if (IsReal(desired.Height) && desired.Height > height)
                height = desired.Height;
        }

        if (!IsReal(width) || !IsReal(height) || (width <= 0 && height <= 0))
            return;
        if (!_sizes.Set(key, width, height))
            return;

        var sizes = _sizes;
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null)
        {
            sizes.Refresh?.Invoke();
            return;
        }

        if (sizes.Pending)
            return;
        sizes.Pending = true;
        dispatcher.Dispatch(() =>
        {
            sizes.Pending = false;
            sizes.Refresh?.Invoke();
        });
    }

    bool TrySize(string key, out double width, out double height)
        => _sizes.TryGet(key, out width, out height);

    static VisualNode Cell(VisualNode node, int column, int row, int rowSpan = 1)
    {
        var host = ViewOf(node);
        Place(host, column, row, rowSpan);
        return host;
    }

    static void At(VisualNode node, int column, int row, int rowSpan)
        => Place(ViewOf(node), column, row, rowSpan);

    static void Place(VisualNode node, int column, int row, int rowSpan)
    {
        if (node is not MauiReactor.IView view)
            return;

        view.GridColumn(column);
        var visual = (VisualNode)view;
        visual.GridRow(row);
        if (rowSpan > 1)
            visual.GridRowSpan(rowSpan);
    }

    static VisualNode Start(VisualNode node, double top)
    {
        var host = ViewOf(node);
        if (host is MauiReactor.IView view)
        {
            view.VStart();
            if (top > 0)
                view.Margin(0, top, 0, 0);
        }

        return host;
    }

    static VisualNode FillWidth(VisualNode node)
    {
        var host = ViewOf(node);
        if (host is MauiReactor.IView view)
        {
            view.HFill();
        }

        return host;
    }

    static VisualNode MarkerHost(VisualNode node, double top)
    {
        var host = ViewOf(node);
        if (host is MauiReactor.IView view)
        {
            view.HCenter();
            view.VStart();
            if (top > 0)
                view.Margin(0, top, 0, 0);
        }

        if (host is MauiReactor.IVisualElement element)
        {
            element.InputTransparent(true);
            element.ZIndex(2);
        }

        return host;
    }

    static VisualNode ViewOf(VisualNode node)
        => node is MauiReactor.IView ? node : new MauiReactor.Grid(node);

    static MauiReactor.BoxView Spacer(double height)
        => new MauiReactor.BoxView()
            .HeightRequest(Math.Max(0, height))
            .Color(Colors.Transparent)
            .InputTransparent(inputTransparent: true);

    static string Slot(int block, int step, string slot)
        => string.Create(CultureInfo.InvariantCulture, $"{block}:{step}:{slot}");

    static bool IsTextSlot(string key)
        => key.EndsWith(":content", StringComparison.Ordinal)
        || key.EndsWith(":label", StringComparison.Ordinal)
        || key.EndsWith(":header", StringComparison.Ordinal);

    static double TextSize(double size)
        => Math.Max(0, size);

    static string Format(double value)
        => Math.Max(0, value).ToString("0.###", CultureInfo.InvariantCulture);

    static bool IsReal(double value)
        => !double.IsNaN(value) && !double.IsInfinity(value);

    internal enum TimelineLineKind
    {
        Solid,
        Dashed,
        None,
    }

    internal enum TimelineMarkerKind
    {
        Filled,
        Ring,
        None,
        Custom,
    }

    enum TimelineLabelSide
    {
        Leading,
        Trailing,
        Inside,
    }

    [StructLayout(LayoutKind.Auto)]
    readonly record struct Frame(
        bool ShowLabel,
        bool ShowContent,
        bool LabelKnown,
        double LabelWidth,
        bool LabelFixed,
        double LabelCap,
        bool RailKnown,
        double RailWidth,
        double LabelGap,
        double ContentGap,
        double StepSpacing,
        double GroupSpacing,
        double HeaderGap,
        double MarkerSize,
        double MarkerStroke,
        double LineThickness,
        double DashLength,
        double DashGap);

    [StructLayout(LayoutKind.Auto)]
    readonly record struct ColumnMap(string Spec, int? Label, int? Rail, int? Content);

    [StructLayout(LayoutKind.Auto)]
    readonly record struct MarkerPoint(double Center, double Radius, double Head, double LabelTop);

    [StructLayout(LayoutKind.Auto)]
    readonly record struct SegmentStyle(Color Color, double Thickness, bool Dashed);

    sealed class Sizes
    {
        readonly Dictionary<string, (double Width, double Height)> _entries = [];

        public Action? Refresh { get; set; }

        public bool Pending { get; set; }

        public bool Reading { get; set; }

        public IReadOnlyDictionary<string, (double Width, double Height)> Entries => _entries;

        public bool Set(string key, double width, double height)
        {
            if (_entries.TryGetValue(key, out var old)
                && Math.Abs(old.Width - width) < SizeSlack
                && Math.Abs(old.Height - height) < SizeSlack)
            {
                return false;
            }

            _entries[key] = (width, height);
            return true;
        }

        public bool TryGet(string key, out double width, out double height)
        {
            if (_entries.TryGetValue(key, out var size))
            {
                width = size.Width;
                height = size.Height;
                return true;
            }

            width = 0;
            height = 0;
            return false;
        }

        public void CopyFrom(Sizes older)
        {
            foreach (var (key, size) in older._entries)
                _entries[key] = size;
            Pending = older.Pending;
        }
    }
}
