namespace Reactor.Kit.Internals.Sheet;

/// <summary>
/// Inserts the sheet for one platform and reports measurements.
/// iOS presents a controller. Android and Windows move a dim and a card.
/// </summary>
interface ISheetLayer
{
    /// <summary>True when this layer moves the card. iOS leaves that to UIKit.</summary>
    bool PositionsCard { get; }

    bool TryAttach(Page page, View host);

    void Detach();

    bool TryMeasure(Page page, out SheetMetrics metrics);

    void Apply(SheetFrame frame);

    void Watch(Action? metricsChanged, Action? back, Action? escape);

    void PresentNative(SheetNative native);

    void UpdateNative(SheetNative native);

    void DismissNative();
}
