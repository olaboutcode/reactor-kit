using System.Numerics;
using Reactor.Kit.ObjectViewTokens;

namespace Reactor.Kit.Internals.Object3D;

/// <summary>
/// Camera, root transform, and gestures. The platform view owns the live numbers.
/// Bound values apply only when they change, so a gesture survives the next render.
/// </summary>
sealed class ObjectPose
{
    const float DragDegrees = 180f;
    const float SpinDegreesPerSecond = 20f;
    const float MinPitch = -89f;
    const float MaxPitch = 89f;

    readonly Lock _gate = new();
    Mesh _mesh = Mesh.Empty;
    int _generation = 1;
    double _boundYaw = double.NaN;
    double _boundPitch = double.NaN;
    double _boundDistance = double.NaN;
    LookTarget _boundTarget = LookTarget.Unset;
    ModelRotation _boundRotation = new(double.NaN, double.NaN, double.NaN);
    double _boundScale = double.NaN;
    float _tintR = float.NaN;
    float _tintG = float.NaN;
    float _tintB = float.NaN;
    float _clearR = float.NaN;
    float _clearG;
    float _clearB;
    float _clearA;
    bool _boundGestures;
    bool _gesturesCompared;
    bool _boundSpin;
    bool _spinCompared;

    float _yaw = 30f;
    float _pitch = 20f;
    float _distance = 3f;
    Vector3 _target;
    bool _distanceHeld;
    bool _targetHeld;
    float _rotYaw;
    float _rotPitch;
    float _rotRoll;
    float _scale = 1f;
    Vector3 _tint = Vector3.One;
    Vector4 _clear;
    bool _opaque;
    bool _gestures = true;
    bool _autoRotate;
    bool _gesturing;
    int _mode;
    int _primary = int.MinValue;
    float _lastX;
    float _lastY;
    float _lastMidX;
    float _lastMidY;
    float _pinchSpan = 1f;
    float _pinchDistance = 1f;

    public event EventHandler? Changed;

    public event EventHandler<OrbitChangedEventArgs>? Settled;

    public bool AutoRotateEnabled
    {
        get
        {
            lock (_gate)
                return _autoRotate;
        }
    }

