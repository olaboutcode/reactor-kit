using System.Diagnostics;
using System.Runtime.InteropServices;
using Android.Graphics;
using Android.Opengl;
using Android.OS;
using Android.Views;
using Java.Nio;
using Reactor.Kit;
using AContext = Android.Content.Context;

namespace Reactor.Kit.Internals.Object3D;

sealed class ObjectPlatformView : TextureView, TextureView.ISurfaceTextureListener
{
    const int ColorSpaceSrgb = 0x3089;
    const int GlColorspace = 0x309D;

    const string VertexShader = """
        #version 300 es
        layout(location = 0) in vec3 aPosition;
        layout(location = 1) in vec3 aNormal;
        uniform mat4 uMvp;
        uniform mat4 uModelView;
        out vec3 vNormal;
        void main() {
            gl_Position = uMvp * vec4(aPosition, 1.0);
            vNormal = mat3(uModelView) * aNormal;
        }
        """;

    const string FragmentShader = """
        #version 300 es
        precision highp float;
        in vec3 vNormal;
        uniform vec3 uTint;
        uniform vec3 uGroup;
        out vec4 oColor;
        void main() {
            vec3 normal = normalize(vNormal);
            vec3 lightDir = normalize(vec3(0.4, 0.8, 0.6));
            float ndotl = clamp(dot(normal, lightDir), 0.0, 1.0);
            vec3 halfVector = normalize(lightDir + vec3(0.0, 0.0, 1.0));
            float specular = pow(clamp(dot(normal, halfVector), 0.0, 1.0), 32.0) * 0.25;
            vec3 albedo = uTint * uGroup;
            oColor = vec4(albedo * (0.25 + 0.75 * ndotl) + specular, 1.0);
        }
        """;

    readonly HandlerThread _thread;
    readonly Handler _queue;
    ObjectSurface? _host;
    EGLDisplay? _display;
    EGLSurface? _surface;
    EGLContext? _context;
    Surface? _window;
    int _program;
    int _mvp;
    int _modelView;
    int _tint;
    int _group;
    int _vertices;
    int _indices;
    int _frame;
    int _color;
    int _depth;
    int _generation;
    int _width;
    int _height;
    int _posted;
    long _lastTicks;
    bool _msaa;
    bool _stopped;

    public ObjectPlatformView(AContext context)
        : base(context)
    {
        SurfaceTextureListener = this;
        Clickable = true;
        SetOpaque(false);
        _thread = new HandlerThread("object-view");
        _thread.Start();
        _queue = new Handler(_thread.Looper!);
    }

    public void Bind(ObjectSurface host)
    {
        if (!ReferenceEquals(_host, host))
        {
            if (_host is not null)
                _host.FrameInvalidated -= OnFrame;
            _host = host;
            host.FrameInvalidated += OnFrame;
        }

        host.Push();
        SetOpaque(host.ClearColor.Alpha >= 1f);
        Request();
    }

    public void Unbind()
    {
        if (_host is not null)
            _host.FrameInvalidated -= OnFrame;
        _host = null;
        _stopped = true;
        var done = new ManualResetEventSlim(false);
        _queue.Post(() =>
        {
            ReleaseGl();
            done.Set();
        });
        done.Wait(TimeSpan.FromSeconds(2));
        _thread.QuitSafely();
    }

    public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
    {
        _width = width;
        _height = height;
        _queue.Post(() => CreateGl(surface, width, height));
        Request();
    }

    public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
    {
        _width = width;
        _height = height;
        Request();
    }

    public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
    {
        var done = new ManualResetEventSlim(false);
        _queue.Post(() =>
        {
            ReleaseGl();
            done.Set();
        });
        done.Wait(TimeSpan.FromSeconds(2));
        return true;
    }

    public void OnSurfaceTextureUpdated(SurfaceTexture surface)
    {
    }

    public override bool OnTouchEvent(MotionEvent? e)
    {
        if (e is null || _host is null)
            return false;

        var action = e.ActionMasked;
        if (action is MotionEventActions.Down or MotionEventActions.PointerDown)
            Parent?.RequestDisallowInterceptTouchEvent(true);
        if (action is MotionEventActions.Up or MotionEventActions.Cancel)
        {
            Parent?.RequestDisallowInterceptTouchEvent(false);
            _host.Pose.SetPointers([], Width, Height);
            return true;
        }

        var skip = action == MotionEventActions.PointerUp ? e.ActionIndex : -1;
        var samples = new List<PointerSample>(e.PointerCount);
        for (var i = 0; i < e.PointerCount; i++)
        {
            if (i == skip)
                continue;
            samples.Add(new PointerSample(e.GetPointerId(i), e.GetX(i), e.GetY(i)));
        }

        _host.Pose.SetPointers(CollectionsMarshal.AsSpan(samples), Width, Height);
        return true;
    }

