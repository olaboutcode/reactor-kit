using Microsoft.Maui.Handlers;
using Reactor.Kit;

namespace Reactor.Kit.Internals.Object3D;

sealed class ObjectViewHandler : ViewHandler<Object3DSurface, ObjectPlatformView>
{
    public static readonly IPropertyMapper<Object3DSurface, ObjectViewHandler> Mapper =
        new PropertyMapper<Object3DSurface, ObjectViewHandler>(ViewMapper)
        {
            [nameof(Object3DSurface.Mesh)] = MapPose,
            [nameof(Object3DSurface.Source)] = MapPose,
            [nameof(Object3DSurface.PackagedSource)] = MapPose,
            [nameof(Object3DSurface.Yaw)] = MapPose,
            [nameof(Object3DSurface.Pitch)] = MapPose,
            [nameof(Object3DSurface.Distance)] = MapPose,
            [nameof(Object3DSurface.Target)] = MapPose,
            [nameof(Object3DSurface.RootRotation)] = MapPose,
            [nameof(Object3DSurface.RootScale)] = MapPose,
            [nameof(Object3DSurface.Color)] = MapPose,
            [nameof(Object3DSurface.ClearColor)] = MapPose,
            [nameof(Object3DSurface.Gestures)] = MapPose,
            [nameof(Object3DSurface.AutoRotate)] = MapPose,
        };

    public ObjectViewHandler()
        : base(Mapper)
    {
    }

    protected override ObjectPlatformView CreatePlatformView()
        => new(Context ?? throw new InvalidOperationException("Android context is required."));

    public override void SetVirtualView(Microsoft.Maui.IView view)
    {
        base.SetVirtualView(view);
        PlatformView.Bind(VirtualView);
    }

    protected override void DisconnectHandler(ObjectPlatformView platformView)
    {
        platformView.Unbind();
        base.DisconnectHandler(platformView);
    }

    static void MapPose(ObjectViewHandler handler, Object3DSurface view)
        => handler.PlatformView.Bind(view);
}
