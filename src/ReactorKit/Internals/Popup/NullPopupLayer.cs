#if !ANDROID && !IOS && !MACCATALYST && !WINDOWS
namespace Reactor.Kit.Internals.Popup;

/// <summary>
/// Keeps the shared target building. Device targets use a platform layer.
/// </summary>
sealed class NullPopupLayer : IPopupLayer
{
    public void Insert(Page page, View overlay)
    {
    }

    public void Remove(View overlay)
    {
    }

    public void Sync(Page page, View overlay)
    {
    }

    public void SetClip(View view, bool clip)
    {
    }

    public void ReleaseShadowClip(View panel)
    {
    }

    public Rect PageBounds(Page page)
        => new(0, 0, Math.Max(0, page.Width), Math.Max(0, page.Height));

    public Rect AnchorBounds(VisualElement anchor, Page page)
    {
        double x = 0;
        double y = 0;
        Element? current = anchor;
        while (current is VisualElement visual && current != page)
        {
            x += visual.X + visual.TranslationX;
            y += visual.Y + visual.TranslationY;
            if (visual.Parent is ScrollView scroll)
            {
                x -= scroll.ScrollX;
                y -= scroll.ScrollY;
            }

            current = visual.Parent;
        }

        return new Rect(x, y, anchor.Width, anchor.Height);
    }

    public Thickness SafeInsets(Page page)
        => page.Padding;

    public void CaptureBack(Action onBack)
    {
    }

    public void ReleaseBack()
    {
    }

    public void CaptureEscape(Action onEscape)
    {
    }

    public void ReleaseEscape()
    {
    }
}
#endif
