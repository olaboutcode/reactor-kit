using Microsoft.Maui.Handlers;

namespace Reactor.Kit;

/// <summary>Registers ReactorKit handlers.</summary>
public static class ReactorKitHost
{
    public static MauiAppBuilder UseReactorKit(this MauiAppBuilder builder)
    {
        builder.ConfigureMauiHandlers(static handlers => Glaze.GlazeHandlers.Register(handlers));
        return builder;
    }
}
