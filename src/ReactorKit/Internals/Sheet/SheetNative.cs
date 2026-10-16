namespace Reactor.Kit.Internals.Sheet;

/// <summary>What an iOS sheet controller needs. Android and Windows ignore the callbacks they do not use.</summary>
sealed class SheetNative
{
    public required SheetProps Props { get; init; }

    public required SheetLayout Layout { get; init; }

    public required Page Page { get; init; }

    public required View Host { get; init; }

    public Action<string>? DetentSettled { get; init; }

    public Action? Dismissed { get; init; }

    public Action<SheetPointer>? Pointer { get; init; }
}
