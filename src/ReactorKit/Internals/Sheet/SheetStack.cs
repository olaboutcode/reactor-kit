namespace Reactor.Kit.Internals.Sheet;

/// <summary>Open sheets in one window, oldest first. The last session is the top.</summary>
static class SheetStack
{
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
}
