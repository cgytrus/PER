using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using PER.Abstractions.Rendering;
using PER.Util;
using Color = PER.Util.Color;

namespace PER.Graphics.OpenGL;

public partial class Renderer : IRenderer {
    public Vector2Int size { get; }
    public IFont font => _settings.font;

    public bool verticalSync {
        get => _vsync;
        set {
            _vsync = value;
            window.VSync = value ? VSyncMode.On : VSyncMode.Off;
        }
    }

    public bool open => !_shouldClose;
    public bool focused => window.IsFocused;
    public event EventHandler? focusChanged;
    public event EventHandler? closed;

    public Color background {
        get => Converters.ToPerColor(_background);
        set => _background = Converters.ToOtkColor(value);
    }

    public NativeWindow window { get; }

    public Dictionary<string, IEffect?> formattingEffects { get; } = [];

    private readonly RendererSettings _settings;
    private bool _vsync;
    private Color4 _background = Color4.Black;
    private bool _shouldClose;

    private readonly bool[] _characters;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void DrawCharacter(Vector2Int position, RenderCharacter character, IEffect? effect = null) {
        Vector2Int offset = new(0, 0);
        (effect as IModifierEffect)?.ApplyModifiers(position, ref offset, ref character);
        foreach(IModifierEffect modEffect in _modEffects)
            modEffect.ApplyModifiers(position, ref offset, ref character);

        int bold = (int)(character.style & RenderStyle.Bold) >> 0;
        int underlineStrikethrough = (int)(character.style & (RenderStyle.Underline | RenderStyle.Strikethrough)) >> 1;
        int italic = (int)(character.style & RenderStyle.Italic) >> 3;

        _pixels.Add(new Pixel {
            position = new Vector2i(position.x, position.y),
            background = Converters.ToOtkColor(character.background),
            foreground = Converters.ToOtkColor(character.foreground),
            character = _characters[character.character] ? character.character : -1,
            style = new Vector3i(bold, underlineStrikethrough, italic),
            offset = Converters.ToOtkVector2Int(offset)
        });

        (effect as IDrawableEffect)?.Draw(position);
    }

    public void AddEffect(IEffect effect) {
        if (effect is IDrawableEffect drawable)
            _drawableEffects.Add(drawable);
        if (effect is IModifierEffect mod)
            _modEffects.Add(mod);
    }

    public void Close() {
        window.Close();
        _shouldClose = true;
    }
}
