namespace KitTests;

/// <summary>
/// Subtle Material Design 3 elevation shadows for MauiReactor.
/// Equal X/Y offsets, restrained opacity.
/// 
/// Level 1: Offset (0.3, 0.3), Radius 4, Opacity 0.28  ← default
/// Level 2: Offset (0.5, 0.5), Radius 5, Opacity 0.30
/// Level 3: Offset (0.7, 0.7), Radius 6, Opacity 0.32
/// </summary>
public static class ShadowElevation
{
    public static MauiReactor.Shadow For(int level) => level switch
    {
        2 => new MauiReactor.Shadow()
                .Brush(Brush.Black)
                .Offset(0.5, 0.5)
                .Radius(5)
                .Opacity(0.30f),
        3 => new MauiReactor.Shadow()
                .Brush(Brush.Black)
                .Offset(0.7, 0.7)
                .Radius(6)
                .Opacity(0.32f),
        _ => new MauiReactor.Shadow() // Level 1 (default)
                .Brush(Brush.Black)
                .Offset(0.3, 0.3)
                .Radius(4)
                .Opacity(0.28f),
    };
}
