using System;
using System.Collections.Generic;
using System.IO;
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

    public Text? text { get; private set; }
    public NativeWindow? window { get; private set; }

    private bool _shouldClose;

    private readonly Dictionary<IPipelineEffect, CachedPipelineEffect> _cachedPipelineEffects = new();

    private bool _swapTextures;

    // TODO
    //private RenderTexture? currentRenderTexture => _swapTextures ? _additionalRenderTexture : _mainRenderTexture;
    //private RenderTexture? otherRenderTexture => _swapTextures ? _mainRenderTexture : _additionalRenderTexture;
    //private Sprite? currentSprite => _swapTextures ? _additionalSprite : _mainSprite;
    //private Sprite? otherSprite => _swapTextures ? _mainSprite : _additionalSprite;

    private Color4 _background = Color4.Black;

    // TODO
    //private RenderTexture? _mainRenderTexture;
    //private RenderTexture? _additionalRenderTexture;
    //private Sprite? _mainSprite;
    //private Sprite? _additionalSprite;

    public override void Update(TimeSpan time) => NativeWindow.ProcessWindowEvents(false);

    public override void Close() {
        window?.Close();
        _shouldClose = true;
    }

    public override void Finish() {
        Dispose();
        text = null;
        window = null;
        // TODO
        //_mainRenderTexture = null;
        //_additionalRenderTexture = null;
        //_mainSprite = null;
        //_additionalSprite = null;
    }

    public override bool Reset(RendererSettings settings) {
        if(base.Reset(settings))
            return true;
        // rebuild global effects cache on soft reload
        _cachedPipelineEffects.Clear();
        foreach(IPipelineEffect effect in pipelineEffects) {
            CachedPipelineEffect cachedPipelineEffect = new(window?.Size ?? Vector2i.Zero,
                new Vector2i(text?.imageWidth ?? 0, text?.imageHeight ?? 0), effect);
            _cachedPipelineEffects.Add(effect, cachedPipelineEffect);
        }
        return false;
    }

    protected override void CreateWindow() {
        // TODO: get monitor from current cursor pos
        MonitorInfo monitor = Monitors.GetPrimaryMonitor();

        Vector2Int windowSize = fullscreen ?
            new Vector2Int(monitor.CurrentVideoMode.Width, monitor.CurrentVideoMode.Height) :
            new Vector2Int(width * font?.size.x ?? 0, height * font?.size.y ?? 0);

        NativeWindowSettings settings = new() {
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
            WindowState = fullscreen ? WindowState.Fullscreen : WindowState.Normal,
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

        window = new NativeWindow(settings);

        GL.Enable(EnableCap.Blend);

#if DEBUG
        GL.DebugMessageCallback(debugMessageDelegate, IntPtr.Zero);
        GL.Enable(EnableCap.DebugOutput);
        GL.Enable(EnableCap.DebugOutputSynchronous);
#endif

        UpdateFont();
        UpdateIcon();
        UpdateVerticalSync();

        window.FocusedChanged += _ => focusChanged?.Invoke(this, EventArgs.Empty);
        window.Closing += _ => closed?.Invoke(this, EventArgs.Empty);

        // TODO
        //_mainRenderTexture = new RenderTexture(videoMode.Width, videoMode.Height);
        //_additionalRenderTexture = new RenderTexture(videoMode.Width, videoMode.Height);
        //_mainSprite = new Sprite(_mainRenderTexture.Texture);
        //_additionalSprite = new Sprite(_additionalRenderTexture.Texture);
    }

    protected override void UpdateVerticalSync() {
        if(window is null)
            return;
        window.VSync = verticalSync ? VSyncMode.On : VSyncMode.Off;
    }

    protected override void UpdateTitle() {
        if(window is null)
            return;
        window.Title = title;
    }

    protected override void UpdateIcon() {
        if(window is null)
            return;
        if(!File.Exists(icon)) {
            window.Icon = new WindowIcon(Array.Empty<Image>());
            return;
        }
        QoiImage image = QoiDecoder.Decode(File.ReadAllBytes(icon));
        window.Icon = new WindowIcon(new Image(image.Width, image.Height, image.Data));
    }

    protected override void UpdateFont() {
        _cachedPipelineEffects.Clear();
        base.UpdateFont();
    }

    protected override void CreateText() =>
        text = new Text(font, new Vector2Int(width, height), display, displayUsed, displayEffects,
            globalDrawableEffects, globalModEffects);

    public override void AddEffect(IEffect effect) {
        base.AddEffect(effect);
        if(effect is not IPipelineEffect pipelineEffect || _cachedPipelineEffects.ContainsKey(pipelineEffect))
            return;
        CachedPipelineEffect cachedPipelineEffect = new(window?.Size ?? Vector2i.Zero,
            new Vector2i(text?.imageWidth ?? 0, text?.imageHeight ?? 0), pipelineEffect);
        _cachedPipelineEffects.Add(pipelineEffect, cachedPipelineEffect);
    }

    public override void Draw() {
        if(window is null) {
            base.Draw();
            return;
        }

        UpdateEffects();

        text?.RebuildQuads();

        GL.ClearColor(_background);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        // TODO
        //_mainRenderTexture?.Clear(_background);
        //_additionalRenderTexture?.Clear(_background);
        //_mainRenderTexture?.Display();
        //_additionalRenderTexture?.Display();

        RunPipelines();

        window.Context.SwapBuffers();

        base.Draw();
    }

    private void RunPipelines() {
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach(IPipelineEffect effect in pipelineEffects) {
            if(effect.pipeline is null)
                continue;

            CachedPipelineEffect cachedPipelineEffect = _cachedPipelineEffects[effect];
            // ignore because can't be null when effect.pipeline is not null
            for(int i = 0; i < cachedPipelineEffect.pipeline!.Length; i++) {
                CachedPipelineStep step = cachedPipelineEffect.pipeline[i];
                RunPipelineStep(step, i);
            }
        }
    }

    private void RunPipelineStep(CachedPipelineStep step, int index) {
        if(window is null)
            return;

        if(step.shader is not null) {
            int stepLocation = step.shader.GetUniformLocation("step");
            if(stepLocation != -1)
                GL.Uniform1(stepLocation, index);
            step.shader.Use();
        }

        switch(step.type) {
            case PipelineStep.Type.Text:
                text?.DrawQuads(step.blendMode);
                break;
            case PipelineStep.Type.Screen:
                break;
            case PipelineStep.Type.TemporaryText:
                break;
            case PipelineStep.Type.TemporaryScreen:
                break;
            case PipelineStep.Type.SwapBuffer:
                _swapTextures = !_swapTextures;
                break;
            case PipelineStep.Type.ClearBuffer:
                break;
        }

        // TODO
        /*
        if(window is null || currentRenderTexture is null)
            return;

        step.shader?.SetUniform("step", index);
        switch(step.type) {
            case PipelineStep.Type.Text:
                step.shader?.SetUniform("current", currentRenderTexture?.Texture);
                step.shader?.SetUniform("target", otherRenderTexture?.Texture);
                text?.DrawQuads(window, step.blendMode, step.shader);
                break;
            case PipelineStep.Type.Screen:
                step.shader?.SetUniform("current", currentRenderTexture?.Texture);
                step.shader?.SetUniform("target", otherRenderTexture?.Texture);
                currentSprite?.Draw(window, step.renderState);
                break;
            case PipelineStep.Type.TemporaryText:
                step.shader?.SetUniform("current", Shader.CurrentTexture);
                text?.DrawQuads(currentRenderTexture, step.blendMode, step.shader);
                break;
            case PipelineStep.Type.TemporaryScreen:
                step.shader?.SetUniform("current", Shader.CurrentTexture);
                step.shader?.SetUniform("target", currentRenderTexture?.Texture);
                otherSprite?.Draw(currentRenderTexture, step.renderState);
                break;
            case PipelineStep.Type.SwapBuffer:
                _swapTextures = !_swapTextures;
                break;
            case PipelineStep.Type.ClearBuffer:
                currentRenderTexture?.Clear(_background);
                break;
        }*/
    }

    public void Dispose() {
        // TODO
        //_additionalSprite?.Dispose();
        //_mainSprite?.Dispose();
        //_additionalRenderTexture?.Dispose();
        //_mainRenderTexture?.Dispose();
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
