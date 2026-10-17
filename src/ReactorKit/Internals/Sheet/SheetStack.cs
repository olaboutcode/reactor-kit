namespace Reactor.Kit.Internals.Sheet;

/// <summary>Open sheets in one window, oldest first. The last session is the top.</summary>
static class SheetStack
{
    /// <summary>Height of the back card left showing above the next card. Matches the iOS stacked sheet.</summary>
    public const double Peek = 10;

    static readonly Dictionary<object, List<SheetSession>> Roots = [];

    public static void Push(object root, SheetSession session)
    {
        if (!Roots.TryGetValue(root, out var list))
        {
            list = [];
            Roots[root] = list;
        }

        if (!list.Contains(session))
            list.Add(session);
    }

    public static void Remove(object root, SheetSession session)
    {
        if (!Roots.TryGetValue(root, out var list))
            return;

        list.Remove(session);
        if (list.Count == 0)
            Roots.Remove(root);
    }

    public static bool IsTop(object root, SheetSession session)
    {
        if (!Roots.TryGetValue(root, out var list) || list.Count == 0)
            return true;

        return ReferenceEquals(list[^1], session);
    }

    /// <summary>Asks the other sheets in this window to ease into the new stack.</summary>
    public static void RestackOthers(object root, SheetSession current)
    {
        if (!Roots.TryGetValue(root, out var list))
            return;

        foreach (var session in list)
        {
            if (!ReferenceEquals(session, current))
                session.Restack();
        }
    }

    /// <summary>
    /// Depth is how many sheets cover this one. Below is how many it covers.
    /// A single sheet, and a centered sheet in front, are not stacked.
    /// </summary>
    public static (int Depth, int Below, bool Stacked) Place(object root, SheetSession session)
    {
        if (!Roots.TryGetValue(root, out var list))
            return (0, 0, false);

        var index = -1;
        var count = 0;
        SheetSession? top = null;
        foreach (var item in list)
        {
            if (!item.CoversStack)
                continue;

            if (ReferenceEquals(item, session))
                index = count;

            top = item;
            count++;
        }

        if (index < 0 || top == null || count < 2 || !top.StacksAsCard)
            return (0, 0, false);

        return (count - index - 1, index, true);
    }

    /// <summary>True when a bottom card is attached above this one, including one that is sliding away.</summary>
    public static bool HasCardAbove(object root, SheetSession session)
    {
        if (!Roots.TryGetValue(root, out var list))
            return false;

        var seen = false;
        foreach (var item in list)
        {
            if (ReferenceEquals(item, session))
            {
                seen = true;
                continue;
            }

            if (seen && item.IsAttached && item.StacksAsCard)
                return true;
        }

        return false;
    }
}
