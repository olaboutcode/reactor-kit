using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Reactor.Kit;
using Reactor.Kit.ObjectViewTokens;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using WinRT;

namespace Reactor.Kit.Internals.Object3D;

sealed class ObjectPlatformView : SwapChainPanel
{
    const string Shader = """
        cbuffer Shade : register(b0)
        {
            float4x4 modelViewProjection;
            float4x4 modelView;
            float4 tint;
            float4 groupColor;
            float4 surface;
        };

        Texture2D albedoTex : register(t0);
        SamplerState albedoSampler : register(s0);

        struct VertexIn
        {
            float3 position : POSITION;
            float3 normal : NORMAL;
            float2 uv : TEXCOORD0;
        };

        struct VertexOut
        {
            float4 position : SV_Position;
            float3 normal : NORMAL;
            float2 uv : TEXCOORD0;
        };

        struct PixelIn
        {
            float4 position : SV_Position;
            float3 normal : NORMAL;
            float2 uv : TEXCOORD0;
            bool front : SV_IsFrontFace;
        };

        VertexOut object_vertex(VertexIn input)
        {
            VertexOut output;
            output.position = mul(modelViewProjection, float4(input.position, 1.0));
            output.normal = mul((float3x3)modelView, input.normal);
            output.uv = input.uv;
            return output;
        }

        float4 object_fragment(PixelIn input) : SV_Target
        {
            float4 texel = albedoTex.Sample(albedoSampler, input.uv);
            float alpha = texel.a * groupColor.a;
            if (surface.y > 0.5 && alpha < surface.x)
                discard;
            float3 normal = normalize(input.normal);
            if (surface.z > 0.5 && !input.front)
                normal = -normal;
            float3 lightDir = normalize(float3(0.4, 0.8, 0.6));
            float ndotl = saturate(dot(normal, lightDir));
            float3 halfVector = normalize(lightDir + float3(0.0, 0.0, 1.0));
            float specular = pow(saturate(dot(normal, halfVector)), 32.0) * 0.25;
            float3 albedo = texel.rgb * tint.rgb * groupColor.rgb;
            return float4(albedo * (0.25 + 0.75 * ndotl) + specular, 1.0);
        }
        """;

    readonly Dictionary<uint, (float X, float Y)> _points = [];
    Object3DSurface? _host;
    ID3D11Device? _device;
    ID3D11DeviceContext? _context;
    IDXGISwapChain1? _swapChain;
    ID3D11Texture2D? _backBuffer;
    ID3D11Texture2D? _msaa;
    ID3D11Texture2D? _depth;
    ID3D11RenderTargetView? _backView;
    ID3D11RenderTargetView? _msaaView;
    ID3D11DepthStencilView? _depthView;
    ID3D11VertexShader? _vertexShader;
    ID3D11PixelShader? _pixelShader;
    ID3D11InputLayout? _layout;
    ID3D11RasterizerState? _raster;
    ID3D11RasterizerState? _rasterNone;
    ID3D11DepthStencilState? _depthState;
    ID3D11Buffer? _vertices;
    ID3D11Buffer? _indices;
    ID3D11Buffer? _constants;
    ID3D11SamplerState? _sampler;
    ID3D11Texture2D? _whiteTexture;
    ID3D11ShaderResourceView? _whiteView;
    ID3D11Texture2D?[] _colorTextures = [];
    ID3D11ShaderResourceView?[] _colorViews = [];
    static readonly byte[] WhiteRgba = [255, 255, 255, 255];
    EventHandler<object>? _rendering;
    uint _width;
    uint _height;
    uint _samples = 1;
    int _generation;
    long _lastTicks;
    bool _pipelineReady;

    public ObjectPlatformView()
    {
        PointerPressed += OnPressed;
        PointerMoved += OnMoved;
        PointerReleased += OnReleased;
        PointerCanceled += OnReleased;
        PointerCaptureLost += OnReleased;
        SizeChanged += (_, _) => EnsureSize();
        CompositionScaleChanged += (_, _) => EnsureSize();
        Loaded += (_, _) => EnsureSize();
        CreateDevice();
    }

