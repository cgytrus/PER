﻿using System;
using System.Collections.Generic;

using JetBrains.Annotations;

using PER.Util;

namespace PER.Abstractions.Rendering;

[PublicAPI]
public interface IRenderer {
    public int width { get; }
    public int height { get; }
    public Vector2Int size => new(width, height);
    public bool verticalSync { get; set; }
    public IFont font { get; }

    public bool open { get; }
    public bool focused { get; }
    public event EventHandler? focusChanged;
    public event EventHandler? closed;

    public Color background { get; set; }

    public Dictionary<string, IEffect?> formattingEffects { get; }

    public void Setup(RendererSettings settings);
    public void Close();
    public void Finish();
    public bool Reset(RendererSettings settings);

    public void BeginDraw();
    public void EndDraw();

    public void DrawCharacter(Vector2Int position, RenderCharacter character, IEffect? effect = null);

    public void DrawText(Vector2Int position, ReadOnlySpan<char> text, Func<char, Formatting> formatter,
        HorizontalAlignment align = HorizontalAlignment.Left, int maxWidth = 0);

    public void AddEffect(IEffect effect);
}
