namespace Reactor.Kit.Internals.Object3D;

static partial class ObjectHandlers
{
    static partial void RegisterPlatform(IMauiHandlersCollection handlers)
        => handlers.AddHandler<Object3DSurface, ObjectViewHandler>();
}
