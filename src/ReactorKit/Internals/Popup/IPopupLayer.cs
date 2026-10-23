namespace Reactor.Kit.Internals.Popup;

/// <summary>
/// Inserts the layer into the view that holds the anchor and reports rectangles in that view's coordinates.
/// A normal page uses the page. A presented sheet uses the sheet, so the page underneath does not cover the panel.
/// </summary>
internal interface IPopupLayer
{
    void Insert(Page page, View overlay, VisualElement anchor);

    void Remove(View overlay);

    void Sync(Page page, View overlay);

    /// <summary>
    /// Clips <paramref name="view"/> to its bounds, or lets a shadow draw outside them.
    /// </summary>
    void SetClip(View view, bool clip);

    /// <summary>
    /// Lets a shadow on <paramref name="panel"/> paint outside the views above it.
    /// </summary>
    void ReleaseShadowClip(View panel);

    Rect PageBounds(Page page, VisualElement anchor);

    Rect AnchorBounds(VisualElement anchor, Page page);

    Thickness SafeInsets(Page page, VisualElement anchor);

    void CaptureBack(Action onBack);

    void ReleaseBack();

    void CaptureEscape(Action onEscape);

    void ReleaseEscape();
}
