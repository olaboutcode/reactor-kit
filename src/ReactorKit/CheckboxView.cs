using System.Globalization;
using MauiReactor;
using MauiReactor.Shapes;

namespace Reactor.Kit;

/// <summary>
/// One checkbox. Size, corner radius, fill, stroke, and the check are properties,
/// so the same view can take a Material, macOS, or Fluent look.
/// The page owns <see cref="IsChecked"/>.
/// </summary>
public sealed partial class CheckboxView : Component
{
    const double Hit = 44;

    [Prop] private bool _isChecked;
    [Prop] private Action<bool>? _onChanged;
    [Prop] private bool _isEnabled = true;
    [Prop] private double _disabledOpacity = 0.38;
    [Prop] private VisualNode? _label;
    [Prop] private double _labelSpacing = 12;
    CheckboxLabelSide _labelSide = CheckboxLabelSide.Leading;
    [Prop] private double _boxSize = 20;
    [Prop] private double _boxCornerRadius = 4;
    [Prop] private Color _boxOnColor = Color.FromArgb("#1C1C1E");
    [Prop] private Color _boxOffColor = Colors.Transparent;
    [Prop] private Color _boxOnStroke = Color.FromArgb("#1C1C1E");
    [Prop] private Color _boxOffStroke = Color.FromArgb("#C6C6C8");
    [Prop] private double _boxOnStrokeThickness;
    [Prop] private double _boxOffStrokeThickness = 1.5;
    [Prop] private Color _checkColor = Color.FromArgb("#FFFFFF");
    [Prop] private double _checkThickness = 2;
    [Prop] private VisualNode? _checkedContent;
    [Prop] private uint _duration = 200;
    [Prop] private Easing _easing = Microsoft.Maui.Easing.CubicOut;

    Motion _motion = new();

    internal CheckboxView()
    {
    }

    /// <summary>
    /// Puts the text in a 17pt label and leaves the text color to the theme.
    /// A view passed to the other overload replaces it.
    /// </summary>
    public CheckboxView Label(string text)
    {
        _label = new MauiReactor.Label(text).FontSize(17);
        return this;
    }

    /// <summary>
    /// Puts the label at the start and the box at the end. This is the default.
    /// A later call replaces an earlier one.
    /// </summary>
    public CheckboxView LabelLeading()
    {
        _labelSide = CheckboxLabelSide.Leading;
        return this;
    }

    /// <summary>
    /// Puts the box at the start and the label at the end.
    /// A later call replaces an earlier one.
    /// </summary>
    public CheckboxView LabelTrailing()
    {
        _labelSide = CheckboxLabelSide.Trailing;
        return this;
    }

    public override VisualNode Render()
    {
        _motion.Refresh = Invalidate;
        Step();

        var frame = Measure();
        var box = HitBox(frame);
        var row = _label == null
            ? Grid(box)
            : _labelSide == CheckboxLabelSide.Trailing
                ? Grid("*", "auto,*",
                    box.GridColumn(0).VCenter(),
                    InColumn(_label, 1))
                    .ColumnSpacing(_labelSpacing)
                : Grid("*", "*,auto",
                    InColumn(_label, 0),
                    box.GridColumn(1).VCenter())
                    .ColumnSpacing(_labelSpacing);

        return row.Opacity(_isEnabled ? 1 : _disabledOpacity);
    }

