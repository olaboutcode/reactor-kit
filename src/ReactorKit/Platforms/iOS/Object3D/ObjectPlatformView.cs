using System.Runtime.InteropServices;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using Metal;
using MetalKit;
using Reactor.Kit;
using Reactor.Kit.ObjectViewTokens;
using UIKit;

namespace Reactor.Kit.Internals.Object3D;

sealed class ObjectPlatformView : MTKView, IMTKViewDelegate
{
    const string Shader = """
        #include <metal_stdlib>
        using namespace metal;

        struct VertexIn {
            float3 position [[attribute(0)]];
            float3 normal [[attribute(1)]];
            float2 uv [[attribute(2)]];
        };

        struct VertexOut {
            float4 position [[position]];
            float3 normal;
            float2 uv;
        };

        struct Uniforms {
            float4x4 modelViewProjection;
            float4x4 modelView;
            float4 tint;
            float4 groupColor;
        };

        vertex VertexOut object_vertex(VertexIn in [[stage_in]], constant Uniforms& uniforms [[buffer(1)]]) {
            VertexOut out;
            out.position = uniforms.modelViewProjection * float4(in.position, 1.0);
            out.normal = (uniforms.modelView * float4(in.normal, 0.0)).xyz;
            out.uv = in.uv;
            return out;
        }

        fragment float4 object_fragment(
            VertexOut in [[stage_in]],
            constant Uniforms& uniforms [[buffer(1)]],
            texture2d<float> albedoTex [[texture(0)]],
            sampler albedoSampler [[sampler(0)]]) {
            float3 normal = normalize(in.normal);
            float3 lightDir = normalize(float3(0.4, 0.8, 0.6));
            float ndotl = saturate(dot(normal, lightDir));
            float3 halfVector = normalize(lightDir + float3(0.0, 0.0, 1.0));
            float specular = pow(saturate(dot(normal, halfVector)), 32.0) * 0.25;
            float3 texel = albedoTex.sample(albedoSampler, in.uv).rgb;
            float3 albedo = texel * uniforms.tint.rgb * uniforms.groupColor.rgb;
            return float4(albedo * (0.25 + 0.75 * ndotl) + specular, 1.0);
        }
        """;

    readonly Dictionary<IntPtr, int> _ids = [];
    readonly Dictionary<IntPtr, CGPoint> _points = [];
    ObjectSurface? _host;
    IMTLCommandQueue? _commands;
    IMTLRenderPipelineState? _pipeline;
    IMTLDepthStencilState? _depth;
    IMTLBuffer? _vertices;
    IMTLBuffer? _indices;
    IMTLTexture? _white;
    IMTLTexture?[] _textures = [];
    IMTLSamplerState? _sampler;
    int _generation;
    static readonly byte[] WhiteRgba = [255, 255, 255, 255];
    int _nextId;
    double _lastTime;
    bool _pipelineReady;

    public ObjectPlatformView()
    {
        var device = MTLDevice.SystemDefault;
        Device = device;
        Delegate = this;
        ColorPixelFormat = MTLPixelFormat.BGRA8Unorm_sRGB;
        DepthStencilPixelFormat = MTLPixelFormat.Depth32Float;
        SampleCount = 4;
        Paused = true;
        EnableSetNeedsDisplay = true;
        PreferredFramesPerSecond = 60;
        MultipleTouchEnabled = true;
        ExclusiveTouch = true;
        Opaque = false;
        _commands = device?.CreateCommandQueue();
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
        OnFrame(this, EventArgs.Empty);
    }

    public void Unbind()
    {
        if (_host is not null)
            _host.FrameInvalidated -= OnFrame;
        _host = null;
        _pipeline?.Dispose();
        _depth?.Dispose();
        _vertices?.Dispose();
        _indices?.Dispose();
        ReleaseTextures();
        _white?.Dispose();
        _sampler?.Dispose();
        _commands?.Dispose();
        _pipeline = null;
        _depth = null;
        _vertices = null;
        _indices = null;
        _white = null;
        _sampler = null;
        _commands = null;
        _generation = 0;
    }

