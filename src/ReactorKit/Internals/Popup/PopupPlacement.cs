using Reactor.Kit.PopupMenuTokens;

namespace Reactor.Kit.Internals.Popup;

/// <summary>
/// Decides the panel rectangle. Platforms only supply the anchor and the safe area.
/// </summary>
static class PopupPlacement
{
    public const double Gap = 8;
    /// <summary>
    /// Default smallest panel width. Zero leaves the measured content width.
    /// </summary>
    public const double MinWidth = 220;
    public const double MinHeight = 44;
    public const double OpenScale = 0.96;
    public const uint Duration = 180;

    public static PopupFrame Place(
        Rect anchor,
        Size desired,
        Rect window,
        Thickness safe,
        MenuAlignment alignment,
        double gap,
        double minWidth,
        double minHeight)
    {
        gap = AtLeastZero(gap);
        minWidth = AtLeastZero(minWidth);
        minHeight = AtLeastZero(minHeight);

        var left = window.X + safe.Left + gap;
        var top = window.Y + safe.Top + gap;
        var right = window.Right - safe.Right - gap;
        var bottom = window.Bottom - safe.Bottom - gap;
        if (right < left)
            right = left;
        if (bottom < top)
            bottom = top;

        // MinWidth raises the measured width. Zero keeps that measurement, so the panel hugs the content.
        // A window narrower than the floor still caps the width.
        var maxWidth = Math.Max(0, right - left);
        var floor = Math.Min(minWidth, maxWidth);
        var width = Math.Min(Math.Max(desired.Width, floor), maxWidth);

        var spaceBelow = bottom - (anchor.Bottom + gap);
        var spaceAbove = anchor.Top - gap - top;
        var above = desired.Height > spaceBelow && spaceAbove > spaceBelow;
        var available = Math.Max(0, above ? spaceAbove : spaceBelow);
        var height = Math.Min(Math.Max(desired.Height, minHeight), available);

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

        var y = above ? anchor.Top - gap - height : anchor.Bottom + gap;
        if (y < top)
            y = top;
        if (y + height > bottom)
            y = Math.Max(top, bottom - height);

        var visible = anchor.Width > 0 && anchor.Height > 0 && Intersects(anchor, window);
        var scrolls = desired.Height > height + 0.5;
        return new PopupFrame(new Rect(x, y, width, height), above, visible, scrolls);
    }

    internal static double AtLeastZero(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
            return 0;

        return value;
    }

    static bool Intersects(Rect anchor, Rect window)
        => anchor.X < window.Right
            && anchor.Right > window.X
            && anchor.Y < window.Bottom
            && anchor.Bottom > window.Y;
}
