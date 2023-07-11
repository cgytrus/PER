using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;

using PER.Abstractions.Rendering;
using PER.Util;

using QoiSharp;

using Color = PER.Util.Color;
using Image = OpenTK.Windowing.Common.Input.Image;

namespace PRR.OpenGL;

public class Renderer : BasicRenderer, IDisposable {
    public override bool open => window is not null && !_shouldClose;
    public override bool focused => window?.IsFocused ?? false;
    public override event EventHandler? focusChanged;
    public override event EventHandler? closed;

    public override Color background {
        get => base.background;
        set {
            base.background = value;
            _background = Converters.ToOtkColor(value);
        }
    }

    public NativeWindow? window { get; private set; }

    private readonly BlendMode _blend = Converters.ToPrrBlendMode(PER.Abstractions.Rendering.BlendMode.alpha);

    private Shader? _shader;
    private const string VertexSource = """
#version 330 core

uniform ivec2 charSize; // font size
uniform vec2 normCharSize; // normalized to viewport

uniform isampler2D styleTex;
uniform isampler2D offsetTex;

layout(location = 0) in vec2 aPosition;

flat out ivec2 position;
out vec2 texCoord;

void main() {
    ivec2 size = textureSize(styleTex, 0);
    position = ivec2(gl_InstanceID % size.x, gl_InstanceID / size.x);

    int italic = texelFetch(styleTex, position, 0).z;
    ivec2 offset = texelFetch(offsetTex, position, 0).xy;

    vec2 processedPos = aPosition + position + (vec2(italic * (1.0 - aPosition.y), 0.0) + offset) / charSize;

    gl_Position = vec4(processedPos * normCharSize * vec2(2.0, -2.0) - vec2(1.0, -1.0), 0.0, 1.0);
    texCoord = aPosition * charSize;
}
""";
    private const string FragmentSource = """
#version 330 core

uniform sampler2D backgroundTex;
uniform sampler2D foregroundTex;
uniform sampler2D characterTex;
uniform isampler2D styleTex;

uniform sampler2D font;
uniform sampler2D formatting;

flat in ivec2 position;
in vec2 texCoord;

out vec4 color;

vec4 blend(vec4 bottom, vec4 top) {
    float t = (1.0 - top.a) * bottom.a;
    float a = t + top.a;
    return vec4((t * bottom.rgb + top.a * top.rgb) / a, a);
}

void main() {
    vec4 backgroundColor = texelFetch(backgroundTex, position, 0);
    vec4 foregroundColor = texelFetch(foregroundTex, position, 0);
    ivec2 character = ivec2(texelFetch(characterTex, position, 0).xy + texCoord);
    ivec3 style = texelFetch(styleTex, position, 0).xyz;
    int bold = style.x;
    int underlineStrikethrough = style.y;

    vec4 foreground = texelFetch(font, character, 0);
    foreground = max(foreground, bold * texelFetch(font, character - ivec2(1, 0), 0));
    foreground = max(foreground, texelFetch(formatting, ivec2(underlineStrikethrough, int(texCoord.y)), 0));

    color = blend(backgroundColor, foregroundColor * foreground);
}
""";

    private Shader? _pixelShader;
    private const string PixelVertexSource = """
#version 330 core

uniform isampler2D offsetTex;

layout(location = 0) in ivec2 aPosition;
layout(location = 1) in vec4 aBackground;
layout(location = 2) in vec4 aForeground;
layout(location = 3) in ivec2 aCharacter;
layout(location = 4) in ivec3 aStyle;
layout(location = 5) in ivec2 aOffset;

out vec4 background;
out vec4 foreground;
flat out ivec2 character;
flat out ivec3 style;
flat out ivec2 offset;

void main() {
    gl_Position = vec4(vec2(aPosition.x + 0.5, aPosition.y + 0.5) / textureSize(offsetTex, 0) * 2.0 - vec2(1.0), 0.0, 1.0);
    background = aBackground;
    foreground = aForeground;
    character = aCharacter;
    style = aStyle;
    offset = aOffset;
}
""";
    private const string PixelFragmentSource = """
#version 330 core

uniform sampler2D backgroundTex;
uniform sampler2D foregroundTex;
uniform sampler2D characterTex;
uniform isampler2D styleTex;
uniform isampler2D offsetTex;

in vec4 background;
in vec4 foreground;
flat in ivec2 character;
flat in ivec3 style;
flat in ivec2 offset;

layout(location = 0) out vec4 bg;
layout(location = 1) out vec4 fg;
layout(location = 2) out vec4 ch;
layout(location = 3) out ivec3 st;
layout(location = 4) out ivec2 off;

void main() {
    bg = background;
    fg = foreground;
    ch = character.x < 0 ? vec4(0.0) : vec4(character, 0.0, 1.0);
    st = style;
    off = offset;
}
""";

