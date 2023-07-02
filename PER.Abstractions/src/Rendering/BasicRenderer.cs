using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using JetBrains.Annotations;

using PER.Util;

namespace PER.Abstractions.Rendering;

[PublicAPI]
public abstract class BasicRenderer : IRenderer {
    public virtual string title {
        get => _title;
        set {
            _title = value;
            UpdateTitle();
        }
    }

    public virtual int width { get; private set; }
    public virtual int height { get; private set; }

    public virtual bool verticalSync {
        get => _verticalSync;
        set {
            _verticalSync = value;
            UpdateVerticalSync();
        }
    }

    public virtual bool fullscreen {
        get => _fullscreen;
        set {
            _fullscreen = value;
            Reset();
        }
    }

    public virtual IFont? font {
        get => _font;
        set {
            _font = value;
            Reset();
        }
    }

    public virtual string? icon {
        get => _icon;
        set {
            _icon = value;
            UpdateIcon();
        }
    }

    public abstract bool open { get; }
    public abstract bool focused { get; }
    public abstract event EventHandler? focusChanged;
    public abstract event EventHandler? closed;

    public virtual Color background { get; set; } = Color.black;

    public Dictionary<string, IDisplayEffect?> formattingEffects { get; } = new();

    protected RenderCharacter[,] display { get; private set; } = new RenderCharacter[0, 0];
    protected bool[,] displayUsed { get; private set; } = new bool[0, 0];

    protected List<IUpdatableEffect> updatableEffects { get; private set; } = new();
    protected List<IPipelineEffect> pipelineEffects { get; private set; } = new();
    protected List<IDrawableEffect> globalDrawableEffects { get; private set; } = new();
    protected List<IModifierEffect> globalModEffects { get; private set; } = new();
    protected IDisplayEffect?[,] displayEffects { get; private set; } = new IDisplayEffect?[0, 0];

    private bool _verticalSync;
    private bool _fullscreen;
    private IFont? _font;
    private string _title = "";
    private string? _icon;

    public virtual void Setup(RendererSettings settings) {
        _title = settings.title;
        width = settings.width;
        height = settings.height;
        _verticalSync = settings.verticalSync;
        _fullscreen = settings.fullscreen;
        _font = settings.font;
        _icon = settings.icon;

        CreateWindow();
    }

    protected abstract void CreateWindow();
    protected abstract void UpdateVerticalSync();
    protected abstract void UpdateTitle();
    protected abstract void UpdateIcon();

    public abstract void Update(TimeSpan time);
    public abstract void Close();
    public abstract void Finish();

    public virtual bool Reset(RendererSettings settings) {
        if(settings.width != width || settings.height != height || settings.font != _font ||
            settings.fullscreen != _fullscreen) {
            Finish();
            Setup(settings);
            return true;
        }
        title = settings.title;
        verticalSync = settings.verticalSync;
        icon = settings.icon;
        return false;
    }

    protected bool Reset() => Reset(new RendererSettings(this));

    protected virtual void UpdateFont() {
        display = new RenderCharacter[height, width];
        displayUsed = new bool[height, width];
        displayEffects = new IDisplayEffect?[height, width];

        updatableEffects.Clear();
        pipelineEffects.Clear();
        globalDrawableEffects.Clear();
        globalModEffects.Clear();

        CreateText();
    }

    protected abstract void CreateText();

    public virtual void Draw() {
        updatableEffects.Clear();
        pipelineEffects.Clear();
        globalDrawableEffects.Clear();
        globalModEffects.Clear();
    }

