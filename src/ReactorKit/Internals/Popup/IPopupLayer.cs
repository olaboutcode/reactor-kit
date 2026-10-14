namespace Reactor.Kit.Internals.Popup;

/// <summary>
/// Inserts the layer into the current page and reports rectangles in that page's coordinates.
/// </summary>
internal interface IPopupLayer
{
    void Insert(Page page, View overlay);

    void Remove(View overlay);

    void Sync(Page page, View overlay);

    /// <summary>
    /// Clips <paramref name="view"/> to its bounds, or lets a shadow draw outside them.
    /// </summary>
    void SetClip(View view, bool clip);

    Rect PageBounds(Page page);

    Rect AnchorBounds(VisualElement anchor, Page page);

    Thickness SafeInsets(Page page);

    void CaptureBack(Action onBack);

    void ReleaseBack();

    void CaptureEscape(Action onEscape);

    void ReleaseEscape();
}