    void OnFrame(object? sender, EventArgs e)
    {
        if (_host is not null)
            SetOpaque(_host.ClearColor.Alpha >= 1f);
        Request();
    }

    void Request()
    {
        if (_stopped || Interlocked.Exchange(ref _posted, 1) == 1)
            return;

        _queue.Post(Run);
    }

    void Run()
    {
        Interlocked.Exchange(ref _posted, 0);
        if (_stopped || _display is null || _surface is null || _context is null)
            return;

        EGL14.EglMakeCurrent(_display, _surface, _surface, _context);
        var auto = DrawFrame();
        EGL14.EglSwapBuffers(_display, _surface);
        if (auto && !_stopped)
            Request();
    }

    bool DrawFrame()
    {
        if (_host is null || _program == 0)
            return false;

        var auto = _host.Pose.AutoRotateEnabled;
        if (auto)
        {
            var now = Stopwatch.GetTimestamp();
            if (_lastTicks != 0)
            {
                var seconds = (now - _lastTicks) / (float)Stopwatch.Frequency;
                _host.Pose.Advance(Math.Min(seconds, 0.1f));
            }

            _lastTicks = now;
        }
        else
        {
            _lastTicks = 0;
        }

        var draw = _host.Pose.Capture(_width / (float)Math.Max(_height, 1), openGlClip: true);
        GLES30.GlBindFramebuffer(GLES30.GlFramebuffer, _msaa ? _frame : 0);
        GLES30.GlViewport(0, 0, _width, _height);
        GLES30.GlClearColor(draw.Clear.X, draw.Clear.Y, draw.Clear.Z, draw.Clear.W);
        GLES30.GlClear(GLES30.GlColorBufferBit | GLES30.GlDepthBufferBit);
        if (!draw.Mesh.IsEmpty)
            DrawMesh(draw);
        if (_msaa)
            Resolve();
        return auto;
    }

    void DrawMesh(ObjectDraw draw)
    {
        EnsureBuffers(draw);
        GLES30.GlUseProgram(_program);
        GLES30.GlBindBuffer(GLES30.GlArrayBuffer, _vertices);
        GLES30.GlBindBuffer(GLES30.GlElementArrayBuffer, _indices);
        GLES30.GlEnableVertexAttribArray(0);
        GLES30.GlEnableVertexAttribArray(1);
        GLES30.GlVertexAttribPointer(0, 3, GLES30.GlFloat, false, 24, 0);
        GLES30.GlVertexAttribPointer(1, 3, GLES30.GlFloat, false, 24, 12);
        var mvp = new float[16];
        var modelView = new float[16];
        MatrixPack.Store(draw.ModelViewProjection, mvp);
        MatrixPack.Store(draw.ModelView, modelView);
        GLES30.GlUniformMatrix4fv(_mvp, 1, false, mvp, 0);
        GLES30.GlUniformMatrix4fv(_modelView, 1, false, modelView, 0);
        GLES30.GlUniform3f(_tint, draw.Tint.X, draw.Tint.Y, draw.Tint.Z);
        foreach (var group in draw.Mesh.Groups)
        {
            GLES30.GlUniform3f(_group, group.Color.X, group.Color.Y, group.Color.Z);
            GLES30.GlDrawElements(GLES30.GlTriangles, group.IndexCount, GLES30.GlUnsignedInt, group.IndexOffset * 4);
        }
    }

    void EnsureBuffers(ObjectDraw draw)
    {
        if (_generation == draw.Generation && _vertices != 0)
            return;

        DeleteBuffers();
        var vertices = new int[1];
        var indices = new int[1];
        GLES30.GlGenBuffers(1, vertices, 0);
        GLES30.GlGenBuffers(1, indices, 0);
        _vertices = vertices[0];
        _indices = indices[0];
        var vertexBuffer = Bytes(draw.Mesh.Interleaved);
        var indexBuffer = Bytes(draw.Mesh.Indices);
        GLES30.GlBindBuffer(GLES30.GlArrayBuffer, _vertices);
        GLES30.GlBufferData(GLES30.GlArrayBuffer, vertexBuffer.Capacity(), vertexBuffer, GLES30.GlStaticDraw);
        GLES30.GlBindBuffer(GLES30.GlElementArrayBuffer, _indices);
        GLES30.GlBufferData(GLES30.GlElementArrayBuffer, indexBuffer.Capacity(), indexBuffer, GLES30.GlStaticDraw);
        _generation = draw.Generation;
    }

