using MauiReactor;
using Reactor.Kit.Internals.Object3D;
using Reactor.Kit.Object3DViewTokens;
using MauiContentView = Microsoft.Maui.Controls.ContentView;
using NativeObject = Reactor.Kit.Internals.Object3D.Object3DSurface;

namespace Reactor.Kit;

/// <summary>
/// One rigid mesh. Drag to orbit, pinch to zoom, and drag with two fingers to pan.
/// </summary>
public sealed class Object3DView : View<MauiContentView>
{
    Action<Orbit>? _onOrbitChanged;

    internal Object3DView(Action<MauiContentView?>? componentRefAction = null)
        : base(componentRefAction)
    {
    }

    protected override void OnMount()
    {
        _nativeControl ??= new NativeObject();
        base.OnMount();
    }

    public Object3DView Mesh(Mesh mesh)
        => this.Set(NativeObject.MeshProperty, mesh);

    public Object3DView Mesh(Func<Mesh> mesh, IComponentWithState? state = null)
        => this.Set(NativeObject.MeshProperty, new PropertyValue<Mesh>(mesh, state));

    /// <summary>
    /// Filesystem path to a .glb. Ignored when <see cref="Mesh(Mesh)"/> or <see cref="PackagedSource(string)"/> is set.
    /// A Maui asset name is not a path. The file is read when the path is set.
    /// A missing file throws the IO exception from that read.
    /// A file that is not a glTF 2.0 .glb throws <see cref="InvalidDataException"/>.
    /// </summary>
    public Object3DView Source(string path)
        => this.Set(NativeObject.SourceProperty, path);

    public Object3DView Source(Func<string> path, IComponentWithState? state = null)
        => this.Set(NativeObject.SourceProperty, new PropertyValue<string>(path, state));

    /// <summary>
    /// Maui package asset name of a .glb, such as <c>industrial_microscope.glb</c>.
    /// Ignored when <see cref="Mesh(Mesh)"/> is set. Wins over <see cref="Source(string)"/>.
    /// The read starts when the name is set. A missing asset throws <see cref="FileNotFoundException"/> on the UI thread.
    /// A file that is not a glTF 2.0 .glb throws <see cref="InvalidDataException"/> on the UI thread.
    /// An image file beside the asset is not read. Images inside the .glb are.
    /// </summary>
    public Object3DView PackagedSource(string name)
        => this.Set(NativeObject.PackagedSourceProperty, name);

    public Object3DView PackagedSource(Func<string> name, IComponentWithState? state = null)
        => this.Set(NativeObject.PackagedSourceProperty, new PropertyValue<string>(name, state));

    public Object3DView Yaw(double degrees)
        => this.Set(NativeObject.YawProperty, degrees);

    public Object3DView Yaw(Func<double> degrees, IComponentWithState? state = null)
        => this.Set(NativeObject.YawProperty, new PropertyValue<double>(degrees, state));

    public Object3DView Pitch(double degrees)
        => this.Set(NativeObject.PitchProperty, degrees);

    public Object3DView Pitch(Func<double> degrees, IComponentWithState? state = null)
        => this.Set(NativeObject.PitchProperty, new PropertyValue<double>(degrees, state));

    public Object3DView Distance(double distance)
        => this.Set(NativeObject.DistanceProperty, distance);

    public Object3DView Distance(Func<double> distance, IComponentWithState? state = null)
        => this.Set(NativeObject.DistanceProperty, new PropertyValue<double>(distance, state));

    public Object3DView Target(double x, double y, double z)
        => this.Set(NativeObject.TargetProperty, new LookTarget(x, y, z));

    public Object3DView Target(Func<LookTarget> target, IComponentWithState? state = null)
        => this.Set(NativeObject.TargetProperty, new PropertyValue<LookTarget>(target, state));

    public Object3DView Rotation(double yaw, double pitch, double roll)
        => this.Set(NativeObject.RootRotationProperty, new ModelRotation(yaw, pitch, roll));

    public Object3DView Rotation(Func<ModelRotation> rotation, IComponentWithState? state = null)
        => this.Set(NativeObject.RootRotationProperty, new PropertyValue<ModelRotation>(rotation, state));

    public Object3DView Scale(double scale)
        => this.Set(NativeObject.RootScaleProperty, scale);

    public Object3DView Scale(Func<double> scale, IComponentWithState? state = null)
        => this.Set(NativeObject.RootScaleProperty, new PropertyValue<double>(scale, state));

    public Object3DView Color(Color color)
        => this.Set(NativeObject.ColorProperty, color);

    public Object3DView Color(Func<Color> color, IComponentWithState? state = null)
        => this.Set(NativeObject.ColorProperty, new PropertyValue<Color>(color, state));

    public Object3DView Background(Color color)
        => this.Set(NativeObject.ClearColorProperty, color);

    public Object3DView Background(Func<Color> color, IComponentWithState? state = null)
        => this.Set(NativeObject.ClearColorProperty, new PropertyValue<Color>(color, state));

    public Object3DView Gestures(bool enabled)
        => this.Set(NativeObject.GesturesProperty, enabled);

    public Object3DView Gestures(Func<bool> enabled, IComponentWithState? state = null)
        => this.Set(NativeObject.GesturesProperty, new PropertyValue<bool>(enabled, state));

    public Object3DView AutoRotate(bool enabled)
        => this.Set(NativeObject.AutoRotateProperty, enabled);

    public Object3DView AutoRotate(Func<bool> enabled, IComponentWithState? state = null)
        => this.Set(NativeObject.AutoRotateProperty, new PropertyValue<bool>(enabled, state));

    public Object3DView OnOrbitChanged(Action<Orbit> callback)
    {
        _onOrbitChanged = callback;
        return this;
    }

    protected override void OnAttachNativeEvents()
    {
        if (NativeControl is NativeObject host)
        {
            host.OrbitChanged -= OnNativeOrbitChanged;
            if (_onOrbitChanged is not null)
                host.OrbitChanged += OnNativeOrbitChanged;
        }

        base.OnAttachNativeEvents();
    }

    protected override void OnDetachNativeEvents()
    {
        if (NativeControl is NativeObject host)
            host.OrbitChanged -= OnNativeOrbitChanged;

        base.OnDetachNativeEvents();
    }

    void OnNativeOrbitChanged(object? sender, OrbitChangedEventArgs e)
        => _onOrbitChanged?.Invoke(e.Orbit);
}