    protected void UpdateEffects() {
        foreach(IUpdatableEffect effect in updatableEffects)
            effect.Update();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public virtual void DrawCharacter(Vector2Int position, RenderCharacter character, IDisplayEffect? effect = null) {
        if(position.x < 0 || position.y < 0 || position.x >= width || position.y >= height)
            return;
        AddEffect(position, effect);
        if(IsCharacterEmpty(character))
            return;
        if(!IsCharacterEmpty(position))
            character = character with { background = GetCharacter(position).background.Blend(character.background) };
        display[position.y, position.x] = character;
        displayUsed[position.y, position.x] = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public virtual void DrawText(Vector2Int position, ReadOnlySpan<char> text, Func<char, Formatting> formatter,
        HorizontalAlignment align = HorizontalAlignment.Left, int maxWidth = 0) {
        if(text.Length == 0) return;

        char formattingFlag = '\0';
        int startIndex = 0;
        int width = 0;
        int y = 0;
        for(int i = 0; i <= text.Length; i++) {
            char currentCharacter = i >= text.Length ? '\n' : text[i];

            if(maxWidth > 0 && width >= maxWidth) {
                DrawCurrent(text);
                startIndex = i;
            }

            switch(currentCharacter) {
                case '\n':
                    DrawCurrent(text);
                    startIndex = i + 1;
                    break;
                case '\f': i++; // skip 2 characters
                    break;
                case not '\r': width++;
                    break;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            void DrawCurrent(ReadOnlySpan<char> allText) {
                int x = GetAlignOffset(align, width);
                DrawTextCharacter(position, allText, startIndex, x, y, width, formatter, ref formattingFlag);

                width = 0;
                y++;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void DrawTextCharacter(Vector2Int position, ReadOnlySpan<char> text, int startIndex, int x, int y,
        int width, Func<char, Formatting> formatter, ref char formattingFlag) {
        for(int i = startIndex; i < startIndex + width; i++) {
            char toDraw = text[i];
            if(toDraw == '\f') {
                formattingFlag = text[++i];
                width += 2;
                continue;
            }

            Formatting formatting = formatter(formattingFlag);
            Vector2Int charPos = new(position.x + x, position.y + y);
            DrawCharacter(charPos,
                new RenderCharacter(toDraw, formatting.backgroundColor, formatting.foregroundColor, formatting.style),
                formatting.effect);
            x++;
        }
    }

    private static int GetAlignOffset(HorizontalAlignment align, int width) => align switch {
        HorizontalAlignment.Left => 0,
        HorizontalAlignment.Middle => -width + width / 2 + 1,
        HorizontalAlignment.Right => -width + 1,
        _ => 0
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public virtual RenderCharacter GetCharacter(Vector2Int position) => IsCharacterEmpty(position) ?
        new RenderCharacter('\0', Color.transparent, Color.transparent) : display[position.y, position.x];

    public virtual void AddEffect(IEffect effect) {
        // ReSharper disable once ConvertIfStatementToSwitchStatement
        if(effect is IUpdatableEffect updatable)
            updatableEffects.Add(updatable);
        if(effect is IPipelineEffect pipeline)
            pipelineEffects.Add(pipeline);
        if(effect is IDrawableEffect drawable)
            globalDrawableEffects.Add(drawable);
        if(effect is IModifierEffect mod)
            globalModEffects.Add(mod);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public virtual void AddEffect(Vector2Int position, IDisplayEffect? effect) {
        if(position.x < 0 || position.y < 0 || position.x >= width || position.y >= height)
            return;
        if(displayEffects[position.y, position.x] is IUpdatableEffect prevUpdatable)
            updatableEffects.Remove(prevUpdatable);
        if(effect is IUpdatableEffect updatable)
            updatableEffects.Add(updatable);
        displayEffects[position.y, position.x] = effect;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public virtual bool IsCharacterEmpty(Vector2Int position) => !displayUsed[position.y, position.x];

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public virtual bool IsCharacterEmpty(RenderCharacter renderCharacter) =>
        renderCharacter.background.a == 0f &&
        (!IsCharacterDrawable(renderCharacter.character, renderCharacter.style) ||
         renderCharacter.foreground.a == 0f);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public virtual bool IsCharacterDrawable(char character, RenderStyle style) =>
        font?.IsCharacterDrawable(character, style & RenderStyle.AllPerFont) ?? false;
}