    void Resolve()
    {
        GLES30.GlBindFramebuffer(GLES30.GlReadFramebuffer, _frame);
        GLES30.GlBindFramebuffer(GLES30.GlDrawFramebuffer, 0);
        GLES30.GlBlitFramebuffer(0, 0, _width, _height, 0, 0, _width, _height, GLES30.GlColorBufferBit, GLES30.GlNearest);
        GLES30.GlBindFramebuffer(GLES30.GlFramebuffer, 0);
    }

    void CreateGl(SurfaceTexture texture, int width, int height)
    {
        ReleaseGl();
        _display = EGL14.EglGetDisplay(EGL14.EglDefaultDisplay);
        if (_display is null || _display == EGL14.EglNoDisplay)
            return;

        var version = new int[2];
        if (!EGL14.EglInitialize(_display, version, 0, version, 1))
            return;

        var config = ChooseConfig(_display);
        if (config is null)
            return;

        var contextAttribs = new[] { EGL14.EglContextClientVersion, 3, EGL14.EglNone };
        _context = EGL14.EglCreateContext(_display, config, EGL14.EglNoContext, contextAttribs, 0);
        _window = new Surface(texture);
        _surface = CreateSurface(_display, config, _window);
        if (_context is null || _surface is null || _surface == EGL14.EglNoSurface)
            return;

        EGL14.EglMakeCurrent(_display, _surface, _surface, _context);
        _program = Link();
        if (_program == 0)
            return;

        _mvp = GLES30.GlGetUniformLocation(_program, "uMvp");
        _modelView = GLES30.GlGetUniformLocation(_program, "uModelView");
        _tint = GLES30.GlGetUniformLocation(_program, "uTint");
        _group = GLES30.GlGetUniformLocation(_program, "uGroup");
        GLES30.GlEnable(GLES30.GlDepthTest);
        GLES30.GlDepthFunc(GLES30.GlLequal);
        GLES30.GlEnable(GLES30.GlCullFaceConst);
        GLES30.GlCullFace(GLES30.GlBack);
        GLES30.GlFrontFace(GLES30.GlCcw);
        _msaa = CreateMsaa(width, height);
        Request();
    }

    bool CreateMsaa(int width, int height)
    {
        var frame = new int[1];
        var color = new int[1];
        var depth = new int[1];
        GLES30.GlGenFramebuffers(1, frame, 0);
        GLES30.GlGenRenderbuffers(1, color, 0);
        GLES30.GlGenRenderbuffers(1, depth, 0);
        GLES30.GlBindFramebuffer(GLES30.GlFramebuffer, frame[0]);
        GLES30.GlBindRenderbuffer(GLES30.GlRenderbuffer, color[0]);
        GLES30.GlRenderbufferStorageMultisample(GLES30.GlRenderbuffer, 4, GLES30.GlSrgb8Alpha8, width, height);
        GLES30.GlFramebufferRenderbuffer(GLES30.GlFramebuffer, GLES30.GlColorAttachment0, GLES30.GlRenderbuffer, color[0]);
        GLES30.GlBindRenderbuffer(GLES30.GlRenderbuffer, depth[0]);
        GLES30.GlRenderbufferStorageMultisample(GLES30.GlRenderbuffer, 4, GLES30.GlDepthComponent24, width, height);
        GLES30.GlFramebufferRenderbuffer(GLES30.GlFramebuffer, GLES30.GlDepthAttachment, GLES30.GlRenderbuffer, depth[0]);
        var complete = GLES30.GlCheckFramebufferStatus(GLES30.GlFramebuffer) == GLES30.GlFramebufferComplete;
        if (!complete)
        {
            GLES30.GlDeleteFramebuffers(1, frame, 0);
            GLES30.GlDeleteRenderbuffers(1, color, 0);
            GLES30.GlDeleteRenderbuffers(1, depth, 0);
            GLES30.GlBindFramebuffer(GLES30.GlFramebuffer, 0);
            return false;
        }

        _frame = frame[0];
        _color = color[0];
        _depth = depth[0];
        return true;
    }

