namespace Reactor.Kit.SheetTokens;

/// <summary>One resting height of a sheet. The page lists these from shortest to tallest.</summary>
public sealed class SheetDetent
{
    SheetDetent(string id, SheetDetentKind kind, double value)
    {
        Id = id;
        Kind = kind;
        Value = value;
    }

    /// <summary>Case-sensitive identifier reported by <c>OnDetentChanged</c>.</summary>
    public string Id { get; }

    /// <summary>How <see cref="Value"/> is read.</summary>
    public SheetDetentKind Kind { get; }

    /// <summary>Fraction or points. Medium and large store 0. Geometry computes the height.</summary>
    public double Value { get; }

    /// <summary>Half of the large detent.</summary>
    public static SheetDetent Medium { get; } = new(SheetIds.Medium, SheetDetentKind.Medium, 0);

    /// <summary>The fully expanded sheet.</summary>
    public static SheetDetent Large { get; } = new(SheetIds.Large, SheetDetentKind.Large, 0);

    /// <summary>A fraction of the large detent. <paramref name="fraction"/> is clamped when the sheet is measured.</summary>
    public static SheetDetent Fraction(string id, double fraction)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        return new SheetDetent(id, SheetDetentKind.Fraction, fraction);
    }

    /// <summary>A point height, clamped to the large detent when the sheet is measured.</summary>
    public static SheetDetent Height(string id, double points)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        return new SheetDetent(id, SheetDetentKind.Height, points);
    }
}
