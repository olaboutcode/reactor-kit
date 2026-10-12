using Reactor.Kit.Internals.Object3D;
using Reactor.Kit.ObjectViewTokens;

namespace Reactor.Kit;

/// <summary>
/// One lit mesh. The platform handler draws it.
/// </summary>
public sealed class ObjectSurface : Microsoft.Maui.Controls.View
{
    public static readonly BindableProperty MeshProperty = BindableProperty.Create(
        nameof(Mesh), typeof(Mesh), typeof(ObjectSurface), null, propertyChanged: OnChanged);

    public static readonly BindableProperty SourceProperty = BindableProperty.Create(
        nameof(Source), typeof(string), typeof(ObjectSurface), null, propertyChanged: OnChanged);

    public static readonly BindableProperty YawProperty = BindableProperty.Create(
        nameof(Yaw), typeof(double), typeof(ObjectSurface), 30d, propertyChanged: OnChanged);

    public static readonly BindableProperty PitchProperty = BindableProperty.Create(
        nameof(Pitch), typeof(double), typeof(ObjectSurface), 20d, propertyChanged: OnChanged);

    public static readonly BindableProperty DistanceProperty = BindableProperty.Create(
        nameof(Distance), typeof(double), typeof(ObjectSurface), double.NaN, propertyChanged: OnChanged);

    public static readonly BindableProperty TargetProperty = BindableProperty.Create(
        nameof(Target), typeof(LookTarget), typeof(ObjectSurface), LookTarget.Unset, propertyChanged: OnChanged);

    public static readonly BindableProperty RootRotationProperty = BindableProperty.Create(
        nameof(RootRotation), typeof(ModelRotation), typeof(ObjectSurface), default(ModelRotation), propertyChanged: OnChanged);

    public static readonly BindableProperty RootScaleProperty = BindableProperty.Create(
        nameof(RootScale), typeof(double), typeof(ObjectSurface), 1d, propertyChanged: OnChanged);

    public static readonly BindableProperty ColorProperty = BindableProperty.Create(
        nameof(Color), typeof(Color), typeof(ObjectSurface), Colors.White, propertyChanged: OnChanged);

    public static readonly BindableProperty ClearColorProperty = BindableProperty.Create(
        nameof(ClearColor), typeof(Color), typeof(ObjectSurface), Colors.Transparent, propertyChanged: OnChanged);

    public static readonly BindableProperty GesturesProperty = BindableProperty.Create(
        nameof(Gestures), typeof(bool), typeof(ObjectSurface), true, propertyChanged: OnChanged);

    public static readonly BindableProperty AutoRotateProperty = BindableProperty.Create(
        nameof(AutoRotate), typeof(bool), typeof(ObjectSurface), false, propertyChanged: OnChanged);

    readonly ObjectPose _pose = new();
    Mesh? _sourceMesh;
    string? _sourceLoaded;

    public ObjectSurface()
    {
        BackgroundColor = Colors.Transparent;
        _pose.Changed += OnPoseChanged;
        _pose.Settled += OnPoseSettled;
    }

    public Mesh? Mesh
    {
        get => (Mesh?)GetValue(MeshProperty);
        set => SetValue(MeshProperty, value);
    }

    public string? Source
    {
        get => (string?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public double Yaw
    {
        get => (double)GetValue(YawProperty);
        set => SetValue(YawProperty, value);
    }

    public double Pitch
    {
        get => (double)GetValue(PitchProperty);
        set => SetValue(PitchProperty, value);
    }

    public double Distance
    {
        get => (double)GetValue(DistanceProperty);
        set => SetValue(DistanceProperty, value);
    }

    public LookTarget Target
    {
        get => (LookTarget)GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    public ModelRotation RootRotation
    {
        get => (ModelRotation)GetValue(RootRotationProperty);
        set => SetValue(RootRotationProperty, value);
    }

    public double RootScale
    {
        get => (double)GetValue(RootScaleProperty);
        set => SetValue(RootScaleProperty, value);
    }

    public Color Color
    {
        get => (Color)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public Color ClearColor
    {
        get => (Color)GetValue(ClearColorProperty);
        set => SetValue(ClearColorProperty, value);
    }

    public bool Gestures
    {
        get => (bool)GetValue(GesturesProperty);
        set => SetValue(GesturesProperty, value);
    }

    public bool AutoRotate
    {
        get => (bool)GetValue(AutoRotateProperty);
        set => SetValue(AutoRotateProperty, value);
    }

    internal ObjectPose Pose
        => _pose;

    internal event EventHandler? FrameInvalidated;

    internal event EventHandler<OrbitChangedEventArgs>? OrbitChanged;

    internal void Push()
    {
        if (Mesh is { } mesh)
        {
            _pose.SetMesh(mesh);
        }
        else if (!string.IsNullOrEmpty(Source))
        {
            if (_sourceMesh is null || !string.Equals(Source, _sourceLoaded, StringComparison.Ordinal))
            {
                _sourceMesh = GlbReader.Read(Source);
                _sourceLoaded = Source;
            }

            _pose.SetMesh(_sourceMesh);
        }
        else
        {
            _pose.SetMesh(Mesh.Empty);
        }

        _pose.SetYaw(Yaw);
        _pose.SetPitch(Pitch);
        _pose.SetDistance(Distance);
        _pose.SetTarget(Target);
        _pose.SetRotation(RootRotation);
        _pose.SetScale(RootScale);
        _pose.SetTint(Color.Red, Color.Green, Color.Blue);
        _pose.SetClear(ClearColor.Red, ClearColor.Green, ClearColor.Blue, ClearColor.Alpha);
        _pose.SetGestures(Gestures);
        _pose.SetAutoRotate(AutoRotate);
    }

    static void OnChanged(BindableObject bindable, object oldValue, object newValue)
        => ((ObjectSurface)bindable).Push();

    void OnPoseChanged(object? sender, EventArgs e)
        => FrameInvalidated?.Invoke(this, EventArgs.Empty);

    void OnPoseSettled(object? sender, OrbitChangedEventArgs e)
        => OrbitChanged?.Invoke(this, e);
}
