using MauiReactor;

namespace Reactor.Kit.Internals.Popup;

/// <summary>
/// Mounts the floating content without becoming the anchor's child.
/// </summary>
sealed class PopupPanelHost : MauiReactor.ContentView
{
    VisualNode? _root;

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
