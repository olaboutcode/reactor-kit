using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;

namespace Reactor.Kit.Glaze;

sealed class GlazeViewHandler : ViewHandler<GlazeView, GlazePlatformView>
{
    static readonly IPropertyMapper<GlazeView, GlazeViewHandler> Mapper =
        new PropertyMapper<GlazeView, GlazeViewHandler>(ViewMapper)
        {
            [nameof(GlazeView.Content)] = MapContent,
            [nameof(GlazeView.TintColor)] = MapMaterial,
            [nameof(GlazeView.TintOpacity)] = MapMaterial,
            [nameof(GlazeView.NoiseOpacity)] = MapMaterial,
            [nameof(GlazeView.Stroke)] = MapMaterial,
            [nameof(GlazeView.StrokeThickness)] = MapMaterial,
            [nameof(GlazeView.CornerRadius)] = MapMaterial,
            [nameof(GlazeView.Padding)] = MapLayout
        };

    public GlazeViewHandler() : base(Mapper)
    {
    }

    protected override GlazePlatformView CreatePlatformView() => new();

    public override void SetVirtualView(IView view)
    {
        base.SetVirtualView(view);
        PlatformView.VirtualView = VirtualView;
    }

    static void MapContent(GlazeViewHandler handler, GlazeView view) => handler.UpdateContent();

    static void MapMaterial(GlazeViewHandler handler, GlazeView view) => handler.PlatformView.Apply(view);

    static void MapLayout(GlazeViewHandler handler, GlazeView view) => handler.PlatformView.InvalidateArrange();

    void UpdateContent()
    {
        if (VirtualView is null || MauiContext is null)
            return;

        var presented = ((IContentView)VirtualView).PresentedContent;
        PlatformView.SetContent(presented?.ToPlatform(MauiContext) as FrameworkElement);
    }
}
