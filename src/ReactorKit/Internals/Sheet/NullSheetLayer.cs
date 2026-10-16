namespace Reactor.Kit.Internals.Sheet;

/// <summary>The layer for the plain net10.0 build. It does not present.</summary>
sealed class NullSheetLayer : ISheetLayer
{
    public bool PositionsCard => false;

    public bool TryAttach(Page page, View host)
    {
        _ = page;
        _ = host;
        return false;
    }

    public void Detach()
    {
    }

    public bool TryMeasure(Page page, out SheetMetrics metrics)
    {
        _ = page;
        metrics = new SheetMetrics();
        return false;
    }

    public void Apply(SheetFrame frame)
        => _ = frame;

    public void Watch(Action? metricsChanged, Action? back, Action? escape)
    {
        _ = metricsChanged;
        _ = back;
        _ = escape;
    }

    public void PresentNative(SheetNative native)
        => _ = native;

    public void UpdateNative(SheetNative native)
        => _ = native;

    public void DismissNative()
    {
    }
}
