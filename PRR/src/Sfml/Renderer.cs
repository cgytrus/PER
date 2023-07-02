using System;
using System.Collections.Generic;
using System.IO;

using PER.Abstractions.Rendering;
using PER.Util;

using SFML.Graphics;
using SFML.System;
using SFML.Window;

using Color = PER.Util.Color;
using Shader = SFML.Graphics.Shader;

namespace PRR.Sfml;

public class Renderer : BasicRenderer, IDisposable {
    public override bool open => window?.IsOpen ?? false;
    public override bool focused => window?.HasFocus() ?? false;
    public override event EventHandler? focusChanged;
    public override event EventHandler? closed;

    public override Color background {
        get => base.background;
        set {
            base.background = value;
            _background = SfmlConverters.ToSfmlColor(value);
        }
    }

    public Text? text { get; private set; }
    public RenderWindow? window { get; private set; }

    private readonly Dictionary<IPipelineEffect, CachedPipelineEffect> _cachedPipelineEffects = new();

    private bool _swapTextures;

    private RenderTexture? currentRenderTexture => _swapTextures ? _additionalRenderTexture : _mainRenderTexture;
    private RenderTexture? otherRenderTexture => _swapTextures ? _mainRenderTexture : _additionalRenderTexture;
    private Sprite? currentSprite => _swapTextures ? _additionalSprite : _mainSprite;
    private Sprite? otherSprite => _swapTextures ? _mainSprite : _additionalSprite;

    private SFML.Graphics.Color _background = SFML.Graphics.Color.Black;

    private RenderTexture? _mainRenderTexture;
    private RenderTexture? _additionalRenderTexture;
    private Sprite? _mainSprite;
    private Sprite? _additionalSprite;

    private Vector2f _textPosition;

    public override void Update(TimeSpan time) => window?.DispatchEvents();

    public override void Close() => window?.Close();

    public override void Finish() {
        if(window?.IsOpen ?? false)
            window.Close();
        Dispose();
        text = null;
        window = null;
        _mainRenderTexture = null;
        _additionalRenderTexture = null;
        _mainSprite = null;
        _additionalSprite = null;
    }

    public override bool Reset(RendererSettings settings) {
        if(base.Reset(settings))
            return true;
        // rebuild global effects cache on soft reload
        _cachedPipelineEffects.Clear();
        foreach(IPipelineEffect effect in pipelineEffects) {
            CachedPipelineEffect cachedPipelineEffect = new() { effect = effect };
            _cachedPipelineEffects.Add(effect, cachedPipelineEffect);
        }
        return false;
    }

    protected override void CreateWindow() {
        UpdateFont();

        VideoMode videoMode = fullscreen ? VideoMode.FullscreenModes[0] :
            new VideoMode((uint)(width * font?.size.x ?? 0), (uint)(height * font?.size.y ?? 0));

        window = new RenderWindow(videoMode, title, fullscreen ? Styles.Fullscreen : Styles.Close);
        window.SetView(new View(new Vector2f(videoMode.Width / 2f, videoMode.Height / 2f),
            new Vector2f(videoMode.Width, videoMode.Height)));

        UpdateIcon();

        window.LostFocus += (_, _) => focusChanged?.Invoke(this, EventArgs.Empty);
        window.GainedFocus += (_, _) => focusChanged?.Invoke(this, EventArgs.Empty);
        window.Closed += (_, _) => closed?.Invoke(this, EventArgs.Empty);

        _mainRenderTexture = new RenderTexture(videoMode.Width, videoMode.Height);
        _additionalRenderTexture = new RenderTexture(videoMode.Width, videoMode.Height);
        _mainSprite = new Sprite(_mainRenderTexture.Texture);
        _additionalSprite = new Sprite(_additionalRenderTexture.Texture);

        _textPosition = new Vector2f((videoMode.Width - text?.imageWidth ?? 0) / 2f,
            (videoMode.Height - text?.imageHeight ?? 0) / 2f);

        UpdateVerticalSync();
    }

    protected override void UpdateVerticalSync() {
        if(window is null)
            return;
        window.SetFramerateLimit(0);
        window.SetVerticalSyncEnabled(verticalSync);
    }

    protected override void UpdateTitle() => window?.SetTitle(title);

    protected override void UpdateIcon() {
        if(window is null)
            return;
        if(!File.Exists(icon)) {
            window.SetIcon(0, 0, Array.Empty<byte>());
            return;
        }
        SFML.Graphics.Image iconImage = new(icon);
        window.SetIcon(iconImage.Size.X, iconImage.Size.Y, iconImage.Pixels);
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
        CachedPipelineEffect cachedPipelineEffect = new() { effect = pipelineEffect };
        _cachedPipelineEffects.Add(pipelineEffect, cachedPipelineEffect);
    }

    public override void Draw() {
        if(window is null) {
            base.Draw();
            return;
        }

        UpdateEffects();

        text?.RebuildQuads(_textPosition);

        window.Clear(_background);
        _mainRenderTexture?.Clear(_background);
        _additionalRenderTexture?.Clear(_background);
        _mainRenderTexture?.Display();
        _additionalRenderTexture?.Display();

        RunPipelines();

        window.Display();

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
        if(window is null || currentRenderTexture is null) return;

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
        }
    }

    public void Dispose() {
        _additionalSprite?.Dispose();
        _mainSprite?.Dispose();
        _additionalRenderTexture?.Dispose();
        _mainRenderTexture?.Dispose();
        window?.Dispose();
        text?.Dispose();
        GC.SuppressFinalize(this);
    }
}
