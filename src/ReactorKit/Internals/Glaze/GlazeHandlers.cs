namespace Reactor.Kit.Internals.Glaze;

static partial class GlazeHandlers
{
    public static void Register(IMauiHandlersCollection handlers)
        => RegisterPlatform(handlers);

    static partial void RegisterPlatform(IMauiHandlersCollection handlers);
}
