using Reactor.Kit.SheetTokens;

namespace Reactor.Kit.Internals.Sheet;

/// <summary>The values copied from <see cref="SheetView"/> onto the session.</summary>
sealed class SheetProps
{
    public SheetStyle Style { get; init; }

    public bool IsOpen { get; init; }

    public IReadOnlyList<SheetDetent> Detents { get; init; } = [];

    public string? SelectedDetent { get; init; }

    public bool Grabber { get; init; }

    public double? CornerRadius { get; init; }

    public string? UndimmedDetent { get; init; }

    public bool BackdropDismiss { get; init; }

    public bool DismissDisabled { get; init; }

    public bool ScrollingExpands { get; init; } = true;

    public bool EdgeAttachedInCompactHeight { get; init; } = true;

    public bool WidthFollowsPreferredSize { get; init; }

    public bool PageSizing { get; init; } = true;

    public double PreferredWidth { get; init; } = 540;

    public double PreferredHeight { get; init; } = 620;

    public uint Duration { get; init; } = 350;

    public Easing Easing { get; init; } = Easing.CubicOut;

    public Action? OnDismiss { get; init; }

    public Action<string>? OnDetentChanged { get; init; }
}