    public override void MovedToWindow()
    {
        base.MovedToWindow();
        RequestDraw();
    }

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();
        RequestDraw();
    }

    public void DrawableSizeWillChange(MTKView view, CGSize size)
        => RequestDraw();

    public void Draw(MTKView view)
    {
        if (_host is null || Device is null || _commands is null)
            return;

        var drawable = CurrentDrawable;
        var pass = CurrentRenderPassDescriptor;
        if (drawable is null || pass is null)
            return;

        var aspect = (float)(DrawableSize.Width / Math.Max(DrawableSize.Height, 1));
        if (_host.Pose.AutoRotateEnabled)
        {
            var time = NSProcessInfo.ProcessInfo.SystemUptime;
            if (_lastTime > 0)
                _host.Pose.Advance((float)Math.Min(time - _lastTime, 0.1));
            _lastTime = time;
        }

        var draw = _host.Pose.Capture(aspect, openGlClip: false);
        ApplyClear(pass, draw, SampleCount);
        Opaque = draw.Opaque;
        if (!EnsurePipeline() || draw.Mesh.IsEmpty)
        {
            EncodeClear(pass, drawable);
            return;
        }

        EnsureBuffers(draw);
        if (_vertices is null || _indices is null || _pipeline is null || _depth is null)
        {
            EncodeClear(pass, drawable);
            return;
        }

        Encode(pass, drawable, draw);
    }

    public override void TouchesBegan(NSSet touches, UIEvent? evt)
    {
        Remember(touches);
        base.TouchesBegan(touches, evt);
    }

    public override void TouchesMoved(NSSet touches, UIEvent? evt)
    {
        Remember(touches);
        base.TouchesMoved(touches, evt);
    }

    public override void TouchesEnded(NSSet touches, UIEvent? evt)
    {
        Forget(touches);
        base.TouchesEnded(touches, evt);
    }

    public override void TouchesCancelled(NSSet touches, UIEvent? evt)
    {
        Forget(touches);
        base.TouchesCancelled(touches, evt);
    }

    void OnFrame(object? sender, EventArgs e)
    {
        var auto = _host?.Pose.AutoRotateEnabled ?? false;
        Paused = !auto;
        EnableSetNeedsDisplay = !auto;
        if (!auto)
        {
            _lastTime = 0;
            RequestDraw();
        }
    }

    void RequestDraw()
    {
        if (Window is null || Bounds.Width <= 0 || Bounds.Height <= 0)
            return;

        SetNeedsDisplay();
    }

    void Remember(NSSet touches)
    {
        foreach (UITouch touch in touches)
        {
            var handle = touch.Handle;
            if (!_ids.ContainsKey(handle))
                _ids[handle] = _nextId++;
            _points[handle] = touch.LocationInView(this);
        }

        Publish();
    }

    void Forget(NSSet touches)
    {
        foreach (UITouch touch in touches)
        {
            var handle = touch.Handle;
            _ids.Remove(handle);
            _points.Remove(handle);
        }

        Publish();
    }

    void Publish()
    {
        if (_host is null)
            return;

        var samples = new PointerSample[_points.Count];
        var index = 0;
        foreach (var pair in _points)
            samples[index++] = new PointerSample(_ids[pair.Key], (float)pair.Value.X, (float)pair.Value.Y);
        _host.Pose.SetPointers(samples, (float)Bounds.Width, (float)Bounds.Height);
    }

    bool EnsurePipeline()
    {
        if (_pipelineReady)
            return _pipeline is not null;
        _pipelineReady = true;
        if (Device is null)
            return false;

        var library = Device.CreateLibrary(Shader, new MTLCompileOptions(), out var error);
        if (library is null || error is not null)
            return false;

        using var vertices = VertexLayout();
        using var descriptor = new MTLRenderPipelineDescriptor
        {
            VertexFunction = library.CreateFunction("object_vertex"),
            FragmentFunction = library.CreateFunction("object_fragment"),
            VertexDescriptor = vertices,
            DepthAttachmentPixelFormat = DepthStencilPixelFormat,
        };
        descriptor.ColorAttachments[0].PixelFormat = ColorPixelFormat;
        ApplySampleCount(descriptor, SampleCount);
        _pipeline = Device.CreateRenderPipelineState(descriptor, out var pipelineError);
        if (_pipeline is null || pipelineError is not null)
        {
            SampleCount = 1;
            ApplySampleCount(descriptor, 1);
            _pipeline = Device.CreateRenderPipelineState(descriptor, out _);
        }

        using var depth = new MTLDepthStencilDescriptor
        {
            DepthCompareFunction = MTLCompareFunction.LessEqual,
            DepthWriteEnabled = true,
        };
        _depth = Device.CreateDepthStencilState(depth);
        library.Dispose();
        return _pipeline is not null && _depth is not null;
    }

    void EnsureBuffers(ObjectDraw draw)
    {
        if (Device is null || _generation == draw.Generation)
            return;

        _vertices?.Dispose();
        _indices?.Dispose();
        ReleaseTextures();
        _vertices = Device.CreateBuffer(FloatBytes(draw.Mesh.Interleaved), MTLResourceOptions.StorageModeShared);
        _indices = Device.CreateBuffer(IndexBytes(draw.Mesh.Indices), MTLResourceOptions.StorageModeShared);
        _sampler ??= Device.CreateSamplerState(new MTLSamplerDescriptor
        {
            MinFilter = MTLSamplerMinMagFilter.Linear,
            MagFilter = MTLSamplerMinMagFilter.Linear,
            SAddressMode = MTLSamplerAddressMode.Repeat,
            TAddressMode = MTLSamplerAddressMode.Repeat,
        });
        _white ??= Upload(1, 1, WhiteRgba);
        _textures = new IMTLTexture?[draw.Mesh.Images.Length];
        for (var i = 0; i < draw.Mesh.Images.Length; i++)
        {
            var image = draw.Mesh.Images[i];
            _textures[i] = Upload(image.Width, image.Height, image.Rgba);
        }

        _generation = draw.Generation;
    }

    IMTLTexture? Upload(int width, int height, byte[] rgba)
    {
        if (Device is null || width <= 0 || height <= 0 || rgba.Length < width * height * 4)
            return null;

        using var descriptor = MTLTextureDescriptor.CreateTexture2DDescriptor(
            MTLPixelFormat.RGBA8Unorm_sRGB,
            (nuint)width,
            (nuint)height,
            false);
        descriptor.Usage = MTLTextureUsage.ShaderRead;
        descriptor.StorageMode = MTLStorageMode.Shared;
        var texture = Device.CreateTexture(descriptor);
        if (texture is null)
            return null;

        using var data = NSData.FromArray(rgba);
        texture.ReplaceRegion(
            MTLRegion.Create2D(0, 0, (nuint)width, (nuint)height),
            0,
            data.Bytes,
            (nuint)(width * 4));
        return texture;
    }

    void ReleaseTextures()
    {
        foreach (var texture in _textures)
            texture?.Dispose();
        _textures = [];
    }

    IMTLTexture? TextureFor(int index)
    {
        if ((uint)index < (uint)_textures.Length && _textures[index] is not null)
            return _textures[index];

        return _white;
    }

    void Encode(MTLRenderPassDescriptor pass, ICAMetalDrawable drawable, ObjectDraw draw)
    {
        var pipeline = _pipeline;
        var depth = _depth;
        var vertices = _vertices;
        var indices = _indices;
        var sampler = _sampler;
        if (pipeline is null || depth is null || vertices is null || indices is null || sampler is null)
            return;

        using var buffer = _commands!.CommandBuffer()!;
        using var encoder = buffer.CreateRenderCommandEncoder(pass)!;
        encoder.SetRenderPipelineState(pipeline);
        encoder.SetDepthStencilState(depth);
        encoder.SetCullMode(MTLCullMode.Back);
        encoder.SetFrontFacingWinding(MTLWinding.CounterClockwise);
        encoder.SetVertexBuffer(vertices, 0, 0);
        encoder.SetFragmentSamplerState(sampler, 0);
        foreach (var group in draw.Mesh.Groups)
        {
            var texture = TextureFor(group.Texture);
            if (texture is null)
                continue;

            encoder.SetFragmentTexture(texture, 0);
            SetShadeBytes(encoder, ShadeUniforms.Pack(draw, group.Color));
            encoder.DrawIndexedPrimitives(
                MTLPrimitiveType.Triangle,
                (nuint)group.IndexCount,
                MTLIndexType.UInt32,
                indices,
                (nuint)(group.IndexOffset * 4));
        }

        encoder.EndEncoding();
        buffer.PresentDrawable(drawable);
        buffer.Commit();
    }

    void EncodeClear(MTLRenderPassDescriptor pass, ICAMetalDrawable drawable)
    {
        using var buffer = _commands!.CommandBuffer()!;
        using var encoder = buffer.CreateRenderCommandEncoder(pass)!;
        encoder.EndEncoding();
        buffer.PresentDrawable(drawable);
        buffer.Commit();
    }

    static void SetShadeBytes(IMTLRenderCommandEncoder encoder, float[] values)
    {
        var bytes = Bytes(values);
        var pin = Marshal.AllocHGlobal(bytes.Length);
        try
        {
            Marshal.Copy(bytes, 0, pin, bytes.Length);
            encoder.SetVertexBytes(pin, (nuint)bytes.Length, 1);
            encoder.SetFragmentBytes(pin, (nuint)bytes.Length, 1);
        }
        finally
        {
            Marshal.FreeHGlobal(pin);
        }
    }

    static MTLVertexDescriptor VertexLayout()
    {
        var layout = new MTLVertexDescriptor();
        var position = layout.Attributes[0];
        position.Format = MTLVertexFormat.Float3;
        position.Offset = 0;
        position.BufferIndex = 0;
        var normal = layout.Attributes[1];
        normal.Format = MTLVertexFormat.Float3;
        normal.Offset = 12;
        normal.BufferIndex = 0;
        var uv = layout.Attributes[2];
        uv.Format = MTLVertexFormat.Float2;
        uv.Offset = 24;
        uv.BufferIndex = 0;
        var buffer = layout.Layouts[0];
        buffer.Stride = Mesh.VertexStride;
        buffer.StepFunction = MTLVertexStepFunction.PerVertex;
        buffer.StepRate = 1;
        return layout;
    }

    static void ApplySampleCount(MTLRenderPipelineDescriptor descriptor, nuint count)
    {
        if (OperatingSystem.IsIOSVersionAtLeast(16) || OperatingSystem.IsMacCatalystVersionAtLeast(16))
            descriptor.RasterSampleCount = count;
        else
            descriptor.SampleCount = count;
    }

    static void ApplyClear(MTLRenderPassDescriptor pass, ObjectDraw draw, nuint sampleCount)
    {
        pass.ColorAttachments[0].ClearColor = new MTLClearColor(draw.Clear.X, draw.Clear.Y, draw.Clear.Z, draw.Clear.W);
        pass.ColorAttachments[0].LoadAction = MTLLoadAction.Clear;
        // A 4x pass resolves into the drawable. Store leaves that drawable black.
        pass.ColorAttachments[0].StoreAction = sampleCount > 1
            ? MTLStoreAction.MultisampleResolve
            : MTLStoreAction.Store;
        if (pass.DepthAttachment is not null)
        {
            pass.DepthAttachment.ClearDepth = 1;
            pass.DepthAttachment.LoadAction = MTLLoadAction.Clear;
            pass.DepthAttachment.StoreAction = MTLStoreAction.DontCare;
        }
    }

    static byte[] FloatBytes(float[] values)
    {
        var bytes = new byte[values.Length * sizeof(float)];
        Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    static byte[] IndexBytes(int[] values)
        => FloatBytes(values.Length == 0 ? [] : MemoryMarshalInts(values));

    static float[] MemoryMarshalInts(int[] values)
    {
        var floats = new float[values.Length];
        Buffer.BlockCopy(values, 0, floats, 0, values.Length * sizeof(int));
        return floats;
    }

    static byte[] Bytes(float[] values)
        => FloatBytes(values);
}
