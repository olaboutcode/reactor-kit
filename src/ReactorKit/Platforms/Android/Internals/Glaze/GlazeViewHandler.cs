using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Reactor.Kit.Internals.Glaze;

sealed class GlazeViewHandler : ViewHandler<GlazeView, GlazePlatformView>
{
    static readonly IPropertyMapper<GlazeView, GlazeViewHandler> Mapper =
        new PropertyMapper<GlazeView, GlazeViewHandler>(ViewMapper)
        {
            [nameof(GlazeView.Content)] = MapContent,
            [nameof(GlazeView.TintColor)] = MapMaterial,
            [nameof(GlazeView.TintOpacity)] = MapMaterial,
            [nameof(GlazeView.BlurRadius)] = MapMaterial,
            [nameof(GlazeView.NoiseOpacity)] = MapMaterial,
            [nameof(GlazeView.Stroke)] = MapMaterial,
            [nameof(GlazeView.StrokeThickness)] = MapMaterial,
            [nameof(GlazeView.CornerRadius)] = MapMaterial,
            [nameof(GlazeView.Padding)] = MapLayout
        };

    public GlazeViewHandler() : base(Mapper)
    {
    }

    protected override GlazePlatformView CreatePlatformView()
        => new(Context ?? throw new InvalidOperationException("Android context is required."));

    public override void SetVirtualView(IView view)
    {
        base.SetVirtualView(view);
        PlatformView.VirtualView = VirtualView;
    }

    protected override void DisconnectHandler(GlazePlatformView platformView)
    {
        platformView.Release();
        platformView.VirtualView = null;
        base.DisconnectHandler(platformView);
    }

    static void MapContent(GlazeViewHandler handler, GlazeView view) => handler.UpdateContent();

    static void MapMaterial(GlazeViewHandler handler, GlazeView view) => handler.PlatformView.Apply(view);

    static void MapLayout(GlazeViewHandler handler, GlazeView view) => handler.PlatformView.RequestLayout();

    void UpdateContent()
    {
        if (VirtualView is null || MauiContext is null)
            return;

        var presented = ((IContentView)VirtualView).PresentedContent;
        PlatformView.SetContent(presented?.ToPlatform(MauiContext));
    }
}
