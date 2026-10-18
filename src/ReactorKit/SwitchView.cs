using System.Globalization;
using MauiReactor;
using MauiReactor.Shapes;

namespace Reactor.Kit;

/// <summary>
/// One on/off pill. Track, thumb, stroke, hole, and thumb content are properties,
/// so the same view can take a Material, Cupertino, or Fluent look.
/// The page owns <see cref="IsOn"/>.
/// </summary>
public sealed partial class SwitchView : Component
{
    const double Hit = 44;

    [Prop] private bool _isOn;
    [Prop] private Action<bool>? _onChanged;
    [Prop] private bool _isEnabled = true;
    [Prop] private double _disabledOpacity = 0.38;
    [Prop] private VisualNode? _label;
    [Prop] private double _labelSpacing = 12;
    SwitchLabelSide _labelSide = SwitchLabelSide.Leading;
    [Prop] private double _trackWidth = 52;
    [Prop] private double _trackHeight = 32;
    [Prop] private Color _trackOnColor = Color.FromArgb("#1C1C1E");
    [Prop] private Color _trackOffColor = Color.FromArgb("#E9E9EB");
    [Prop] private Color _trackOnStroke = Color.FromArgb("#1C1C1E");
    [Prop] private Color _trackOffStroke = Color.FromArgb("#C6C6C8");
    [Prop] private double _trackOnStrokeThickness;
    [Prop] private double _trackOffStrokeThickness;
    [Prop] private double? _trackCornerRadius;
    [Prop] private double _thumbOnSize = 24;
    [Prop] private double _thumbOffSize = 24;
    [Prop] private double _thumbPressedSize = 24;
    [Prop] private Color _thumbOnColor = Color.FromArgb("#FFFFFF");
    [Prop] private Color _thumbOffColor = Color.FromArgb("#FFFFFF");
    [Prop] private Color _thumbOnStroke = Color.FromArgb("#FFFFFF");
    [Prop] private Color _thumbOffStroke = Color.FromArgb("#C6C6C8");
    [Prop] private double _thumbOnStrokeThickness;
    [Prop] private double _thumbOffStrokeThickness;
    [Prop] private double? _thumbInset;
    [Prop] private double _thumbOnInnerSize;
    [Prop] private double _thumbOffInnerSize;
    [Prop] private Color? _thumbInnerColor;
    [Prop] private VisualNode? _thumbOnContent;
    [Prop] private VisualNode? _thumbOffContent;
    [Prop] private uint _duration = 200;
    [Prop] private Easing _easing = Microsoft.Maui.Easing.CubicOut;

    Motion _motion = new();

    /// <summary>
    /// Puts the text in a 17pt label and leaves the text color to the theme.
    /// A view passed to the other overload replaces it.
    /// </summary>
    public SwitchView Label(string text)
    {
        _label = new MauiReactor.Label(text).FontSize(17);
        return this;
    }

    /// <summary>
    /// Puts the label at the start and the pill at the end. This is the default.
    /// A later call replaces an earlier one.
    /// </summary>
    public SwitchView LabelLeading()
    {
        _labelSide = SwitchLabelSide.Leading;
        return this;
    }

    /// <summary>
    /// Puts the pill at the start and the label at the end.
    /// A later call replaces an earlier one.
    /// </summary>
    public SwitchView LabelTrailing()
    {
        _labelSide = SwitchLabelSide.Trailing;
        return this;
    }

    public override VisualNode Render()
    {
        _motion.Refresh = Invalidate;
        if (!_isEnabled)
        {
            _motion.Dragging = false;
            _motion.Pressed = false;
        }

        Step();

        var frame = Measure();
        var pill = Pill(frame);
        var row = _label == null
            ? Grid(pill)
            : _labelSide == SwitchLabelSide.Trailing
                ? Grid("*", "auto,*",
                    pill.GridColumn(0).VCenter(),
                    InColumn(_label, 1))
                    .ColumnSpacing(_labelSpacing)
                : Grid("*", "*,auto",
                    InColumn(_label, 0),
                    pill.GridColumn(1).VCenter())
                    .ColumnSpacing(_labelSpacing);

        return row.Opacity(_isEnabled ? 1 : _disabledOpacity);
    }

    protected override void MergeWith(VisualNode newNode)
    {
        if (newNode is SwitchView next)
        {
            next._motion = _motion;
            _motion.Refresh = next.Invalidate;
        }

        base.MergeWith(newNode);
    }

    protected override void OnWillUnmount()
    {
        _motion.Refresh = null;
        _motion.Animating = false;
        base.OnWillUnmount();
    }

