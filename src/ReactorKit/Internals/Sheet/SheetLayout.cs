namespace Reactor.Kit.Internals.Sheet;

/// <summary>Geometry output. <see cref="RaiseInactive"/> is true when the requested id was stored but inactive.</summary>
sealed class SheetLayout
{
    public required SheetFrame Frame { get; init; }

    public required IReadOnlyList<SheetRest> Rests { get; init; }

    public bool RaiseInactive { get; init; }

    public double Large { get; init; }
}
