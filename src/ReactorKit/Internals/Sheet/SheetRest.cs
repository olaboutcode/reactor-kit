namespace Reactor.Kit.Internals.Sheet;

/// <summary>One resting detent after compact-height filtering.</summary>
sealed class SheetRest
{
    public SheetRest(string id, double height)
    {
        Id = id;
        Height = height;
    }

    public string Id { get; }

    public double Height { get; }
}
