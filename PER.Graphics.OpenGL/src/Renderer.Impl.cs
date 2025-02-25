using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using PER.Abstractions.Rendering;
using PER.Util;

namespace PER.Graphics.OpenGL;

public partial class Renderer : IRenderer.IImpl, IDisposable {
    private readonly IRenderer.IHandler _handler;

    private readonly List<IDrawableEffect> _drawableEffects = [];
    private readonly List<IModifierEffect> _modEffects = [];

    private readonly BlendMode _blend = Converters.ToPrrBlendMode(PER.Abstractions.Rendering.BlendMode.alpha);

    private readonly Shader _shader;
    private readonly Shader _pixelShader;

    private readonly int[] _displayTex;
    private readonly int _display;

    private readonly int _pixelVao;
    private readonly int _pixelVbo;
    private readonly List<Pixel> _pixels = [];
    private int _lastPixelsCapacity;

    private readonly int _font;
    private readonly int _formatting;
    private readonly int _charactersTex;
    private readonly int _charactersBuf;
    private readonly int _vao;
    private readonly int _vbo;

    [StructLayout(LayoutKind.Sequential)]
    private struct Pixel {
        public Vector2i position;
        public Color4 background;
        public Color4 foreground;
        public int character;
        public Vector3i style;
        public Vector2i offset;

