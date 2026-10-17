using MauiReactor;
using MauiBorder = Microsoft.Maui.Controls.Border;
using MauiContentView = Microsoft.Maui.Controls.ContentView;
using MauiLayout = Microsoft.Maui.Controls.Layout;
using MauiScrollView = Microsoft.Maui.Controls.ScrollView;

namespace Reactor.Kit.Internals.Sheet;

/// <summary>Mounts the sheet body without becoming a child of the page layout.</summary>
sealed class SheetPanelHost : MauiReactor.ContentView
{
    VisualNode? _root;

    public View? Element => NativeControl;

    protected override void OnMount()
    {
        base.OnMount();
        if (NativeControl != null)
            NativeControl.SafeAreaEdges = SafeAreaEdges.None;
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();
        NativeControl?.Dispatcher.Dispatch(ReleaseSafeArea);
    }

    protected override void OnAddChild(VisualNode widget, BindableObject childControl)
    {
        base.OnAddChild(widget, childControl);
        NativeControl?.Dispatcher.Dispatch(ReleaseSafeArea);
    }

    /// <summary>
    /// A layout defaults to the container safe area, so the home indicator
    /// stays as a gap after the caller sets bottom padding to zero. The sheet body fills the card.
    /// </summary>
    void ReleaseSafeArea()
    {
        if (NativeControl != null)
            NativeControl.SafeAreaEdges = SafeAreaEdges.None;

        Release(NativeControl?.Content);
    }

    static void Release(Element? element)
    {
        if (element == null)
            return;

        Clear(element);

        if (element is MauiScrollView scroll)
            Release(scroll.Content);

        if (element is MauiContentView content)
            Release(content.Content);

        if (element is MauiBorder border)
            Release(border.Content);

        if (element is not MauiLayout layout)
            return;

        foreach (var child in layout.Children)
        {
            if (child is Element nested)
                Release(nested);
        }
    }

    static void Clear(BindableObject element)
    {
        switch (element)
        {
            case MauiLayout layout when !layout.IsSet(MauiLayout.SafeAreaEdgesProperty):
                layout.SafeAreaEdges = SafeAreaEdges.None;
                break;
            case MauiContentView view when !view.IsSet(MauiContentView.SafeAreaEdgesProperty):
                view.SafeAreaEdges = SafeAreaEdges.None;
                break;
            case MauiScrollView scroll when !scroll.IsSet(MauiScrollView.SafeAreaEdgesProperty):
                scroll.SafeAreaEdges = SafeAreaEdges.None;
                break;
            case MauiBorder border when !border.IsSet(MauiBorder.SafeAreaEdgesProperty):
                border.SafeAreaEdges = SafeAreaEdges.None;
                break;
        }
    }

    public VisualNode? Root
    {
        get => _root;
        set
        {
            if (ReferenceEquals(_root, value))
                return;

            _root = value;
            if (_isMounted)
                Invalidate();
        }
    }

    protected override IEnumerable<VisualNode> RenderChildren()
    {
        if (_root != null)
            yield return _root;
    }
}