    public void SetMesh(Mesh mesh)
    {
        var changed = false;
        lock (_gate)
        {
            if (ReferenceEquals(_mesh, mesh))
                return;

            _mesh = mesh;
            _generation++;
            if (!_distanceHeld)
                _distance = OrbitMath.FrameDistance(VisualRadius());
            if (!_targetHeld)
                _target = mesh.Center;
            changed = true;
        }

        if (changed)
            Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetYaw(double degrees)
        => SetBound(ref _boundYaw, degrees, () => _yaw = (float)degrees);

    public void SetPitch(double degrees)
        => SetBound(ref _boundPitch, degrees, () => _pitch = Math.Clamp((float)degrees, MinPitch, MaxPitch));

    public void SetDistance(double distance)
    {
        var changed = false;
        lock (_gate)
        {
            if (_boundDistance.Equals(distance))
                return;

            _boundDistance = distance;
            if (double.IsNaN(distance))
            {
                _distanceHeld = false;
                _distance = OrbitMath.FrameDistance(VisualRadius());
            }
            else
            {
                _distanceHeld = true;
                _distance = MathF.Max((float)distance, 0.001f);
            }

            changed = true;
        }

        if (changed)
            Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetTarget(LookTarget target)
    {
        var changed = false;
        lock (_gate)
        {
            if (_boundTarget.Equals(target))
                return;

            _boundTarget = target;
            if (target.IsUnset)
            {
                _targetHeld = false;
                _target = _mesh.Center;
            }
            else
            {
                _targetHeld = true;
                _target = new Vector3((float)target.X, (float)target.Y, (float)target.Z);
            }

            changed = true;
        }

        if (changed)
            Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetRotation(ModelRotation rotation)
    {
        var changed = false;
        lock (_gate)
        {
            if (_boundRotation.Equals(rotation))
                return;

            _boundRotation = rotation;
            _rotYaw = (float)rotation.Yaw;
            _rotPitch = (float)rotation.Pitch;
            _rotRoll = (float)rotation.Roll;
            changed = true;
        }

        if (changed)
            Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetScale(double scale)
    {
        var changed = false;
        lock (_gate)
        {
            if (_boundScale.Equals(scale))
                return;

            _boundScale = scale;
            _scale = MathF.Max((float)scale, 0.0001f);
            if (!_distanceHeld)
                _distance = OrbitMath.FrameDistance(VisualRadius());
            changed = true;
        }

        if (changed)
            Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetTint(float r, float g, float b)
    {
        var changed = false;
        lock (_gate)
        {
            if (_tintR.Equals(r) && _tintG.Equals(g) && _tintB.Equals(b))
                return;

            _tintR = r;
            _tintG = g;
            _tintB = b;
            var linear = ColorSpace.Linear(r, g, b);
            _tint = new Vector3(linear.R, linear.G, linear.B);
            changed = true;
        }

        if (changed)
            Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetClear(float r, float g, float b, float a)
    {
        var changed = false;
        lock (_gate)
        {
            if (_clearR.Equals(r) && _clearG.Equals(g) && _clearB.Equals(b) && _clearA.Equals(a))
                return;

            _clearR = r;
            _clearG = g;
            _clearB = b;
            _clearA = a;
            var clear = ColorSpace.LinearPremultiplied(r, g, b, a);
            _clear = new Vector4(clear.R, clear.G, clear.B, clear.A);
            _opaque = a >= 1f;
            changed = true;
        }

        if (changed)
            Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetGestures(bool enabled)
    {
        var changed = false;
        lock (_gate)
        {
            if (_gesturesCompared && _boundGestures == enabled)
                return;

            _gesturesCompared = true;
            _boundGestures = enabled;
            _gestures = enabled;
            if (!enabled)
                EndGesture();
            changed = true;
        }

        if (changed)
            Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetAutoRotate(bool enabled)
    {
        var changed = false;
        lock (_gate)
        {
            if (_spinCompared && _boundSpin == enabled)
                return;

            _spinCompared = true;
            _boundSpin = enabled;
            _autoRotate = enabled;
            changed = true;
        }

        if (changed)
            Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Advance(float seconds)
    {
        if (seconds <= 0f)
            return;

        lock (_gate)
        {
            if (!_autoRotate || _gesturing)
                return;

            _yaw += SpinDegreesPerSecond * seconds;
        }
    }

    public void SetPointers(ReadOnlySpan<PointerSample> samples, float width, float height)
    {
        Orbit? settled = null;
        var changed = false;
        lock (_gate)
        {
            if (!_gestures || width < 1f || height < 1f)
                return;

            if (samples.Length == 0)
            {
                if (_gesturing)
                {
                    EndGesture();
                    settled = CurrentOrbit();
                    changed = true;
                }
            }
            else
            {
                changed = Move(samples, width, height);
            }
        }

        if (changed)
            Changed?.Invoke(this, EventArgs.Empty);
        if (settled is { } orbit)
            Settled?.Invoke(this, new OrbitChangedEventArgs(orbit));
    }

    public ObjectDraw Capture(float aspect, bool openGlClip)
    {
        lock (_gate)
        {
            var model = ModelMatrix();
            var focus = Focus(model);
            var eye = OrbitMath.Eye(focus, _yaw, _pitch, _distance);
            var view = Matrix4x4.CreateLookAt(eye, focus, Vector3.UnitY);
            var projection = OrbitMath.Projection(aspect, _distance, VisualRadius(), openGlClip);
            return new ObjectDraw(
                _mesh,
                _generation,
                model * view * projection,
                model * view,
                _tint,
                _clear,
                _opaque,
                _autoRotate);
        }
    }

    void SetBound(ref double bound, double value, Action apply)
    {
        var changed = false;
        lock (_gate)
        {
            if (bound.Equals(value))
                return;

            bound = value;
            apply();
            changed = true;
        }

        if (changed)
            Changed?.Invoke(this, EventArgs.Empty);
    }

    bool Move(ReadOnlySpan<PointerSample> samples, float width, float height)
    {
        _gesturing = true;
        if (samples.Length == 1)
            return DragOne(samples[0], width, height);

        return DragTwo(samples[0], samples[1], height);
    }

    bool DragOne(PointerSample pointer, float width, float height)
    {
        if (_mode != 1 || _primary != pointer.Id)
        {
            _mode = 1;
            _primary = pointer.Id;
            _lastX = pointer.X;
            _lastY = pointer.Y;
            return false;
        }

        var dx = pointer.X - _lastX;
        var dy = pointer.Y - _lastY;
        _lastX = pointer.X;
        _lastY = pointer.Y;
        _yaw -= dx / width * DragDegrees;
        _pitch = Math.Clamp(_pitch - (dy / height * DragDegrees), MinPitch, MaxPitch);
        return dx != 0f || dy != 0f;
    }

    bool DragTwo(PointerSample a, PointerSample b, float height)
    {
        var midX = (a.X + b.X) * 0.5f;
        var midY = (a.Y + b.Y) * 0.5f;
        var span = MathF.Max(Vector2.Distance(new Vector2(a.X, a.Y), new Vector2(b.X, b.Y)), 1f);
        if (_mode != 2)
        {
            _mode = 2;
            _lastMidX = midX;
            _lastMidY = midY;
            _pinchSpan = span;
            _pinchDistance = _distance;
            return false;
        }

        var dx = midX - _lastMidX;
        var dy = midY - _lastMidY;
        _lastMidX = midX;
        _lastMidY = midY;
        var radius = MathF.Max(VisualRadius(), 0.001f);
        _distance = Math.Clamp(_pinchDistance * (_pinchSpan / span), radius * 0.1f, radius * 20f);
        _distanceHeld = true;
        Pan(dx, dy, height);
        _targetHeld = true;
        return true;
    }

    void Pan(float dx, float dy, float height)
    {
        var model = ModelMatrix();
        var focus = Focus(model);
        var eye = OrbitMath.Eye(focus, _yaw, _pitch, _distance);
        var forward = focus - eye;
        if (forward.LengthSquared() < 1e-8f)
            return;

        forward = Vector3.Normalize(forward);
        var right = Vector3.Cross(forward, Vector3.UnitY);
        right = right.LengthSquared() < 1e-8f ? Vector3.UnitX : Vector3.Normalize(right);
        var up = Vector3.Normalize(Vector3.Cross(right, forward));
        var worldPerPixel = _distance * MathF.Tan(OrbitMath.Radians(OrbitMath.FovDegrees) * 0.5f) * 2f / height;
        var next = focus + (right * (dx * worldPerPixel)) - (up * (dy * worldPerPixel));
        _target = next;
    }

    void EndGesture()
    {
        _gesturing = false;
        _mode = 0;
        _primary = int.MinValue;
    }

    Matrix4x4 ModelMatrix()
    {
        var rotation =
            Matrix4x4.CreateRotationY(OrbitMath.Radians(_rotYaw))
            * Matrix4x4.CreateRotationX(OrbitMath.Radians(_rotPitch))
            * Matrix4x4.CreateRotationZ(OrbitMath.Radians(_rotRoll));
        return rotation * Matrix4x4.CreateScale(_scale);
    }

    Vector3 Focus(Matrix4x4 model)
        => _targetHeld ? _target : Vector3.Transform(_mesh.Center, model);

    float VisualRadius()
        => _mesh.Radius * _scale;

    Orbit CurrentOrbit()
    {
        var focus = Focus(ModelMatrix());
        return new Orbit(_yaw, _pitch, _distance, focus.X, focus.Y, focus.Z);
    }
}
