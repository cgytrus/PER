using System;
using System.Collections.Generic;

using JetBrains.Annotations;

using PER.Util;

namespace PER.Abstractions.Rendering;

[PublicAPI]
public interface IRenderer {
    public Vector2Int size { get; }
    public int width => size.x;
    public int height => size.y;
    public IFont font { get; }
    public bool verticalSync { get; set; }

    public bool open { get; }
    public bool focused { get; }
    public event EventHandler? focusChanged;
    public event EventHandler? closed;

    public Color background { get; set; }

    public Dictionary<string, IEffect?> formattingEffects { get; }

    public void Setup(RendererSettings settings);
    public void Close();
    public void Finish();

    public void BeginDraw();
    public void EndDraw();

    public void DrawCharacter(Vector2Int position, RenderCharacter character, IEffect? effect = null);

    public void DrawText(Vector2Int position, ReadOnlySpan<char> text, Func<char, Formatting> formatter,
        HorizontalAlignment align = HorizontalAlignment.Left, int maxWidth = 0);

    public void AddEffect(IEffect effect);
}
