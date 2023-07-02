using System;
using System.Collections.Generic;

using JetBrains.Annotations;

using PER.Util;

namespace PER.Abstractions.Rendering;

[PublicAPI]
public interface IRenderer : IUpdatable {
    public string title { get; set; }
    public int width { get; }
    public int height { get; }
    public Vector2Int size => new(width, height);
    public bool verticalSync { get; set; }
    public bool fullscreen { get; set; }
    public IFont? font { get; set; }
    public string? icon { get; set; }

    public bool open { get; }
    public bool focused { get; }
    public event EventHandler? focusChanged;
    public event EventHandler? closed;

    public Color background { get; set; }

    public Dictionary<string, IDisplayEffect?> formattingEffects { get; }

    public void Setup(RendererSettings settings);
    public void Close();
    public void Finish();
    public bool Reset(RendererSettings settings);

    public void Draw();
    public void DrawCharacter(Vector2Int position, RenderCharacter character, IDisplayEffect? effect = null);
    public void DrawText(Vector2Int position, ReadOnlySpan<char> text, Func<char, Formatting> formatter,
        HorizontalAlignment align = HorizontalAlignment.Left, int maxWidth = 0);

    public RenderCharacter GetCharacter(Vector2Int position);

    public void AddEffect(IEffect effect);
    public void AddEffect(Vector2Int position, IDisplayEffect? effect);

    public bool IsCharacterEmpty(Vector2Int position);
    public bool IsCharacterEmpty(RenderCharacter renderCharacter);
    public bool IsCharacterDrawable(char character, RenderStyle style);
}
