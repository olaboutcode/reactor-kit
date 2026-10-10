namespace Reactor.Kit.Glaze;

static partial class GlazeHandlers
{
    static partial void RegisterPlatform(IMauiHandlersCollection handlers)
        => handlers.AddHandler<GlazeView, GlazeViewHandler>();
}
