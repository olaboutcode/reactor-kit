using MauiReactor;

namespace Reactor.Kit.PopupMenuTokens;

/// <summary>
/// One row in a pop-up menu. A string is the title. A view is the row itself.
/// </summary>
public sealed class PopupMenuItem
{
    public PopupMenuItem(string text, Action<string>? onTap = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        Text = text;
        TextTap = onTap;
    }

    public PopupMenuItem(VisualNode content, Action? onTap = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        Content = content;
        Tap = onTap;
    }

    PopupMenuItem(bool isRule)
    {
        IsRule = isRule;
    }

    /// <summary>
    /// A hairline between rows.
    /// </summary>
    public static PopupMenuItem Rule()
        => new(isRule: true);

    public string? Text { get; }

    public VisualNode? Content { get; }

    public Action<string>? TextTap { get; }

    public Action? Tap { get; }

    public bool IsRule { get; }
}
