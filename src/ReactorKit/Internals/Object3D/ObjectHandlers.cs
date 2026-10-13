namespace Reactor.Kit.Internals.Object3D;

static partial class ObjectHandlers
{
    public static void Register(IMauiHandlersCollection handlers)
        => RegisterPlatform(handlers);

    static partial void RegisterPlatform(IMauiHandlersCollection handlers);
}