    protected override void MergeWith(VisualNode newNode)
    {
        if (newNode is CheckboxView next)
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

    MauiReactor.Grid HitBox(Frame frame)
    {
        var hit = Grid(
                Square(frame)
                    .HCenter()
                    .VCenter()
                    .InputTransparent(true),
                Mark(frame)
                    .HCenter()
                    .VCenter()
                    .InputTransparent(true))
            .WidthRequest(Math.Max(Hit, frame.Size))
            .HeightRequest(Math.Max(Hit, frame.Size));

        if (!_isEnabled)
            return hit.InputTransparent(true);

        return hit.OnTapped(OnTap);
    }

    static MauiReactor.Border Square(Frame frame)
        => new MauiReactor.Border()
            .BackgroundColor(frame.Fill)
            .Stroke(frame.StrokeThickness <= 0 ? Colors.Transparent : frame.Stroke)
            .StrokeThickness(Math.Max(0, frame.StrokeThickness))
            .StrokeCornerRadius(frame.Radius)
            .WidthRequest(frame.Size)
            .HeightRequest(frame.Size)
            .InputTransparent(true);

    MauiReactor.Grid Mark(Frame frame)
    {
        var face = Grid()
            .WidthRequest(frame.Size)
            .HeightRequest(frame.Size);

        if (frame.Size > 0)
        {
            face.Clip(
                new RoundRectangleGeometry()
                    .Rect(new Microsoft.Maui.Graphics.Rect(0, 0, frame.Size, frame.Size))
                    .CornerRadius(frame.Radius));
        }

        if (frame.Progress <= 0.01)
            return face;

        if (_checkedContent != null)
            face.Add(Grid(Centered(_checkedContent)).Opacity(frame.Progress));
        else
            face.Add(Check(frame).Opacity(frame.Progress));

        return face;
    }

    MauiReactor.Shapes.Path Check(Frame frame)
    {
        var size = frame.Size;
        var x1 = size * 0.22;
        var y1 = size * 0.52;
        var x2 = size * 0.42;
        var y2 = size * 0.72;
        var x3 = size * 0.78;
        var y3 = size * 0.30;
        var data = string.Create(
            CultureInfo.InvariantCulture,
            $"M{x1:0.###},{y1:0.###} L{x2:0.###},{y2:0.###} L{x3:0.###},{y3:0.###}");

        return new MauiReactor.Shapes.Path()
            .Data(data)
            .Stroke(_checkColor)
            .StrokeThickness(Math.Max(0, _checkThickness))
            .StrokeLineCap(Microsoft.Maui.Controls.Shapes.PenLineCap.Round)
            .StrokeLineJoin(Microsoft.Maui.Controls.Shapes.PenLineJoin.Round)
            .Aspect(Stretch.None)
            .WidthRequest(size)
            .HeightRequest(size)
            .BackgroundColor(Colors.Transparent)
            .InputTransparent(true);
    }

    static VisualNode InColumn(VisualNode node, int column)
        => node is MauiReactor.IView view
            ? (VisualNode)view.GridColumn(column).VCenter()
            : Grid(node).GridColumn(column).VCenter();

    static VisualNode Centered(VisualNode node)
        => node is MauiReactor.IView view
            ? (VisualNode)view.HCenter().VCenter()
            : Grid(node).HCenter().VCenter();

    Frame Measure()
    {
        var progress = Math.Clamp(_motion.Progress, 0, 1);
        var size = Math.Max(0, _boxSize);
        var radius = size <= 0 ? 0 : Math.Clamp(_boxCornerRadius, 0, size / 2);

        return new Frame(
            progress,
            size,
            radius,
            Mix(_boxOffColor, _boxOnColor, progress),
            Mix(_boxOffStroke, _boxOnStroke, progress),
            Lerp(_boxOffStrokeThickness, _boxOnStrokeThickness, progress));
    }

    void Step()
    {
        var target = _isChecked ? 1d : 0d;
        if (!_motion.Ready)
        {
            _motion.Ready = true;
            _motion.Progress = target;
            return;
        }

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
            return;
        }

        var eased = _easing?.Ease(elapsed) ?? elapsed;
        _motion.Progress = _motion.AnimFrom + ((_motion.AnimTo - _motion.AnimFrom) * eased);
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
        if (!_isEnabled)
            return;

        Commit(!_isChecked);
    }

    void Commit(bool on)
    {
        if (on == _isChecked)
            return;

        _onChanged?.Invoke(on);
        // The page owns IsChecked. If it ignores the callback, draw the check back.
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

    enum CheckboxLabelSide
    {
        Leading,
        Trailing,
    }

    sealed class Motion
    {
        public double Progress { get; set; }

        public bool Ready { get; set; }

        public bool Animating { get; set; }

        public long AnimStart { get; set; }

        public double AnimFrom { get; set; }

        public double AnimTo { get; set; }

        public bool Ticking { get; set; }

        public Action? Refresh { get; set; }
    }

    readonly record struct Frame(
        double Progress,
        double Size,
        double Radius,
        Color Fill,
        Color Stroke,
        double StrokeThickness);
}