    MauiReactor.Grid Pill(Frame frame)
    {
        var hit = Grid(
                Disc(_trackWidth, _trackHeight, frame.Corner, frame.Track, frame.TrackStroke, frame.TrackStrokeThickness)
                    .HCenter()
                    .VCenter()
                    .InputTransparent(true),
                Thumb(frame)
                    .HCenter()
                    .VCenter()
                    .InputTransparent(true))
            .WidthRequest(Math.Max(Hit, _trackWidth))
            .HeightRequest(Math.Max(Hit, _trackHeight))
            .OnLoaded(NoteFlow);

        if (!_isEnabled)
            return hit.InputTransparent(true);

        return hit
            .OnTapped(OnTap)
            .OnPanUpdated(OnPan);
    }

    MauiReactor.Grid Thumb(Frame frame)
    {
        var edge = frame.Edge.ToString("0.###", CultureInfo.InvariantCulture);
        return Grid("*", $"{edge},auto,*",
                ThumbFace(frame).GridColumn(1).VCenter())
            .WidthRequest(Math.Max(0, _trackWidth))
            .HeightRequest(Math.Max(0, _trackHeight))
            .ColumnSpacing(0);
    }

    MauiReactor.Grid ThumbFace(Frame frame)
    {
        var face = Grid(
                Disc(frame.Thumb, frame.Thumb, frame.Thumb / 2, frame.ThumbColor, frame.ThumbStroke, frame.ThumbStrokeThickness)
                    .HCenter()
                    .VCenter())
            .WidthRequest(frame.Thumb)
            .HeightRequest(frame.Thumb);

        if (frame.Thumb > 0)
        {
            var radius = frame.Thumb / 2;
            face.Clip(
                new EllipseGeometry()
                    .Center(radius, radius)
                    .RadiusX(radius)
                    .RadiusY(radius));
        }

        if (frame.Inner > 0.5)
        {
            face.Add(
                Disc(frame.Inner, frame.Inner, frame.Inner / 2, frame.InnerColor, Colors.Transparent, 0)
                    .HCenter()
                    .VCenter());
        }

        AddContent(face, _thumbOffContent, 1 - frame.Progress);
        AddContent(face, _thumbOnContent, frame.Progress);
        return face;
    }

    static void AddContent(MauiReactor.Grid face, VisualNode? content, double opacity)
    {
        if (content == null || opacity <= 0.01)
            return;

        face.Add(Grid(Centered(content)).Opacity(opacity));
    }

    static VisualNode InColumn(VisualNode node, int column)
        => node is MauiReactor.IView view
            ? (VisualNode)view.GridColumn(column).VCenter()
            : Grid(node).GridColumn(column).VCenter();

    static VisualNode Centered(VisualNode node)
        => node is MauiReactor.IView view
            ? (VisualNode)view.HCenter().VCenter()
            : Grid(node).HCenter().VCenter();

    static MauiReactor.Border Disc(double width, double height, double radius, Color fill, Color stroke, double thickness)
        => new MauiReactor.Border()
            .BackgroundColor(fill)
            .Stroke(thickness <= 0 ? Colors.Transparent : stroke)
            .StrokeThickness(Math.Max(0, thickness))
            .StrokeCornerRadius(Math.Max(0, radius))
            .WidthRequest(Math.Max(0, width))
            .HeightRequest(Math.Max(0, height))
            .InputTransparent(true);

    Frame Measure()
    {
        var progress = Math.Clamp(_motion.Progress, 0, 1);
        var off = Math.Max(0, _thumbOffSize);
        var on = Math.Max(0, _thumbOnSize);
        var size = _motion.Pressed
            ? Math.Max(0, _thumbPressedSize)
            : Lerp(off, on, progress);
        var inset = _thumbInset ?? Math.Max(0, (_trackHeight - size) / 2);
        var offEdge = inset;
        var onEdge = Math.Max(offEdge, _trackWidth - inset - size);
        var edge = Lerp(offEdge, onEdge, progress);
        var track = Mix(_trackOffColor, _trackOnColor, progress);
        var inner = Lerp(Math.Max(0, _thumbOffInnerSize), Math.Max(0, _thumbOnInnerSize), progress);

        return new Frame(
            progress,
            edge,
            size,
            _trackCornerRadius ?? _trackHeight / 2,
            track,
            Mix(_trackOffStroke, _trackOnStroke, progress),
            Lerp(_trackOffStrokeThickness, _trackOnStrokeThickness, progress),
            Mix(_thumbOffColor, _thumbOnColor, progress),
            Mix(_thumbOffStroke, _thumbOnStroke, progress),
            Lerp(_thumbOffStrokeThickness, _thumbOnStrokeThickness, progress),
            inner,
            _thumbInnerColor ?? track);
    }

