using MauiReactor;
using Reactor.Kit.Internals.Object3D;
using Reactor.Kit.ObjectViewTokens;
using MauiView = Microsoft.Maui.Controls.View;

namespace Reactor.Kit;

/// <summary>
/// One rigid mesh. Drag to orbit, pinch to zoom, and drag with two fingers to pan.
/// </summary>
public sealed class ObjectView : View<ObjectSurface>
{
    Action<Orbit>? _onOrbitChanged;

    internal ObjectView(Action<MauiView?>? componentRefAction = null)
        : base(componentRefAction is null ? null : host => componentRefAction(host))
    {
    }

    protected override void OnMount()
    {
        _nativeControl ??= new ObjectSurface();
        base.OnMount();
    }

    public ObjectView Mesh(Mesh mesh)
        => this.Set(ObjectSurface.MeshProperty, mesh);

    public ObjectView Mesh(Func<Mesh> mesh, IComponentWithState? state = null)
        => this.Set(ObjectSurface.MeshProperty, new PropertyValue<Mesh>(mesh, state));

    /// <summary>
    /// Filesystem path to a .glb. Ignored when <see cref="Mesh(Mesh)"/> or <see cref="PackagedSource(string)"/> is set.
    /// A Maui asset name is not a path. The file is read when the path is set.
    /// A missing file throws the IO exception from that read.
    /// A file that is not a glTF 2.0 .glb throws <see cref="InvalidDataException"/>.
    /// </summary>
    public ObjectView Source(string path)
        => this.Set(ObjectSurface.SourceProperty, path);

    public ObjectView Source(Func<string> path, IComponentWithState? state = null)
        => this.Set(ObjectSurface.SourceProperty, new PropertyValue<string>(path, state));

    /// <summary>
    /// Maui package asset name of a .glb, such as <c>industrial_microscope.glb</c>.
    /// Ignored when <see cref="Mesh(Mesh)"/> is set. Wins over <see cref="Source(string)"/>.
    /// The read starts when the name is set. A missing asset throws <see cref="FileNotFoundException"/> on the UI thread.
    /// A file that is not a glTF 2.0 .glb throws <see cref="InvalidDataException"/> on the UI thread.
    /// An image file beside the asset is not read. Images inside the .glb are.
    /// </summary>
    public ObjectView PackagedSource(string name)
        => this.Set(ObjectSurface.PackagedSourceProperty, name);

    public ObjectView PackagedSource(Func<string> name, IComponentWithState? state = null)
        => this.Set(ObjectSurface.PackagedSourceProperty, new PropertyValue<string>(name, state));

    public ObjectView Yaw(double degrees)
        => this.Set(ObjectSurface.YawProperty, degrees);

    public ObjectView Yaw(Func<double> degrees, IComponentWithState? state = null)
        => this.Set(ObjectSurface.YawProperty, new PropertyValue<double>(degrees, state));

    public ObjectView Pitch(double degrees)
        => this.Set(ObjectSurface.PitchProperty, degrees);

    public ObjectView Pitch(Func<double> degrees, IComponentWithState? state = null)
        => this.Set(ObjectSurface.PitchProperty, new PropertyValue<double>(degrees, state));

    public ObjectView Distance(double distance)
        => this.Set(ObjectSurface.DistanceProperty, distance);

    public ObjectView Distance(Func<double> distance, IComponentWithState? state = null)
        => this.Set(ObjectSurface.DistanceProperty, new PropertyValue<double>(distance, state));

    public ObjectView Target(double x, double y, double z)
        => this.Set(ObjectSurface.TargetProperty, new LookTarget(x, y, z));

    public ObjectView Target(Func<LookTarget> target, IComponentWithState? state = null)
        => this.Set(ObjectSurface.TargetProperty, new PropertyValue<LookTarget>(target, state));

    public ObjectView Rotation(double yaw, double pitch, double roll)
        => this.Set(ObjectSurface.RootRotationProperty, new ModelRotation(yaw, pitch, roll));

    public ObjectView Rotation(Func<ModelRotation> rotation, IComponentWithState? state = null)
        => this.Set(ObjectSurface.RootRotationProperty, new PropertyValue<ModelRotation>(rotation, state));

    public ObjectView Scale(double scale)
        => this.Set(ObjectSurface.RootScaleProperty, scale);

    public ObjectView Scale(Func<double> scale, IComponentWithState? state = null)
        => this.Set(ObjectSurface.RootScaleProperty, new PropertyValue<double>(scale, state));

    public ObjectView Color(Color color)
        => this.Set(ObjectSurface.ColorProperty, color);

    public ObjectView Color(Func<Color> color, IComponentWithState? state = null)
        => this.Set(ObjectSurface.ColorProperty, new PropertyValue<Color>(color, state));

    public ObjectView Background(Color color)
        => this.Set(ObjectSurface.ClearColorProperty, color);

    public ObjectView Background(Func<Color> color, IComponentWithState? state = null)
        => this.Set(ObjectSurface.ClearColorProperty, new PropertyValue<Color>(color, state));

    public ObjectView Gestures(bool enabled)
        => this.Set(ObjectSurface.GesturesProperty, enabled);

    public ObjectView Gestures(Func<bool> enabled, IComponentWithState? state = null)
        => this.Set(ObjectSurface.GesturesProperty, new PropertyValue<bool>(enabled, state));

    public ObjectView AutoRotate(bool enabled)
        => this.Set(ObjectSurface.AutoRotateProperty, enabled);

    public ObjectView AutoRotate(Func<bool> enabled, IComponentWithState? state = null)
        => this.Set(ObjectSurface.AutoRotateProperty, new PropertyValue<bool>(enabled, state));

    public ObjectView OnOrbitChanged(Action<Orbit> callback)
    {
        _onOrbitChanged = callback;
        return this;
    }

    protected override void OnAttachNativeEvents()
    {
        if (NativeControl is ObjectSurface host)
        {
            host.OrbitChanged -= OnNativeOrbitChanged;
            if (_onOrbitChanged is not null)
                host.OrbitChanged += OnNativeOrbitChanged;
        }

        base.OnAttachNativeEvents();
    }

    protected override void OnDetachNativeEvents()
    {
        if (NativeControl is ObjectSurface host)
            host.OrbitChanged -= OnNativeOrbitChanged;

        base.OnDetachNativeEvents();
    }

    void OnNativeOrbitChanged(object? sender, OrbitChangedEventArgs e)
        => _onOrbitChanged?.Invoke(e.Orbit);
}
