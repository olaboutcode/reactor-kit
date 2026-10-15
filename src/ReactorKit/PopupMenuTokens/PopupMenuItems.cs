using MauiReactor;

namespace Reactor.Kit.PopupMenuTokens;

/// <summary>
/// The rows of one pop-up menu. Each call returns this list, so the page can chain them.
/// </summary>
public sealed class PopupMenuItems
{
    readonly List<PopupMenuItem> _rows = [];

    /// <summary>
    /// A title row. The tap receives that title.
    /// </summary>
    public PopupMenuItems Item(string text, Action<string>? onTap = null)
    {
        _rows.Add(new PopupMenuItem(text, onTap));
        return this;
    }

    /// <summary>
    /// A row that shows <paramref name="content"/>.
    /// </summary>
    public PopupMenuItems Item(VisualNode content, Action? onTap = null)
    {
        _rows.Add(new PopupMenuItem(content, onTap));
        return this;
    }

    /// <summary>
    /// A hairline.
    /// </summary>
    public PopupMenuItems Rule()
    {
        _rows.Add(PopupMenuItem.Rule());
        return this;
    }

    internal IReadOnlyList<PopupMenuItem> Rows => _rows;
}
