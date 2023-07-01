using System;
using System.Collections.Generic;

using PER.Abstractions.Rendering;
using PER.Util;

namespace PER.Headless;

internal class HeadlessRenderer : IRenderer {
    public bool open { get; private set; }
    public void Setup(RendererSettings settings) => open = true;
    public void Close() {
        open = false;
        closed?.Invoke(this, EventArgs.Empty);
    }

    // everything else is an invalid operation.

    public string title {
        get => throw new InvalidOperationException();
        set => throw new InvalidOperationException();
    }
    public int width => throw new InvalidOperationException();
    public int height => throw new InvalidOperationException();
    public bool verticalSync {
        get => throw new InvalidOperationException();
        set => throw new InvalidOperationException();
    }
    public bool fullscreen {
        get => throw new InvalidOperationException();
        set => throw new InvalidOperationException();
    }
    public IFont? font {
        get => throw new InvalidOperationException();
        set => throw new InvalidOperationException();
    }
    public string? icon {
        get => throw new InvalidOperationException();
        set => throw new InvalidOperationException();
    }
    public bool focused => throw new InvalidOperationException();
    // it's an interface implementation dumbass
#pragma warning disable CS0067
    public event EventHandler? focusChanged;
#pragma warning restore CS0067
    public event EventHandler? closed;
    public Color background {
        get => throw new InvalidOperationException();
        set => throw new InvalidOperationException();
    }
    public Dictionary<string, IEffect?> formattingEffects => throw new InvalidOperationException();
    public void Finish() => throw new InvalidOperationException();
    public bool Reset(RendererSettings settings) => throw new InvalidOperationException();
    public void Clear() => throw new InvalidOperationException();
    public void Update(TimeSpan time) => throw new InvalidOperationException();
    public void Draw() => throw new InvalidOperationException();
    public void DrawCharacter(Vector2Int position, RenderCharacter character,
        RenderOptions options = RenderOptions.Default, IEffect? effect = null) => throw new InvalidOperationException();
    public void DrawText(Vector2Int position, ReadOnlySpan<char> text, Func<char, Formatting> formatter,
        HorizontalAlignment align = HorizontalAlignment.Left, int maxWidth = 0) =>
        throw new InvalidOperationException();
    public RenderCharacter GetCharacter(Vector2Int position) => throw new InvalidOperationException();
    public void AddEffect(IEffect effect) => throw new InvalidOperationException();
    public void AddEffect(Vector2Int position, IEffect? effect) => throw new InvalidOperationException();
    public bool IsCharacterEmpty(Vector2Int position) => throw new InvalidOperationException();
    public bool IsCharacterEmpty(RenderCharacter renderCharacter) => throw new InvalidOperationException();
    public bool IsCharacterDrawable(char character, RenderStyle style) => throw new InvalidOperationException();
}
