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
