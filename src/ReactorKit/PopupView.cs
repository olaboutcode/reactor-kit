using MauiReactor;
using Reactor.Kit.Internals.Popup;
using Reactor.Kit.PopupMenuTokens;
using MauiContentView = Microsoft.Maui.Controls.ContentView;
using MauiView = Microsoft.Maui.Controls.View;

namespace Reactor.Kit;

/// <summary>
/// Anchors a floating panel to a child. The page owns <see cref="IsOpen(bool)"/>.
/// A tap on the anchor does not open the panel. The panel can hold any content.
/// </summary>
public sealed class PopupView : View<MauiContentView>
{
    readonly PopupPanelHost _panel = new();
    Action? _onDismiss;

    internal PopupView(Action<MauiContentView?>? componentRefAction = null)
        : base(componentRefAction)
    {
    }

    /// <summary>
    /// The floating content. An empty panel still opens.
    /// </summary>
    public PopupView Content(VisualNode? content)
    {
        _panel.Root = content;
        return this;
    }

    /// <summary>
    /// The page sets this. The anchor's own tap does not.
    /// </summary>
    public PopupView IsOpen(bool open)
        => this.Set(PopupSurface.IsOpenProperty, open);

    /// <inheritdoc cref="IsOpen(bool)"/>
    public PopupView IsOpen(Func<bool> open, IComponentWithState? state = null)
        => this.Set(PopupSurface.IsOpenProperty, new PropertyValue<bool>(open, state));

    /// <summary>
    /// Which edge of the anchor the panel lines up with. The default is trailing.
    /// </summary>
    public PopupView Alignment(MenuAlignment alignment)
        => this.Set(PopupSurface.AlignmentProperty, alignment);

    /// <inheritdoc cref="Alignment(MenuAlignment)"/>
    public PopupView Alignment(Func<MenuAlignment> alignment, IComponentWithState? state = null)
        => this.Set(PopupSurface.AlignmentProperty, new PropertyValue<MenuAlignment>(alignment, state));

    /// <summary>
    /// Space between the anchor and the panel, in points. The same inset keeps the panel inside the safe area. The default is 8.
    /// </summary>
    public PopupView Gap(double gap)
        => this.Set(PopupSurface.GapProperty, gap);

    /// <inheritdoc cref="Gap(double)"/>
    public PopupView Gap(Func<double> gap, IComponentWithState? state = null)
        => this.Set(PopupSurface.GapProperty, new PropertyValue<double>(gap, state));

    /// <summary>
    /// Smallest panel width, in points. The default is 220. A narrower window uses the space that remains.
    /// </summary>
    public PopupView MinWidth(double width)
        => this.Set(PopupSurface.MinWidthProperty, width);

    /// <inheritdoc cref="MinWidth(double)"/>
    public PopupView MinWidth(Func<double> width, IComponentWithState? state = null)
        => this.Set(PopupSurface.MinWidthProperty, new PropertyValue<double>(width, state));

    /// <summary>
    /// Smallest panel height, in points. The default is 44.
    /// </summary>
    public PopupView MinHeight(double height)
        => this.Set(PopupSurface.MinHeightProperty, height);

    /// <inheritdoc cref="MinHeight(double)"/>
    public PopupView MinHeight(Func<double> height, IComponentWithState? state = null)
        => this.Set(PopupSurface.MinHeightProperty, new PropertyValue<double>(height, state));

    /// <summary>
    /// Horizontal scale the panel starts from when it opens, and returns to when it closes. The default is 0.96.
    /// </summary>
    public PopupView OpenScaleX(double scale)
        => this.Set(PopupSurface.OpenScaleXProperty, scale);

    /// <inheritdoc cref="OpenScaleX(double)"/>
    public PopupView OpenScaleX(Func<double> scale, IComponentWithState? state = null)
        => this.Set(PopupSurface.OpenScaleXProperty, new PropertyValue<double>(scale, state));