    private int[]? _displayTex;
    private int _display = -1;

    private int _pixelVao;
    private int _pixelVbo;
    private readonly List<Pixel> _pixels = new();

    private int _font;
    private int _formatting;
    private int _vao;
    private int _vbo;
    private Vector2i?[] _characters = Array.Empty<Vector2i?>();

    private bool _shouldClose;

    private Color4 _background = Color4.Black;

    [StructLayout(LayoutKind.Sequential)]
    private struct Pixel {
        public Vector2i position;
        public Color4 background;
        public Color4 foreground;
        public Vector2i character;
        public Vector3i style;
        public Vector2i offset;

        public static void VertexAttrib() {
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribIPointer(0, 2, VertexAttribIntegerType.Int, 4 * 17, 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 4 * 17, 4 * 2);
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, 4 * 17, 4 * 6);
            GL.EnableVertexAttribArray(3);
            GL.VertexAttribIPointer(3, 2, VertexAttribIntegerType.Int, 4 * 17, 4 * 10);
            GL.EnableVertexAttribArray(4);
            GL.VertexAttribIPointer(4, 3, VertexAttribIntegerType.Int, 4 * 17, 4 * 12);
            GL.EnableVertexAttribArray(5);
            GL.VertexAttribIPointer(5, 2, VertexAttribIntegerType.Int, 4 * 17, 4 * 15);
        }
    }

    public override void Close() {
        window?.Close();
        _shouldClose = true;
    }

    public override void Finish() {
        Dispose();
        window = null;
    }

    public override void Setup(RendererSettings settings) {
        base.Setup(settings);

        // TODO: get monitor from current cursor pos
        MonitorInfo monitor = Monitors.GetPrimaryMonitor();

        Vector2Int windowSize = settings.fullscreen ?
            new Vector2Int(monitor.CurrentVideoMode.Width, monitor.CurrentVideoMode.Height) :
            new Vector2Int(width * font.size.x, height * font.size.y);

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
            Title = settings.title,
            StartFocused = true,
            StartVisible = true,
            WindowState = settings.fullscreen ? WindowState.Fullscreen : WindowState.Normal,
            WindowBorder = WindowBorder.Fixed,
            Location = new Vector2i(monitor.CurrentVideoMode.Width / 2 - windowSize.x / 2,
                monitor.CurrentVideoMode.Height / 2 - windowSize.y / 2),
            Size = Converters.ToOtkVector2Int(windowSize),
            NumberOfSamples = 0,
            // no stencil or depth needed
            StencilBits = 0,
            DepthBits = 0,
            SrgbCapable = false,
            TransparentFramebuffer = false
        };
        if(!string.IsNullOrWhiteSpace(settings.icon) && File.Exists(settings.icon)) {
            QoiImage image = QoiDecoder.Decode(File.ReadAllBytes(settings.icon));
            windowSettings.Icon = new WindowIcon(new Image(image.Width, image.Height, image.Data));
        }

        window = new NativeWindow(windowSettings);
        if(_shader is null) {
            _shader = new Shader(VertexSource, FragmentSource);
            _shader.Use();
            SetTextureUniforms(_shader);
        }

        if(_pixelShader is null) {
            _pixelShader = new Shader(PixelVertexSource, PixelFragmentSource);
            _pixelShader.Use();
            SetTextureUniforms(_pixelShader);
        }

        GL.Enable(EnableCap.Blend);
        _blend.Use();

#if DEBUG
        GL.DebugMessageCallback(debugMessageDelegate, IntPtr.Zero);
        GL.Enable(EnableCap.DebugOutput);
        GL.Enable(EnableCap.DebugOutputSynchronous);
