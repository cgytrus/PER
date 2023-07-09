using System;
using System.IO;
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

    private RenderCharacter[,] display { get; set; } = new RenderCharacter[0, 0];
    private bool[,] displayUsed { get; set; } = new bool[0, 0];

    public Text? text { get; private set; }
    public NativeWindow? window { get; private set; }

    private readonly BlendMode _blend = Converters.ToPrrBlendMode(PER.Abstractions.Rendering.BlendMode.alpha);

    private Shader? _shader;
    private const string VertexSource = """
#version 330 core

uniform ivec2 viewSize;
uniform ivec2 imageSize;

layout(location = 0) in vec2 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec4 aBackgroundColor;
layout(location = 3) in vec4 aForegroundColor;

out vec2 texCoord;
out vec4 backgroundColor;
out vec4 foregroundColor;

void main() {
    gl_Position = vec4((aPosition * vec2(2.0, -2.0) - vec2(imageSize.x, -imageSize.y)) / viewSize, 0.0, 1.0);
    texCoord = aTexCoord;
    backgroundColor = aBackgroundColor;
    foregroundColor = aForegroundColor;
}
""";
    private const string FragmentSource = """
#version 330 core

uniform sampler2D font;

in vec2 texCoord;
in vec4 backgroundColor;
in vec4 foregroundColor;

out vec4 fragColor;

void main() {
    vec4 top = foregroundColor * texture(font, texCoord.st);
    float t = (1.0 - top.a) * backgroundColor.a;
    float a = t + top.a;
    vec3 final = (t * backgroundColor.rgb + top.a * top.rgb) / a;
    fragColor = vec4(final, a);
}
""";

    private bool _shouldClose;

    private Color4 _background = Color4.Black;

    public override void Update(TimeSpan time) => NativeWindow.ProcessWindowEvents(false);

    public override void Close() {
        window?.Close();
        _shouldClose = true;
    }

    public override void Finish() {
        Dispose();
        text = null;
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
            int font = _shader.GetUniformLocation("font");
            if(font != -1)
                GL.Uniform1(font, 0);
        }
        _shader.Use();
        int viewSize = _shader.GetUniformLocation("viewSize");
        if(viewSize != -1)
            GL.Uniform2(viewSize, window.Size);

        GL.Enable(EnableCap.Blend);

#if DEBUG
        GL.DebugMessageCallback(debugMessageDelegate, IntPtr.Zero);
        GL.Enable(EnableCap.DebugOutput);
        GL.Enable(EnableCap.DebugOutputSynchronous);
#endif

        UpdateVerticalSync();

        window.FocusedChanged += _ => focusChanged?.Invoke(this, EventArgs.Empty);
        window.Closing += _ => closed?.Invoke(this, EventArgs.Empty);

        UpdateFont();
    }

    protected override void UpdateFont() {
        base.UpdateFont();
        display = new RenderCharacter[height, width];
        displayUsed = new bool[height, width];
        text = new Text(font, new Vector2Int(width, height), display, displayUsed, displayEffects,
            globalDrawableEffects, globalModEffects);
        if(_shader is null)
            return;
        _shader.Use();
        int imageSize = _shader.GetUniformLocation("imageSize");
        if(imageSize != -1)
            GL.Uniform2(imageSize, new Vector2i(text.imageWidth, text.imageHeight));
    }

    protected override void UpdateVerticalSync() {
        if(window is null)
            return;
        window.VSync = verticalSync ? VSyncMode.On : VSyncMode.Off;
    }

    public override void Draw() {
        if(window is null || text is null || _shader is null) {
            updatableEffects.Clear();
            globalDrawableEffects.Clear();
            globalModEffects.Clear();
            return;
        }

        foreach(IUpdatableEffect effect in updatableEffects)
            effect.Update();

        text.RebuildQuads();

        updatableEffects.Clear();
        globalDrawableEffects.Clear();
        globalModEffects.Clear();

        GL.ClearColor(_background);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        _shader.Use();
        text.DrawQuads(_blend);

        window.Context.SwapBuffers();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public override void DrawCharacter(Vector2Int position, RenderCharacter character, IDisplayEffect? effect = null) {
        if(position.x < 0 || position.y < 0 || position.x >= width || position.y >= height)
            return;
        AddEffect(position, effect);
        if(character.background.a == 0f &&
            (!IsCharacterDrawable(character.character, character.style) || character.foreground.a == 0f))
            return;
        if(displayUsed[position.y, position.x])
            character = character with { background = GetCharacter(position).background.Blend(character.background) };
        display[position.y, position.x] = character;
        displayUsed[position.y, position.x] = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public override RenderCharacter GetCharacter(Vector2Int position) =>
        position.x < 0 || position.y < 0 || position.x >= width || position.y >= height ||
        !displayUsed[position.y, position.x] ?
            new RenderCharacter('\0', Color.transparent, Color.transparent) : display[position.y, position.x];

    public void Dispose() {
        window?.Dispose();
        text?.Dispose();
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