    void Step()
    {
        var target = _isOn ? 1d : 0d;
        if (!_motion.Ready)
        {
            _motion.Ready = true;
            _motion.Progress = target;
            return;
        }

        if (_motion.Dragging)
            return;

        if (!_motion.Animating)
        {
            if (Math.Abs(_motion.Progress - target) > 0.001)
                Begin(target);
            return;
        }

        var span = Math.Max(1, _duration);
        var elapsed = (Environment.TickCount64 - _motion.AnimStart) / (double)span;
        if (elapsed >= 1)
        {
            _motion.Progress = _motion.AnimTo;
            _motion.Animating = false;
            _motion.Pressed = false;
            return;
        }

        var eased = _easing?.Ease(elapsed) ?? elapsed;
        _motion.Progress = _motion.AnimFrom + (_motion.AnimTo - _motion.AnimFrom) * eased;
        Kick();
    }

    void Begin(double to)
    {
        _motion.Animating = true;
        _motion.AnimFrom = _motion.Progress;
        _motion.AnimTo = to;
        _motion.AnimStart = Environment.TickCount64;
        Kick();
    }

    void Kick()
    {
        if (_motion.Ticking)
            return;

        _motion.Ticking = true;
        var motion = _motion;
        Application.Current?.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(16), () =>
        {
            motion.Ticking = false;
            if (motion.Animating)
                motion.Refresh?.Invoke();
        });
    }

    void OnTap()
    {
        if (!_isEnabled || _motion.Moved)
        {
            _motion.Moved = false;
            return;
        }

        Commit(!_isOn);
    }

    void OnPan(PanUpdatedEventArgs e)
    {
        if (!_isEnabled)
            return;

        if (e.StatusType == GestureStatus.Started)
        {
            _motion.Dragging = true;
            _motion.Pressed = true;
            _motion.Moved = false;
            _motion.DragOrigin = _motion.Progress;
            _motion.Animating = false;
            Invalidate();
            return;
        }

        if (e.StatusType == GestureStatus.Running)
        {
            var travel = Math.Max(1, Travel());
            var dx = _motion.Rtl ? -e.TotalX : e.TotalX;
            if (Math.Abs(e.TotalX) > 6)
                _motion.Moved = true;

            _motion.Progress = Math.Clamp(_motion.DragOrigin + (dx / travel), 0, 1);
            Invalidate();
            return;
        }

        _motion.Dragging = false;
        _motion.Pressed = false;
        ForgetMove();
        var on = _motion.Progress >= 0.5;
        if (on != _isOn)
            Commit(on);
        else
            Begin(_isOn ? 1 : 0);
    }

    void ForgetMove()
    {
        var motion = _motion;
        Application.Current?.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(50), () => motion.Moved = false);
    }

    double Travel()
    {
        var size = Math.Max(0, Lerp(_thumbOffSize, _thumbOnSize, 1));
        var inset = _thumbInset ?? Math.Max(0, (_trackHeight - size) / 2);
        return Math.Max(1, _trackWidth - (inset * 2) - size);
    }

    void Commit(bool on)
    {
        if (on == _isOn)
            return;

        _onChanged?.Invoke(on);
        // The page owns IsOn. If it ignores the callback, draw the thumb back.
        _motion.Refresh?.Invoke();
    }

    void NoteFlow(object? sender, EventArgs _)
    {
        if (sender is not VisualElement element)
            return;

        var rtl = false;
        var flow = element.FlowDirection;
        var parent = element.Parent as VisualElement;
        while (flow == FlowDirection.MatchParent && parent != null)
        {
            flow = parent.FlowDirection;
            parent = parent.Parent as VisualElement;
        }

        rtl = flow == FlowDirection.RightToLeft;
        if (_motion.Rtl == rtl)
            return;

        _motion.Rtl = rtl;
        _motion.Refresh?.Invoke();
    }

    static double Lerp(double from, double to, double t)
        => from + ((to - from) * Math.Clamp(t, 0, 1));

    static Color Mix(Color from, Color to, double t)
    {
        var u = (float)Math.Clamp(t, 0, 1);
        return new Color(
            from.Red + ((to.Red - from.Red) * u),
            from.Green + ((to.Green - from.Green) * u),
            from.Blue + ((to.Blue - from.Blue) * u),
            from.Alpha + ((to.Alpha - from.Alpha) * u));
    }

    enum SwitchLabelSide
    {
        Leading,
        Trailing,
    }

    sealed class Motion
    {
        public double Progress { get; set; }

        public bool Ready { get; set; }

        public bool Dragging { get; set; }

        public bool Pressed { get; set; }

        public bool Moved { get; set; }

        public double DragOrigin { get; set; }

        public bool Animating { get; set; }

        public long AnimStart { get; set; }

        public double AnimFrom { get; set; }

        public double AnimTo { get; set; }

        public bool Ticking { get; set; }

        public bool Rtl { get; set; }

        public Action? Refresh { get; set; }
    }

    readonly record struct Frame(
        double Progress,
        double Edge,
        double Thumb,
        double Corner,
        Color Track,
        Color TrackStroke,
        double TrackStrokeThickness,
        Color ThumbColor,
        Color ThumbStroke,
        double ThumbStrokeThickness,
        double Inner,
        Color InnerColor);
}