    static EGLConfig? ChooseConfig(EGLDisplay display)
    {
        var attribs = new[]
        {
            EGL14.EglRenderableType, EGL14.EglOpenglEs2Bit,
            EGL14.EglRedSize, 8,
            EGL14.EglGreenSize, 8,
            EGL14.EglBlueSize, 8,
            EGL14.EglAlphaSize, 8,
            EGL14.EglDepthSize, 24,
            EGL14.EglNone,
        };
        var configs = new EGLConfig[1];
        var count = new int[1];
        if (!EGL14.EglChooseConfig(display, attribs, 0, configs, 0, 1, count, 0) || count[0] == 0)
            return null;
        return configs[0];
    }

    static EGLSurface? CreateSurface(EGLDisplay display, EGLConfig config, Surface window)
    {
        var srgb = new[] { GlColorspace, ColorSpaceSrgb, EGL14.EglNone };
        var surface = EGL14.EglCreateWindowSurface(display, config, window, srgb, 0);
        if (surface is not null && surface != EGL14.EglNoSurface)
            return surface;

        return EGL14.EglCreateWindowSurface(display, config, window, new[] { EGL14.EglNone }, 0);
    }

    static int Link()
    {
        var vertex = Compile(GLES30.GlVertexShader, VertexShader);
        var fragment = Compile(GLES30.GlFragmentShader, FragmentShader);
        if (vertex == 0 || fragment == 0)
            return 0;

        var program = GLES30.GlCreateProgram();
        GLES30.GlAttachShader(program, vertex);
        GLES30.GlAttachShader(program, fragment);
        GLES30.GlLinkProgram(program);
        GLES30.GlDeleteShader(vertex);
        GLES30.GlDeleteShader(fragment);
        var status = new int[1];
        GLES30.GlGetProgramiv(program, GLES30.GlLinkStatus, status, 0);
        if (status[0] == 0)
        {
            GLES30.GlDeleteProgram(program);
            return 0;
        }

        return program;
    }

    static int Compile(int type, string source)
    {
        var shader = GLES30.GlCreateShader(type);
        GLES30.GlShaderSource(shader, source);
        GLES30.GlCompileShader(shader);
        var status = new int[1];
        GLES30.GlGetShaderiv(shader, GLES30.GlCompileStatus, status, 0);
        if (status[0] != 0)
            return shader;

        GLES30.GlDeleteShader(shader);
        return 0;
    }

    void DeleteBuffers()
    {
        if (_vertices != 0)
        {
            GLES30.GlDeleteBuffers(1, [_vertices], 0);
            _vertices = 0;
        }

        if (_indices != 0)
        {
            GLES30.GlDeleteBuffers(1, [_indices], 0);
            _indices = 0;
        }
    }

    void ReleaseGl()
    {
        if (_display is null)
            return;

        EGL14.EglMakeCurrent(_display, EGL14.EglNoSurface, EGL14.EglNoSurface, EGL14.EglNoContext);
        DeleteBuffers();
        if (_frame != 0)
        {
            GLES30.GlDeleteFramebuffers(1, [_frame], 0);
            GLES30.GlDeleteRenderbuffers(1, [_color], 0);
            GLES30.GlDeleteRenderbuffers(1, [_depth], 0);
            _frame = 0;
        }

        if (_program != 0)
        {
            GLES30.GlDeleteProgram(_program);
            _program = 0;
        }

        if (_surface is not null && _surface != EGL14.EglNoSurface)
            EGL14.EglDestroySurface(_display, _surface);
        if (_context is not null && _context != EGL14.EglNoContext)
            EGL14.EglDestroyContext(_display, _context);
        EGL14.EglTerminate(_display);
        _window?.Release();
        _window?.Dispose();
        _surface = null;
        _context = null;
        _display = null;
        _window = null;
        _generation = 0;
        _msaa = false;
    }

    static ByteBuffer Bytes(float[] values)
    {
        var buffer = ByteBuffer.AllocateDirect(values.Length * sizeof(float)).Order(ByteOrder.NativeOrder()!)!;
        var floats = buffer.AsFloatBuffer()!;
        floats.Put(values);
        buffer.Position(0);
        return buffer;
    }

    static ByteBuffer Bytes(int[] values)
    {
        var buffer = ByteBuffer.AllocateDirect(values.Length * sizeof(int)).Order(ByteOrder.NativeOrder()!)!;
        var ints = buffer.AsIntBuffer()!;
        ints.Put(values);
        buffer.Position(0);
        return buffer;
    }
}
