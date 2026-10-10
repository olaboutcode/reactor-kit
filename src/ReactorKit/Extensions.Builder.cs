namespace Reactor.Kit;

/// <summary>
/// Registers ReactorKit handlers and the control factories.
/// </summary>
#pragma warning disable MA0048 // Ignore file name & type mismatch
public static partial class Extensions
#pragma warning restore MA0048
{
    public static MauiAppBuilder UseReactorKit(this MauiAppBuilder builder, Action<ReactorKitSettings>? configure = null)
    {
        var settings = new ReactorKitSettings();
        configure?.Invoke(settings);

        builder.ConfigureMauiHandlers(handlers =>
        {
            Glaze.GlazeHandlers.Register(handlers);
            if (settings.EnableBorderlessEntry)
                Handlers.BorderlessEntryHandler.Register();
        });
        return builder;
    }
}
