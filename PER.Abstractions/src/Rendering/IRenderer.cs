using System;
using System.Collections.Generic;

using JetBrains.Annotations;
using PER.Abstractions.Meta;
using PER.Util;

namespace PER.Abstractions.Rendering;

[PublicAPI, RequiresHead]
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

    public void DrawCharacter(Vector2Int position, RenderCharacter character, IEffect? effect = null);

    public void DrawText(Vector2Int position, ReadOnlySpan<char> text, Func<char, Formatting> formatter,
        HorizontalAlignment align = HorizontalAlignment.Left, int maxWidth = 0) {
        if (text.Length == 0)
            return;

        char formattingFlag = '\0';
        int startIndex = 0;
        int currWidth = 0;
        int y = 0;
        for (int i = 0; i <= text.Length; i++) {
            char currentCharacter = i >= text.Length ? '\n' : text[i];

            if (maxWidth > 0 && currWidth >= maxWidth) {
                DrawCurrent(text);
                startIndex = i;
            }

            switch (currentCharacter) {
                case '\n':
                    DrawCurrent(text);
                    startIndex = i + 1;
                    break;
                case '\f': i++; // skip 2 characters
                    break;
                case not '\r': currWidth++;
                    break;
            }
            continue;

            void DrawCurrent(ReadOnlySpan<char> allText) {
                int x = GetAlignOffset(align, currWidth);
                DrawTextCharacter(position, allText, startIndex, x, y, currWidth, formatter, ref formattingFlag);

                currWidth = 0;
                y++;
            }
        }
    }

    private void DrawTextCharacter(Vector2Int position, ReadOnlySpan<char> text, int startIndex, int x, int y,
        int currWidth, Func<char, Formatting> formatter, ref char formattingFlag) {
        for (int i = startIndex; i < startIndex + currWidth; i++) {
            char toDraw = text[i];
            if (toDraw == '\f') {
                formattingFlag = text[++i];
                currWidth += 2;
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

    public void AddEffect(IEffect effect);

    public void Close();

    public interface IHandler {
        public RendererSettings SetupRenderer(IRenderer renderer);
        public void Render(IRenderer renderer);
    }

    public interface IImpl {
        public void Update();
    }
}
