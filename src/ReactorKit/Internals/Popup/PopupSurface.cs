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
        nameof(Alignment), typeof(MenuAlignment), typeof(PopupSurface), MenuAlignment.Trailing, propertyChanged: OnAlignmentChanged);

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

    static void OnAlignmentChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is PopupSurface surface)
            surface.Session.Relayout();
    }
}