    public void Bind(Object3DSurface host)
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
        StopLoop();
        _points.Clear();
        ReleaseDevice();
    }

    void OnFrame(object? sender, EventArgs e)
    {
        if (_host?.Pose.AutoRotateEnabled == true)
            EnsureLoop();
        else
        {
            StopLoop();
            Draw(advance: false);
        }
    }

    void OnRendering(object? sender, object e)
        => Draw(advance: true);

    void OnPressed(object sender, PointerRoutedEventArgs e)
    {
        CapturePointer(e.Pointer);
        Remember(e);
        e.Handled = true;
    }

    void OnMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_points.ContainsKey(e.Pointer.PointerId))
            return;

        Remember(e);
        e.Handled = true;
    }

    void OnReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_points.Remove(e.Pointer.PointerId))
            return;

        ReleasePointerCapture(e.Pointer);
        Publish();
        e.Handled = true;
    }

    void Remember(PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(this).Position;
        _points[e.Pointer.PointerId] = ((float)point.X, (float)point.Y);
        Publish();
    }

    void Publish()
    {
        if (_host is null || ActualWidth < 1 || ActualHeight < 1)
            return;

        var samples = new PointerSample[_points.Count];
        var index = 0;
        foreach (var pair in _points)
            samples[index++] = new PointerSample((int)pair.Key, pair.Value.X, pair.Value.Y);
        _host.Pose.SetPointers(samples, (float)ActualWidth, (float)ActualHeight);
    }

    void EnsureLoop()
    {
        if (_rendering is not null)
            return;

        _lastTicks = 0;
        _rendering = OnRendering;
        CompositionTarget.Rendering += _rendering;
    }

    void StopLoop()
    {
        if (_rendering is null)
            return;

        CompositionTarget.Rendering -= _rendering;
        _rendering = null;
        _lastTicks = 0;
    }

    void CreateDevice()
    {
        var result = D3D11.D3D11CreateDevice(
            IntPtr.Zero,
            DriverType.Hardware,
            DeviceCreationFlags.BgraSupport,
            [FeatureLevel.Level_11_0],
            out ID3D11Device device,
            out ID3D11DeviceContext context);
        if (result.Failure)
            return;

        _device = device;
        _context = context;
        _samples = device.CheckMultisampleQualityLevels(Format.B8G8R8A8_UNorm_SRgb, 4) > 0 ? 4u : 1u;
    }

    void EnsureSize()
    {
        var width = Pixels(ActualWidth, CompositionScaleX);
        var height = Pixels(ActualHeight, CompositionScaleY);
        if (width == 0 || height == 0 || _device is null)
            return;
        if (_swapChain is not null && width == _width && height == _height)
        {
            ApplyScale();
            return;
        }

        ReleaseTargets();
        if (_swapChain is null)
            CreateSwapChain(width, height);
        else
            _swapChain.ResizeBuffers(0, width, height, Format.Unknown);

        if (_swapChain is null)
            return;

        CreateTargets(width, height);
        _width = width;
        _height = height;
        ApplyScale();
        Draw(advance: false);
    }

    void ApplyScale()
    {
        if (_swapChain is null)
            return;

        var scaleX = CompositionScaleX <= 0 ? 1d : CompositionScaleX;
        var scaleY = CompositionScaleY <= 0 ? 1d : CompositionScaleY;
        using var scaled = _swapChain.QueryInterface<IDXGISwapChain2>();
        scaled.MatrixTransform = new System.Numerics.Matrix3x2((float)(1d / scaleX), 0, 0, (float)(1d / scaleY), 0, 0);
    }

    void CreateSwapChain(uint width, uint height)
    {
        if (_device is null)
            return;

        using var dxgi = _device.QueryInterface<IDXGIDevice>();
        using var adapter = dxgi.GetAdapter();
        using var factory = adapter.GetParent<IDXGIFactory2>();
        _swapChain = factory.CreateSwapChainForComposition(_device, new SwapChainDescription1
        {
            Width = width,
            Height = height,
            Format = Format.B8G8R8A8_UNorm_SRgb,
            SampleDescription = new SampleDescription(1, 0),
            BufferUsage = Usage.RenderTargetOutput,
            BufferCount = 2,
            Scaling = Scaling.Stretch,
            SwapEffect = SwapEffect.FlipSequential,
            AlphaMode = AlphaMode.Premultiplied,
        }, null);
        this.As<ISwapChainPanelNative>().SetSwapChain(_swapChain.NativePointer);
    }

    void CreateTargets(uint width, uint height)
    {
        if (_device is null || _swapChain is null)
            return;

        _backBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0);
        _backView = _device.CreateRenderTargetView(_backBuffer);
        if (_samples > 1)
        {
            _msaa = _device.CreateTexture2DMultisample(
                Format.B8G8R8A8_UNorm_SRgb, width, height, _samples, 1, BindFlags.RenderTarget);
            _msaaView = _device.CreateRenderTargetView(_msaa);
        }

        _depth = _device.CreateTexture2DMultisample(
            Format.D32_Float, width, height, _samples, 1, BindFlags.DepthStencil);
        _depthView = _device.CreateDepthStencilView(_depth);
    }

    void Draw(bool advance)
    {
        if (_host is null || _context is null || _swapChain is null || _backView is null || _depthView is null)
            return;

        if (advance && _host.Pose.AutoRotateEnabled)
            AdvanceSpin();

        var draw = _host.Pose.Capture(_width / (float)Math.Max(_height, 1), openGlClip: false);
        var target = _msaaView ?? _backView;
        _context.OMSetRenderTargets(target, _depthView);
        _context.RSSetViewports([new Viewport(0, 0, _width, _height)]);
        var clear = draw.Clear;
        _context.ClearRenderTargetView(target, new Color4(clear.X, clear.Y, clear.Z, clear.W));
        _context.ClearDepthStencilView(_depthView, DepthStencilClearFlags.Depth, 1f, 0);
        if (!draw.Mesh.IsEmpty && EnsurePipeline())
            DrawMesh(draw);
        if (_msaa is not null && _backBuffer is not null)
            _context.ResolveSubresource(_backBuffer, 0, _msaa, 0, Format.B8G8R8A8_UNorm_SRgb);
        _swapChain.Present(0);
    }

    void AdvanceSpin()
    {
        var now = Stopwatch.GetTimestamp();
        if (_lastTicks != 0 && _host is not null)
        {
            var seconds = (now - _lastTicks) / (float)Stopwatch.Frequency;
            _host.Pose.Advance(Math.Min(seconds, 0.1f));
        }

        _lastTicks = now;
    }

    void DrawMesh(ObjectDraw draw)
    {
        if (_context is null || _layout is null || _vertexShader is null || _pixelShader is null
            || _raster is null || _rasterNone is null || _depthState is null || _constants is null || _sampler is null)
            return;

        EnsureBuffers(draw);
        if (_vertices is null || _indices is null)
            return;

        _context.IASetInputLayout(_layout);
        _context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        _context.IASetVertexBuffers(0, [_vertices], [(uint)Mesh.VertexStride], [0u]);
        _context.IASetIndexBuffer(_indices, Format.R32_UInt, 0);
        _context.VSSetShader(_vertexShader);
        _context.PSSetShader(_pixelShader);
        _context.VSSetConstantBuffers(0, [_constants]);
        _context.PSSetConstantBuffers(0, [_constants]);
        _context.OMSetDepthStencilState(_depthState);
        _context.PSSetSamplers(0, [_sampler]);
        foreach (var group in draw.Mesh.Groups)
        {
            if (group.IndexCount <= 0)
                continue;

            var view = TextureFor(group.Texture);
            if (view is null)
                continue;

            _context.RSSetState(group.DoubleSided ? _rasterNone : _raster);
            _context.PSSetShaderResources(0, [view]);
            _context.UpdateSubresource(ShadeUniforms.Pack(draw, group), _constants, 0);
            _context.DrawIndexed((uint)group.IndexCount, (uint)group.IndexOffset, 0);
        }
    }

    ID3D11ShaderResourceView? TextureFor(int index)
    {
        if ((uint)index < (uint)_colorViews.Length && _colorViews[index] is not null)
            return _colorViews[index];

        return _whiteView;
    }

    bool EnsurePipeline()
    {
        if (_pipelineReady)
            return _layout is not null;
        _pipelineReady = true;
        if (_device is null)
            return false;

        try
        {
            var vertexCode = Compiler.Compile(Shader, "object_vertex", "object", "vs_5_0");
            var pixelCode = Compiler.Compile(Shader, "object_fragment", "object", "ps_5_0");
            _vertexShader = _device.CreateVertexShader(vertexCode.Span);
            _pixelShader = _device.CreatePixelShader(pixelCode.Span);
            _layout = _device.CreateInputLayout(
            [
                new InputElementDescription("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                new InputElementDescription("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
                new InputElementDescription("TEXCOORD", 0, Format.R32G32_Float, 24, 0),
            ], vertexCode.Span);
            _raster = RasterState(CullMode.Back);
            _rasterNone = RasterState(CullMode.None);
            _depthState = _device.CreateDepthStencilState(
                new DepthStencilDescription(true, DepthWriteMask.All, ComparisonFunction.LessEqual));
            _constants = _device.CreateBuffer(176, BindFlags.ConstantBuffer, ResourceUsage.Default, CpuAccessFlags.None);
            _sampler = _device.CreateSamplerState(new SamplerDescription(
                Filter.MinMagLinearMipPoint,
                TextureAddressMode.Wrap,
                0f,
                1,
                ComparisonFunction.Never,
                0f,
                0f));
        }
        catch (Exception)
        {
            _vertexShader?.Dispose();
            _pixelShader?.Dispose();
            _layout?.Dispose();
            _raster?.Dispose();
            _rasterNone?.Dispose();
            _depthState?.Dispose();
            _constants?.Dispose();
            _sampler?.Dispose();
            _vertexShader = null;
            _pixelShader = null;
            _layout = null;
            _raster = null;
            _rasterNone = null;
            _depthState = null;
            _constants = null;
            _sampler = null;
            return false;
        }

        return _layout is not null && _constants is not null;
    }

    ID3D11RasterizerState RasterState(CullMode cull)
        => _device!.CreateRasterizerState(new RasterizerDescription(cull, FillMode.Solid)
        {
            FrontCounterClockwise = true,
            MultisampleEnable = true,
        });

    void EnsureBuffers(ObjectDraw draw)
    {
        if (_device is null || _generation == draw.Generation)
            return;

        _vertices?.Dispose();
        _indices?.Dispose();
        ReleaseColorTextures();
        _vertices = _device.CreateBuffer(draw.Mesh.Interleaved, BindFlags.VertexBuffer);
        _indices = _device.CreateBuffer(draw.Mesh.Indices, BindFlags.IndexBuffer);
        if (_whiteView is null)
            (_whiteTexture, _whiteView) = Upload(WhiteRgba, 1, 1);
        _colorTextures = new ID3D11Texture2D?[draw.Mesh.Images.Length];
        _colorViews = new ID3D11ShaderResourceView?[draw.Mesh.Images.Length];
        for (var i = 0; i < draw.Mesh.Images.Length; i++)
        {
            var image = draw.Mesh.Images[i];
            (_colorTextures[i], _colorViews[i]) = Upload(image.Rgba, image.Width, image.Height);
        }

        _generation = draw.Generation;
    }

    (ID3D11Texture2D? Texture, ID3D11ShaderResourceView? View) Upload(byte[] rgba, int width, int height)
    {
        if (_device is null || width <= 0 || height <= 0 || rgba.Length < width * height * 4)
            return (null, null);

        ID3D11Texture2D? texture = null;
        try
        {
            texture = _device.CreateTexture2D(
                rgba,
                Format.R8G8B8A8_UNorm_SRgb,
                (uint)width,
                (uint)height);
            var view = _device.CreateShaderResourceView(texture, null);
            return (texture, view);
        }
        catch (Exception)
        {
            texture?.Dispose();
            return (null, null);
        }
    }

    void ReleaseColorTextures()
    {
        foreach (var view in _colorViews)
            view?.Dispose();
        foreach (var texture in _colorTextures)
            texture?.Dispose();
        _colorViews = [];
        _colorTextures = [];
    }

    static uint Pixels(double dips, double scale)
    {
        if (double.IsNaN(dips) || dips <= 0 || scale <= 0)
            return 0;

        return (uint)Math.Max(1, Math.Round(dips * scale));
    }

    void ReleaseTargets()
    {
        _backView?.Dispose();
        _msaaView?.Dispose();
        _depthView?.Dispose();
        _backBuffer?.Dispose();
        _msaa?.Dispose();
        _depth?.Dispose();
        _backView = null;
        _msaaView = null;
        _depthView = null;
        _backBuffer = null;
        _msaa = null;
        _depth = null;
    }

    void ReleaseDevice()
    {
        ReleaseTargets();
        _vertices?.Dispose();
        _indices?.Dispose();
        _constants?.Dispose();
        _sampler?.Dispose();
        ReleaseColorTextures();
        _whiteView?.Dispose();
        _whiteTexture?.Dispose();
        _layout?.Dispose();
        _raster?.Dispose();
        _rasterNone?.Dispose();
        _depthState?.Dispose();
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _swapChain?.Dispose();
        _context?.Dispose();
        _device?.Dispose();
        _vertices = null;
        _indices = null;
        _constants = null;
        _sampler = null;
        _whiteView = null;
        _whiteTexture = null;
        _layout = null;
        _raster = null;
        _rasterNone = null;
        _depthState = null;
        _vertexShader = null;
        _pixelShader = null;
        _swapChain = null;
        _context = null;
        _device = null;
        _width = 0;
        _height = 0;
        _generation = 0;
        _pipelineReady = false;
    }
}