#endif

        UpdateVerticalSync();

        window.FocusedChanged += _ => focusChanged?.Invoke(this, EventArgs.Empty);
        window.Closing += _ => closed?.Invoke(this, EventArgs.Empty);

        ResetGl();

        _displayTex = new int[] {
            CreateDisplayTexture(PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float),
            CreateDisplayTexture(PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float),
            CreateDisplayTexture(PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float),
            CreateDisplayTexture(PixelInternalFormat.Rgb32i, PixelFormat.RgbInteger, PixelType.Int),
            CreateDisplayTexture(PixelInternalFormat.Rg32i, PixelFormat.RgInteger, PixelType.Int)
        };

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
        GL.DrawBuffers(5, new DrawBuffersEnum[] {
            DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1,
            DrawBuffersEnum.ColorAttachment2, DrawBuffersEnum.ColorAttachment3,
            DrawBuffersEnum.ColorAttachment4
        });
        GL.Enable(IndexedEnableCap.Blend, 0);
        GL.Disable(IndexedEnableCap.Blend, 1);
        GL.Enable(IndexedEnableCap.Blend, 2);
        GL.Disable(IndexedEnableCap.Blend, 3);
        GL.Disable(IndexedEnableCap.Blend, 4);
        FramebufferErrorCode error = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        if(error != FramebufferErrorCode.FramebufferComplete)
            throw new InvalidOperationException(error.ToString());

        _characters = new Vector2i?[0xFFFF];
        foreach((char c, Vector2Int x) in font.characters)
            _characters[c] = Converters.ToOtkVector2Int(x);

        _font = CreateTexture(font.image);
        _formatting = CreateTexture(font.formattingImage);

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

        if(_shader is null || window is null)
            return;
        _shader.Use();
        int charSize = _shader.GetUniformLocation("charSize");
        if(charSize != -1)
            GL.Uniform2(charSize, new Vector2i(font.size.x, font.size.y));
        int normCharSize = _shader.GetUniformLocation("normCharSize");
        if(normCharSize != -1)
            GL.Uniform2(normCharSize,
                new Vector2(font.size.x / (float)window.Size.X, font.size.y / (float)window.Size.Y));
    }

    private int CreateDisplayTexture(PixelInternalFormat internalFormat, PixelFormat format, PixelType type) {
        int texture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, texture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, format, type, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.BindTexture(TextureTarget.Texture2D, 0);
        return texture;
    }
    private static int CreateTexture(PER.Abstractions.Rendering.Image image) {
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
    private static void SetTextureUniforms(Shader shader) {
        int backgroundTex = shader.GetUniformLocation("backgroundTex");
        int foregroundTex = shader.GetUniformLocation("foregroundTex");
        int characterTex = shader.GetUniformLocation("characterTex");
        int styleTex = shader.GetUniformLocation("styleTex");
        int offsetTex = shader.GetUniformLocation("offsetTex");
        int font = shader.GetUniformLocation("font");
        int formatting = shader.GetUniformLocation("formatting");
        if(backgroundTex != -1)
            GL.Uniform1(backgroundTex, 0);
        if(foregroundTex != -1)
            GL.Uniform1(foregroundTex, 1);
        if(characterTex != -1)
            GL.Uniform1(characterTex, 2);
        if(styleTex != -1)
            GL.Uniform1(styleTex, 3);
        if(offsetTex != -1)
            GL.Uniform1(offsetTex, 4);
        if(font != -1)
            GL.Uniform1(font, 5);
        if(formatting != -1)
            GL.Uniform1(formatting, 6);
    }
    private void BindAllTextures() {
        if(_displayTex is not null) {
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
        }
        GL.ActiveTexture(TextureUnit.Texture5);
        GL.BindTexture(TextureTarget.Texture2D, _font);
        GL.ActiveTexture(TextureUnit.Texture6);
        GL.BindTexture(TextureTarget.Texture2D, _formatting);
    }

    protected override void UpdateVerticalSync() {
        if(window is null)
            return;
        window.VSync = verticalSync ? VSyncMode.On : VSyncMode.Off;
    }

    public override void BeginDraw() {
        _pixels.Clear();
        updatableEffects.Clear();
        drawableEffects.Clear();
        modEffects.Clear();

        NativeWindow.ProcessWindowEvents(false);

        if(window is null || _shader is null || _displayTex is null)
            return;

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _display);
        GL.Viewport(0, 0, width, height);

        GL.ClearColor(Color4.Black with { A = 0f });
        GL.Clear(ClearBufferMask.ColorBufferBit);
    }

    public override void EndDraw() {
        if(window is null || _shader is null || _displayTex is null)
            return;

        foreach(IUpdatableEffect effect in updatableEffects)
            effect.Update();
        foreach(IDrawableEffect effect in drawableEffects)
            for(int y = 0; y < height; y++)
                for(int x = 0; x < width; x++)
                    effect.Draw(new Vector2Int(x, y));

        _blend.Use();
        _pixelShader?.Use();
        BindAllTextures();
        GL.BindVertexArray(_pixelVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _pixelVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, 4 * 17 * _pixels.Capacity, IntPtr.Zero, BufferUsageHint.StreamDraw);
        unsafe {
            Pixel* buf = (Pixel*)GL.MapBufferRange(BufferTarget.ArrayBuffer, IntPtr.Zero, 4 * 17 * _pixels.Count,
                BufferAccessMask.MapInvalidateRangeBit | BufferAccessMask.MapWriteBit);
            CollectionsMarshal.AsSpan(_pixels).CopyTo(new Span<Pixel>(buf, 4 * 17 * _pixels.Count));
        }
        GL.UnmapBuffer(BufferTarget.ArrayBuffer);
        GL.DrawArrays(PrimitiveType.Points, 0, _pixels.Count);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, window.Size.X, window.Size.Y);

        GL.ClearColor(_background);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        _blend.Use();
        _shader.Use();
        BindAllTextures();
        GL.BindVertexArray(_vao);
        GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, 6, width * height);

        window.Context.SwapBuffers();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public override void DrawCharacter(Vector2Int position, RenderCharacter character, IDisplayEffect? effect = null) {
        Vector2Int offset = new(0, 0);
        if(effect is IModifierEffect modifierEffect)
            modifierEffect.ApplyModifiers(position, ref offset, ref character);
        foreach(IModifierEffect modEffect in modEffects)
            modEffect.ApplyModifiers(position, ref offset, ref character);

        bool bold = (character.style & RenderStyle.Bold) != 0;
        bool underline = (character.style & RenderStyle.Underline) != 0;
        bool strikethrough = (character.style & RenderStyle.Strikethrough) != 0;
        bool italic = (character.style & RenderStyle.Italic) != 0;

        _pixels.Add(new Pixel {
            position = new Vector2i(position.x, position.y),
            background = Converters.ToOtkColor(character.background),
            foreground = Converters.ToOtkColor(character.foreground),
            character = _characters[character.character] ?? new Vector2i(-1, -1),
            style = new Vector3i(bold ? 1 : 0, (underline ? 1 : 0) + (strikethrough ? 2 : 0), italic ? 1 : 0),
            offset = Converters.ToOtkVector2Int(offset)
        });

        if(effect is IDrawableEffect drawableEffect)
            drawableEffect.Draw(position);
        if(effect is IUpdatableEffect updatableEffect && !updatableEffects.Contains(updatableEffect))
            updatableEffects.Add(updatableEffect);
    }

    private void ResetGl() {
        GL.BindTexture(TextureTarget.Texture2D, 0);
        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        if(_displayTex is not null)
            foreach(int tex in _displayTex)
                GL.DeleteTexture(tex);
        _displayTex = null;
        if(_display != -1)
            GL.DeleteFramebuffer(_display);
        _display = -1;
        GL.DeleteVertexArray(_vao);
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_pixelVao);
        GL.DeleteBuffer(_pixelVbo);
        GL.DeleteTexture(_font);
        GL.DeleteTexture(_formatting);
    }

    public void Dispose() {
        ResetGl();
        window?.Dispose();
        GC.SuppressFinalize(this);
    }

#if DEBUG
    private static readonly DebugProc debugMessageDelegate = OnDebugMessage;

    private static void OnDebugMessage(DebugSource source, DebugType type, int id, DebugSeverity severity, int length,
        IntPtr pMessage, IntPtr pUserParam) {
        string message = Marshal.PtrToStringAnsi(pMessage, length);
        Console.WriteLine("[{0} source={1} type={2} id={3}] {4}", severity, source, type, id, message);
        if(type == DebugType.DebugTypeError)
            throw new Exception(message);
    }
#endif
}