        public static void VertexAttrib() {
            int stride = Marshal.SizeOf<Pixel>();
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribIPointer(0, 2, VertexAttribIntegerType.Int, stride,
                Marshal.OffsetOf<Pixel>(nameof(position)));
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, stride,
                Marshal.OffsetOf<Pixel>(nameof(background)));
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, stride,
                Marshal.OffsetOf<Pixel>(nameof(foreground)));
            GL.EnableVertexAttribArray(3);
            GL.VertexAttribIPointer(3, 1, VertexAttribIntegerType.Int, stride,
                Marshal.OffsetOf<Pixel>(nameof(character)));
            GL.EnableVertexAttribArray(4);
            GL.VertexAttribIPointer(4, 3, VertexAttribIntegerType.Int, stride, Marshal.OffsetOf<Pixel>(nameof(style)));
            GL.EnableVertexAttribArray(5);
            GL.VertexAttribIPointer(5, 2, VertexAttribIntegerType.Int, stride, Marshal.OffsetOf<Pixel>(nameof(offset)));
        }
    }

    public Renderer(IRenderer.IHandler handler, string title, Vector2Int size) {
        _handler = handler;
        this.size = size;

        _settings = _handler.SetupRenderer(this);

        // TODO: get monitor from current cursor pos
        MonitorInfo monitor = Monitors.GetPrimaryMonitor();

        Vector2Int windowSize = _settings.fullscreen ?
            new Vector2Int(monitor.CurrentVideoMode.Width, monitor.CurrentVideoMode.Height) :
            new Vector2Int(size.x * font.size.x, size.y * font.size.y);

        NativeWindowSettings windowSettings = new() {
            IsEventDriven = false,
            API = ContextAPI.OpenGL,
            Profile = ContextProfile.Core,
#if DEBUG
            Flags = ContextFlags.ForwardCompatible | ContextFlags.Debug,
#else
            Flags = ContextFlags.ForwardCompatible,
#endif
            AutoLoadBindings = true,
            APIVersion = new Version(3, 3),
            Title = title,
            StartFocused = true,
            StartVisible = true,
            WindowState = _settings.fullscreen ? WindowState.Fullscreen : WindowState.Normal,
            WindowBorder = WindowBorder.Fixed,
            Location = new Vector2i(monitor.CurrentVideoMode.Width / 2 - windowSize.x / 2,
                monitor.CurrentVideoMode.Height / 2 - windowSize.y / 2),
            ClientSize = Converters.ToOtkVector2Int(windowSize),
            NumberOfSamples = 0,
            // no stencil or depth needed
            StencilBits = 0,
            DepthBits = 0,
            SrgbCapable = false,
            TransparentFramebuffer = false
        };
        if (_settings.icon.HasValue)
            windowSettings.Icon = new WindowIcon(Converters.ToOtkImage(_settings.icon.Value));

        window = new NativeWindow(windowSettings);

        _shader = new Shader(ShaderSources.Final.Vertex, ShaderSources.Final.Fragment);
        _shader.Use();
        SetTextureUniforms(_shader);

        _pixelShader = new Shader(ShaderSources.Pixel.Vertex, ShaderSources.Pixel.Fragment);
        _pixelShader.Use();
        SetTextureUniforms(_pixelShader);

        GL.Enable(EnableCap.Blend);
        _blend.Use();

#if DEBUG
        GL.DebugMessageCallback(OnDebugMessage, IntPtr.Zero);
        GL.Enable(EnableCap.DebugOutput);
        GL.Enable(EnableCap.DebugOutputSynchronous);
#endif

        window.VSync = verticalSync ? VSyncMode.On : VSyncMode.Off;

        window.FocusedChanged += _ => focusChanged?.Invoke(this, EventArgs.Empty);
        window.Closing += _ => closed?.Invoke(this, EventArgs.Empty);

        _displayTex = [
            CreateDisplayTexture(PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float),
            CreateDisplayTexture(PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float),
            CreateDisplayTexture(PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float),
            CreateDisplayTexture(PixelInternalFormat.Rgb32i, PixelFormat.RgbInteger, PixelType.Int),
            CreateDisplayTexture(PixelInternalFormat.Rg32i, PixelFormat.RgInteger, PixelType.Int)
        ];

        _display = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _display);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, _displayTex[0], 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1,
            TextureTarget.Texture2D, _displayTex[1], 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment2,
            TextureTarget.Texture2D, _displayTex[2], 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment3,
            TextureTarget.Texture2D, _displayTex[3], 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment4,
            TextureTarget.Texture2D, _displayTex[4], 0);
        GL.DrawBuffers(5, [
            DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1,
            DrawBuffersEnum.ColorAttachment2, DrawBuffersEnum.ColorAttachment3,
            DrawBuffersEnum.ColorAttachment4
        ]);
        GL.Enable(IndexedEnableCap.Blend, 0);
        GL.Disable(IndexedEnableCap.Blend, 1);
        GL.Enable(IndexedEnableCap.Blend, 2);
        GL.Disable(IndexedEnableCap.Blend, 3);
        GL.Disable(IndexedEnableCap.Blend, 4);
        FramebufferErrorCode error = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        if (error != FramebufferErrorCode.FramebufferComplete)
            throw new InvalidOperationException(error.ToString());

        _font = CreateTexture(font.image);
        _formatting = CreateTexture(font.formattingImage);

        _characters = new bool[0xFFFF];
        Vector2i[] characters = new Vector2i[0xFFFF];
        for (int i = 0; i < 0xFFFF; i++) {
            bool has = font.characters.TryGetValue((char)i, out Vector2Int x);
            _characters[i] = has;
            characters[i] = has ? Converters.ToOtkVector2Int(x) : Vector2i.Zero;
        }
        (_charactersTex, _charactersBuf) = CreateTextureBuffer(SizedInternalFormat.Rg32i, characters);

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        GL.BindVertexArray(_vao);

        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * 12, new Vector2[] {
            new(0f, 0f), new(0f, 1f), new(1f, 0f),
            new(0f, 1f), new(1f, 0f), new(1f, 1f)
        }, BufferUsageHint.StaticDraw);

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, sizeof(float) * 2, 0);

        _pixelVao = GL.GenVertexArray();
        _pixelVbo = GL.GenBuffer();
        GL.BindVertexArray(_pixelVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _pixelVbo);
        Pixel.VertexAttrib();
        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

        _shader.Use();
        int charSize = _shader.GetUniformLocation("charSize");
        if (charSize != -1)
            GL.Uniform2(charSize, new Vector2i(font.size.x, font.size.y));
        int normCharSize = _shader.GetUniformLocation("normCharSize");
        if (normCharSize != -1)
            GL.Uniform2(normCharSize,
                new Vector2(font.size.x / (float)window.ClientSize.X, font.size.y / (float)window.ClientSize.Y));
    }

    private int CreateDisplayTexture(PixelInternalFormat internalFormat, PixelFormat format, PixelType type) {
        int texture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, texture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, size.x, size.y, 0, format, type, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.BindTexture(TextureTarget.Texture2D, 0);
        return texture;
    }

    private static int CreateTexture(Abstractions.Rendering.Image image) {
        int texture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, texture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.width, image.height, 0,
            PixelFormat.Rgba, PixelType.Float,
            image.pixels.Cast<Color>().Select(Converters.ToOtkColor).ToArray());
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.BindTexture(TextureTarget.Texture2D, 0);
        return texture;
    }

    private static (int texture, int buffer) CreateTextureBuffer<T>(SizedInternalFormat format, T[] data)
        where T : struct {
        int buffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.TextureBuffer, buffer);
        GL.BufferData(BufferTarget.TextureBuffer, Marshal.SizeOf<T>() * data.Length, data, BufferUsageHint.StaticRead);
        GL.BindBuffer(BufferTarget.TextureBuffer, 0);

        int texture = GL.GenTexture();
        GL.BindTexture(TextureTarget.TextureBuffer, texture);
        GL.TexBuffer(TextureBufferTarget.TextureBuffer, format, buffer);
        GL.BindTexture(TextureTarget.TextureBuffer, 0);

        return (texture, buffer);
    }

    private static void SetTextureUniforms(Shader shader) {
        int backgroundTex = shader.GetUniformLocation("backgroundTex");
        int foregroundTex = shader.GetUniformLocation("foregroundTex");
        int characterTex = shader.GetUniformLocation("characterTex");
        int styleTex = shader.GetUniformLocation("styleTex");
        int offsetTex = shader.GetUniformLocation("offsetTex");
        int font = shader.GetUniformLocation("font");
        int formatting = shader.GetUniformLocation("formatting");
        int charMap = shader.GetUniformLocation("charMap");
        if (backgroundTex != -1)
            GL.Uniform1(backgroundTex, 0);
        if (foregroundTex != -1)
            GL.Uniform1(foregroundTex, 1);
        if (characterTex != -1)
            GL.Uniform1(characterTex, 2);
        if (styleTex != -1)
            GL.Uniform1(styleTex, 3);
        if (offsetTex != -1)
            GL.Uniform1(offsetTex, 4);
        if (font != -1)
            GL.Uniform1(font, 5);
        if (formatting != -1)
            GL.Uniform1(formatting, 6);
        if (charMap != -1)
            GL.Uniform1(charMap, 7);
    }

    private void BindAllTextures() {
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _displayTex[0]);
        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2D, _displayTex[1]);
        GL.ActiveTexture(TextureUnit.Texture2);
        GL.BindTexture(TextureTarget.Texture2D, _displayTex[2]);
        GL.ActiveTexture(TextureUnit.Texture3);
        GL.BindTexture(TextureTarget.Texture2D, _displayTex[3]);
        GL.ActiveTexture(TextureUnit.Texture4);
        GL.BindTexture(TextureTarget.Texture2D, _displayTex[4]);
        GL.ActiveTexture(TextureUnit.Texture5);
        GL.BindTexture(TextureTarget.Texture2D, _font);
        GL.ActiveTexture(TextureUnit.Texture6);
        GL.BindTexture(TextureTarget.Texture2D, _formatting);
        GL.ActiveTexture(TextureUnit.Texture7);
        GL.BindTexture(TextureTarget.TextureBuffer, _charactersTex);
    }

    public void Update() {
        _pixels.Clear();
        _drawableEffects.Clear();
        _modEffects.Clear();

        NativeWindow.ProcessWindowEvents(false);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _display);
        GL.Viewport(0, 0, size.x, size.y);

        GL.ClearColor(Color4.Black with { A = 0f });
        GL.Clear(ClearBufferMask.ColorBufferBit);

        _handler.Render(this);
        RenderEffects();

        _blend.Use();
        _pixelShader.Use();
        BindAllTextures();
        GL.BindVertexArray(_pixelVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _pixelVbo);
        if (_lastPixelsCapacity != _pixels.Capacity) {
            GL.BufferData(BufferTarget.ArrayBuffer, Marshal.SizeOf<Pixel>() * _pixels.Capacity, IntPtr.Zero, BufferUsageHint.StreamDraw);
            _lastPixelsCapacity = _pixels.Capacity;
        }
        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, Marshal.SizeOf<Pixel>() * _pixels.Count,
            ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(_pixels)));
        GL.DrawArrays(PrimitiveType.Points, 0, _pixels.Count);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, window.ClientSize.X, window.ClientSize.Y);

        GL.ClearColor(_background);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        _blend.Use();
        _shader.Use();
        BindAllTextures();
        GL.BindVertexArray(_vao);
        GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, 6, size.x * size.y);

        window.Context.SwapBuffers();
    }

    private void RenderEffects() {
        foreach (IDrawableEffect effect in _drawableEffects) {
            for (int y = 0; y < size.y; y++) {
                for (int x = 0; x < size.x; x++)
                    effect.Draw(new Vector2Int(x, y));
            }
        }
    }

    public void Dispose() {
        GL.BindTexture(TextureTarget.Texture2D, 0);
        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        foreach (int tex in _displayTex)
            GL.DeleteTexture(tex);
        if (_display != -1)
            GL.DeleteFramebuffer(_display);
        GL.DeleteVertexArray(_vao);
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_pixelVao);
        GL.DeleteBuffer(_pixelVbo);
        _lastPixelsCapacity = 0;
        GL.DeleteTexture(_font);
        GL.DeleteTexture(_formatting);
        GL.DeleteTexture(_charactersTex);
        GL.DeleteBuffer(_charactersBuf);
        _shader.Dispose();
        _pixelShader.Dispose();
        window.Dispose();
        GC.SuppressFinalize(this);
    }

#if DEBUG
    private static void OnDebugMessage(DebugSource source, DebugType type, int id, DebugSeverity severity, int length,
        IntPtr pMessage, IntPtr pUserParam) {
        string message = Marshal.PtrToStringAnsi(pMessage, length);
        Console.WriteLine("[{0} source={1} type={2} id={3}] {4}", severity, source, type, id, message);
        if(type == DebugType.DebugTypeError)
            throw new Exception(message);
    }
#endif
}
