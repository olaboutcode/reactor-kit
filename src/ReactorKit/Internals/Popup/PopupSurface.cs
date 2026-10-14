using Reactor.Kit.PopupMenuTokens;

namespace Reactor.Kit.Internals.Popup;

/// <summary>
/// The anchor host. The floating panel is presented by <see cref="PopupSession"/>.
/// </summary>
internal sealed class PopupSurface : ContentView
{
    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(
        nameof(IsOpen), typeof(bool), typeof(PopupSurface), false, propertyChanged: OnOpenChanged);

    public static readonly BindableProperty AlignmentProperty = BindableProperty.Create(
        nameof(Alignment), typeof(MenuAlignment), typeof(PopupSurface), MenuAlignment.Trailing, propertyChanged: OnMetricsChanged);

    public static readonly BindableProperty GapProperty = BindableProperty.Create(
        nameof(Gap), typeof(double), typeof(PopupSurface), PopupPlacement.Gap, propertyChanged: OnMetricsChanged);

    public static readonly BindableProperty MinWidthProperty = BindableProperty.Create(
        nameof(MinWidth), typeof(double), typeof(PopupSurface), PopupPlacement.MinWidth, propertyChanged: OnMetricsChanged);

    public static readonly BindableProperty MinHeightProperty = BindableProperty.Create(
        nameof(MinHeight), typeof(double), typeof(PopupSurface), PopupPlacement.MinHeight, propertyChanged: OnMetricsChanged);

    public static readonly BindableProperty OpenScaleProperty = BindableProperty.Create(
        nameof(OpenScale), typeof(double), typeof(PopupSurface), PopupPlacement.OpenScale, propertyChanged: OnMetricsChanged);

    public static readonly BindableProperty DurationProperty = BindableProperty.Create(
        nameof(Duration), typeof(uint), typeof(PopupSurface), PopupPlacement.Duration, propertyChanged: OnMetricsChanged);

    public PopupSurface()
    {
        BackgroundColor = Colors.Transparent;
        Session = new PopupSession(this);
    }

    internal PopupSession Session { get; }

    internal event EventHandler? Dismissed;

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public MenuAlignment Alignment
    {
        get => (MenuAlignment)GetValue(AlignmentProperty);
        set => SetValue(AlignmentProperty, value);
    }

    public double Gap
    {
        get => (double)GetValue(GapProperty);
        set => SetValue(GapProperty, value);
    }

    public double MinWidth
    {
        get => (double)GetValue(MinWidthProperty);
        set => SetValue(MinWidthProperty, value);
    }

    public double MinHeight
    {
        get => (double)GetValue(MinHeightProperty);
        set => SetValue(MinHeightProperty, value);
    }

    public double OpenScale
    {
        get => (double)GetValue(OpenScaleProperty);
        set => SetValue(OpenScaleProperty, value);
    }

    public uint Duration
    {
        get => (uint)GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    internal void AttachPanel(ContentView panel)
        => Session.AttachPanel(panel);

    internal void DetachPanel(ContentView? panel)
        => Session.DetachPanel(panel);

    internal void Close()
        => Session.Close();

    internal void RaiseDismissed()
        => Dismissed?.Invoke(this, EventArgs.Empty);

    static void OnOpenChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is PopupSurface surface)
            surface.Session.SetOpen((bool)newValue);
    }

    static void OnMetricsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is PopupSurface surface)
            surface.Session.Relayout();
    }
}
