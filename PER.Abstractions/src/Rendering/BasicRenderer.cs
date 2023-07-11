using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using JetBrains.Annotations;

using PER.Util;

namespace PER.Abstractions.Rendering;

[PublicAPI]
public abstract class BasicRenderer : IRenderer {
    public virtual int width => _settings.width;
    public virtual int height => _settings.height;

    public virtual bool verticalSync {
        get => _settings.verticalSync;
        set {
            _settings = _settings with { verticalSync = value };
            UpdateVerticalSync();
        }
    }

    public virtual IFont font => _settings.font;

    public abstract bool open { get; }
    public abstract bool focused { get; }
    public abstract event EventHandler? focusChanged;
    public abstract event EventHandler? closed;

    public virtual Color background { get; set; } = Color.black;

    public Dictionary<string, IDisplayEffect?> formattingEffects { get; } = new();

    protected List<IUpdatableEffect> updatableEffects { get; private set; } = new();
    protected List<IDrawableEffect> drawableEffects { get; private set; } = new();
    protected List<IModifierEffect> modEffects { get; private set; } = new();

    private RendererSettings _settings;

    public virtual void Setup(RendererSettings settings) => _settings = settings;

    protected abstract void UpdateVerticalSync();

    public abstract void Close();
    public abstract void Finish();

    public virtual bool Reset(RendererSettings settings) {
        if(settings.width == width && settings.height == height && settings.font == font &&
            settings.fullscreen == _settings.fullscreen) {
            _settings = settings;
            return false;
        }
        Finish();
        Setup(settings);
        return true;
    }

    public abstract void BeginDraw();
    public abstract void EndDraw();

    public abstract void DrawCharacter(Vector2Int position, RenderCharacter character, IDisplayEffect? effect = null);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public virtual void DrawText(Vector2Int position, ReadOnlySpan<char> text, Func<char, Formatting> formatter,
        HorizontalAlignment align = HorizontalAlignment.Left, int maxWidth = 0) {
        if(text.Length == 0)
            return;

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

    public virtual void AddEffect(IEffect effect) {
        // ReSharper disable once ConvertIfStatementToSwitchStatement
        if(effect is IUpdatableEffect updatable)
            updatableEffects.Add(updatable);
        if(effect is IDrawableEffect drawable)
            drawableEffects.Add(drawable);
        if(effect is IModifierEffect mod)
            modEffects.Add(mod);
    }
}
