using Reactor.Kit.PopupMenuTokens;

namespace Reactor.Kit.Internals.Popup;

/// <summary>
/// Decides the panel rectangle. Platforms only supply the anchor and the safe area.
/// </summary>
static class PopupPlacement
{
    public const double Gap = 4;
    public const double MinWidth = 220;
    public const double MinHeight = 44;
    public const double OpenScale = 0.96;
    public const uint Duration = 180;

    public static PopupFrame Place(Rect anchor, Size desired, Rect window, Thickness safe, MenuAlignment alignment)
    {
        var left = window.X + safe.Left + Gap;
        var top = window.Y + safe.Top + Gap;
        var right = window.Right - safe.Right - Gap;
        var bottom = window.Bottom - safe.Bottom - Gap;
        if (right < left)
            right = left;
        if (bottom < top)
            bottom = top;

        var maxWidth = Math.Max(0, right - left);
        var floor = Math.Min(MinWidth, maxWidth);
        var width = Math.Min(Math.Max(desired.Width, floor), maxWidth);

        var spaceBelow = bottom - (anchor.Bottom + Gap);
        var spaceAbove = anchor.Top - Gap - top;
        var above = desired.Height > spaceBelow && spaceAbove > spaceBelow;
        var available = Math.Max(0, above ? spaceAbove : spaceBelow);
        var height = Math.Min(Math.Max(desired.Height, 0), available);

        var x = alignment switch
        {
            MenuAlignment.Leading => anchor.X,
            MenuAlignment.Center => anchor.Center.X - (width / 2),
            _ => anchor.Right - width,
        };

        if (x < left)
            x = left;
        if (x + width > right)
            x = Math.Max(left, right - width);

        var y = above ? anchor.Top - Gap - height : anchor.Bottom + Gap;
        if (y < top)
            y = top;
        if (y + height > bottom)
            y = Math.Max(top, bottom - height);

        var visible = anchor.Width > 0 && anchor.Height > 0 && Intersects(anchor, window);
        var scrolls = desired.Height > height + 0.5;
        return new PopupFrame(new Rect(x, y, width, height), above, visible, scrolls);
    }

    static bool Intersects(Rect anchor, Rect window)
        => anchor.X < window.Right
            && anchor.Right > window.X
            && anchor.Y < window.Bottom
            && anchor.Bottom > window.Y;
}
