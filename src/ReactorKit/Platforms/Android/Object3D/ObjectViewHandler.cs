using Microsoft.Maui.Handlers;
using Reactor.Kit;

namespace Reactor.Kit.Internals.Object3D;

sealed class ObjectViewHandler : ViewHandler<ObjectSurface, ObjectPlatformView>
{
    public static readonly IPropertyMapper<ObjectSurface, ObjectViewHandler> Mapper =
        new PropertyMapper<ObjectSurface, ObjectViewHandler>(ViewMapper)
        {
            [nameof(ObjectSurface.Mesh)] = MapPose,
            [nameof(ObjectSurface.Source)] = MapPose,
            [nameof(ObjectSurface.PackagedSource)] = MapPose,
            [nameof(ObjectSurface.Yaw)] = MapPose,
            [nameof(ObjectSurface.Pitch)] = MapPose,
            [nameof(ObjectSurface.Distance)] = MapPose,
            [nameof(ObjectSurface.Target)] = MapPose,
            [nameof(ObjectSurface.RootRotation)] = MapPose,
            [nameof(ObjectSurface.RootScale)] = MapPose,
            [nameof(ObjectSurface.Color)] = MapPose,
            [nameof(ObjectSurface.ClearColor)] = MapPose,
            [nameof(ObjectSurface.Gestures)] = MapPose,
            [nameof(ObjectSurface.AutoRotate)] = MapPose,
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

    static void MapPose(ObjectViewHandler handler, ObjectSurface view)
        => handler.PlatformView.Bind(view);
}
