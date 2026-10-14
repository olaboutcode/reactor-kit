using MauiReactor;

namespace Reactor.Kit.PopupMenuTokens;

/// <summary>
/// One row in a pop-up menu. The button shows <see cref="Title"/>.
/// <see cref="Content"/>, when set, is the row instead of that title.
/// </summary>
public sealed record PopupMenuItem
{
    public PopupMenuItem(string id, string title, bool enabled = true)
        : this(id, title, content: null, enabled)
    {
    }

    public PopupMenuItem(string id, string title, VisualNode? content, bool enabled = true)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(title);
        Id = id;
        Title = title;
        Content = content;
        Enabled = enabled;
    }

    public string Id { get; }

    public string Title { get; }

    public VisualNode? Content { get; }

    public bool Enabled { get; }
}
