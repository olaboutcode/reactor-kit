using MauiReactor;

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