    /// <summary>
    /// Vertical scale the panel starts from when it opens, and returns to when it closes. The default is 0.96.
    /// </summary>
    public PopupView OpenScaleY(double scale)
        => this.Set(PopupSurface.OpenScaleYProperty, scale);

    /// <inheritdoc cref="OpenScaleY(double)"/>
    public PopupView OpenScaleY(Func<double> scale, IComponentWithState? state = null)
        => this.Set(PopupSurface.OpenScaleYProperty, new PropertyValue<double>(scale, state));

    /// <summary>
    /// Sets <see cref="OpenScaleX(double)"/> and <see cref="OpenScaleY(double)"/> to the same scale. The default is 0.96.
    /// </summary>
    public PopupView OpenScale(double scale)
        => OpenScaleX(scale).OpenScaleY(scale);

    /// <inheritdoc cref="OpenScale(double)"/>
    public PopupView OpenScale(Func<double> scale, IComponentWithState? state = null)
        => OpenScaleX(scale, state).OpenScaleY(scale, state);

    /// <summary>
    /// Curve for the fade and both scales. The default is <see cref="Easing.CubicOut"/>.
    /// </summary>
    public PopupView Easing(Easing easing)
        => this.Set(PopupSurface.EasingProperty, easing);

    /// <inheritdoc cref="Easing(Easing)"/>
    public PopupView Easing(Func<Easing> easing, IComponentWithState? state = null)
        => this.Set(PopupSurface.EasingProperty, new PropertyValue<Easing>(easing, state));

    /// <summary>
    /// Open and close animation length, in milliseconds. The default is 180.
    /// </summary>
    public PopupView Duration(uint milliseconds)
        => this.Set(PopupSurface.DurationProperty, milliseconds);

    /// <inheritdoc cref="Duration(uint)"/>
    public PopupView Duration(Func<uint> milliseconds, IComponentWithState? state = null)
        => this.Set(PopupSurface.DurationProperty, new PropertyValue<uint>(milliseconds, state));

    /// <summary>
    /// Runs once when an outside tap, a second tap on the anchor, or the platform back or Escape key closes the panel.
    /// The kit does not change the page state.
    /// </summary>
    public PopupView OnDismiss(Action? callback)
    {
        _onDismiss = callback;
        return this;
    }

    protected override void OnMount()
    {
        _nativeControl ??= new PopupSurface();
        base.OnMount();
    }

    protected override void OnUnmount()
    {
        if (NativeControl is PopupSurface surface)
            surface.Close();

        base.OnUnmount();
    }

    protected override IEnumerable<VisualNode> RenderChildren()
    {
        yield return _panel;

        foreach (var child in base.RenderChildren())
            yield return child;
    }

    protected override void OnAddChild(VisualNode widget, BindableObject childControl)
    {
        if (widget is PopupPanelHost)
        {
            if (childControl is MauiContentView panel && NativeControl is PopupSurface surface)
                surface.AttachPanel(panel);

            return;
        }

        if (childControl is MauiView view && NativeControl != null)
            NativeControl.Content = view;

        base.OnAddChild(widget, childControl);
    }

    protected override void OnRemoveChild(VisualNode widget, BindableObject childControl)
    {
        if (widget is PopupPanelHost)
        {
            if (NativeControl is PopupSurface surface)
                surface.DetachPanel(childControl as MauiContentView);

            return;
        }

        if (childControl is MauiView && NativeControl != null)
            NativeControl.Content = null;

        base.OnRemoveChild(widget, childControl);
    }

    protected override void OnAttachNativeEvents()
    {
        if (NativeControl is PopupSurface surface)
        {
            surface.Dismissed -= OnNativeDismissed;
            if (_onDismiss != null)
                surface.Dismissed += OnNativeDismissed;
        }

        base.OnAttachNativeEvents();
    }

    protected override void OnDetachNativeEvents()
    {
        if (NativeControl is PopupSurface surface)
            surface.Dismissed -= OnNativeDismissed;

        base.OnDetachNativeEvents();
    }

    void OnNativeDismissed(object? sender, EventArgs e)
        => _onDismiss?.Invoke();
}
