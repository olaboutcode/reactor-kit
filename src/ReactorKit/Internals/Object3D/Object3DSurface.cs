using System.Runtime.ExceptionServices;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using Reactor.Kit.Object3DViewTokens;

namespace Reactor.Kit.Internals.Object3D;

/// <summary>
/// One lit mesh. The platform handler draws it.
/// </summary>
internal sealed class Object3DSurface : ContentView
{
    public static readonly BindableProperty MeshProperty = BindableProperty.Create(
        nameof(Mesh), typeof(Mesh), typeof(Object3DSurface), null, propertyChanged: OnChanged);

    public static readonly BindableProperty SourceProperty = BindableProperty.Create(
        nameof(Source), typeof(string), typeof(Object3DSurface), null, propertyChanged: OnChanged);

    public static readonly BindableProperty PackagedSourceProperty = BindableProperty.Create(
        nameof(PackagedSource), typeof(string), typeof(Object3DSurface), null, propertyChanged: OnChanged);

    public static readonly BindableProperty YawProperty = BindableProperty.Create(
        nameof(Yaw), typeof(double), typeof(Object3DSurface), 30d, propertyChanged: OnChanged);

    public static readonly BindableProperty PitchProperty = BindableProperty.Create(
        nameof(Pitch), typeof(double), typeof(Object3DSurface), 20d, propertyChanged: OnChanged);

    public static readonly BindableProperty DistanceProperty = BindableProperty.Create(
        nameof(Distance), typeof(double), typeof(Object3DSurface), double.NaN, propertyChanged: OnChanged);

    public static readonly BindableProperty TargetProperty = BindableProperty.Create(
        nameof(Target), typeof(LookTarget), typeof(Object3DSurface), LookTarget.Unset, propertyChanged: OnChanged);

    public static readonly BindableProperty RootRotationProperty = BindableProperty.Create(
        nameof(RootRotation), typeof(ModelRotation), typeof(Object3DSurface), default(ModelRotation), propertyChanged: OnChanged);

    public static readonly BindableProperty RootScaleProperty = BindableProperty.Create(
        nameof(RootScale), typeof(double), typeof(Object3DSurface), 1d, propertyChanged: OnChanged);

    public static readonly BindableProperty ColorProperty = BindableProperty.Create(
        nameof(Color), typeof(Color), typeof(Object3DSurface), Colors.White, propertyChanged: OnChanged);

    public static readonly BindableProperty ClearColorProperty = BindableProperty.Create(
        nameof(ClearColor), typeof(Color), typeof(Object3DSurface), Colors.Transparent, propertyChanged: OnChanged);

    public static readonly BindableProperty GesturesProperty = BindableProperty.Create(
        nameof(Gestures), typeof(bool), typeof(Object3DSurface), true, propertyChanged: OnChanged);

    public static readonly BindableProperty AutoRotateProperty = BindableProperty.Create(
        nameof(AutoRotate), typeof(bool), typeof(Object3DSurface), false, propertyChanged: OnChanged);

    readonly ObjectPose _pose = new();
    Mesh? _sourceMesh;
    string? _sourceLoaded;
    Mesh? _packageMesh;
    string? _packageRequested;
    string? _packageLoaded;
    int _packageGeneration;

    public Object3DSurface()
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

    public string? PackagedSource
    {
        get => (string?)GetValue(PackagedSourceProperty);
        set => SetValue(PackagedSourceProperty, value);
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
        else if (!string.IsNullOrWhiteSpace(PackagedSource))
        {
            EnsurePackage(PackagedSource);
            _pose.SetMesh(_packageMesh ?? Mesh.Empty);
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

    void EnsurePackage(string name)
    {
        if (string.Equals(name, _packageLoaded, StringComparison.Ordinal) && _packageMesh is not null)
            return;

        if (string.Equals(name, _packageRequested, StringComparison.Ordinal))
            return;

        _packageRequested = name;
        _packageLoaded = null;
        _packageMesh = null;
        var generation = ++_packageGeneration;
        _ = ReadPackage(name, generation);
    }

    async Task ReadPackage(string name, int generation)
    {
        try
        {
            var bytes = await ReadAsset(name).ConfigureAwait(false);
            var mesh = GlbReader.Read(bytes, name);
            MainThread.BeginInvokeOnMainThread(() => ApplyPackage(name, generation, mesh, null));
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() => ApplyPackage(name, generation, null, ex));
        }
    }

    static async Task<byte[]> ReadAsset(string name)
    {
        var stream = await FileSystem.OpenAppPackageFileAsync(name).ConfigureAwait(false);
        try
        {
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer).ConfigureAwait(false);
            return buffer.ToArray();
        }
        finally
        {
            await stream.DisposeAsync().ConfigureAwait(false);
        }
    }

    void ApplyPackage(string name, int generation, Mesh? mesh, Exception? error)
    {
        if (generation != _packageGeneration || !string.Equals(PackagedSource, name, StringComparison.Ordinal))
            return;

        if (error is not null)
        {
            _packageRequested = null;
            if (Mesh is null)
                ExceptionDispatchInfo.Capture(error).Throw();

            return;
        }

        _packageMesh = mesh;
        _packageLoaded = name;
        if (Mesh is null)
            Push();
    }

    static void OnChanged(BindableObject bindable, object oldValue, object newValue)
        => ((Object3DSurface)bindable).Push();

    void OnPoseChanged(object? sender, EventArgs e)
        => FrameInvalidated?.Invoke(this, EventArgs.Empty);

    void OnPoseSettled(object? sender, OrbitChangedEventArgs e)
        => OrbitChanged?.Invoke(this, e);
}
