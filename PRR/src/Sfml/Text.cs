using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using PER.Abstractions.Rendering;
using PER.Util;

using SFML.Graphics;
using SFML.System;

using BlendMode = SFML.Graphics.BlendMode;
using Color = SFML.Graphics.Color;
using Shader = SFML.Graphics.Shader;

namespace PRR.Sfml;

public class Text : IDisposable {
    public uint imageWidth { get; }
    public uint imageHeight { get; }

    private readonly Vector2Int _size;
    private readonly RenderCharacter[,] _display;
    private readonly bool[,] _displayUsed;
    private readonly IDisplayEffect?[,] _displayEffects;
    private readonly List<IDrawableEffect> _globalDrawableEffects;
    private readonly List<IModifierEffect> _globalModEffects;
    private readonly uint _charWidth;
    private readonly uint _charHeight;
    private readonly Vector2f _charBottomRight;
    private readonly Vector2f _charTopRight;
    private readonly Vector2f _charTopLeft;
    private readonly Vertex[] _quads;
    private readonly Texture _texture;
    private readonly Vector2f[] _backgroundCharacter;
    private readonly Vector2f[]?[] _characters;

    private uint _quadCount;

    public Text(IFont? font, Vector2Int size, RenderCharacter[,] display, bool[,] displayUsed,
        IDisplayEffect?[,] displayEffects, List<IDrawableEffect> globalDrawableEffects,
        List<IModifierEffect> globalModEffects) {
        _size = size;
        _display = display;
        _displayUsed = displayUsed;
        _displayEffects = displayEffects;
        _globalDrawableEffects = globalDrawableEffects;
        _globalModEffects = globalModEffects;
        _texture = font is null ? new Texture(0, 0) : new Texture(SfmlConverters.ToSfmlImage(font.image));
        _charWidth = (uint)(font?.size.x ?? 0);
        _charHeight = (uint)(font?.size.y ?? 0);
        _charBottomRight = new Vector2f(_charWidth, 0f);
        _charTopRight = new Vector2f(_charWidth, _charHeight);
        _charTopLeft = new Vector2f(0f, _charHeight);
        uint textWidth = (uint)size.x;
        uint textHeight = (uint)size.y;
        imageWidth = textWidth * _charWidth;
        imageHeight = textHeight * _charHeight;
        _quads = new Vertex[8 * textWidth * textHeight];
        _backgroundCharacter = font?.backgroundCharacter.Select(SfmlConverters.ToSfmlVector2).ToArray() ??
            Array.Empty<Vector2f>();
        _characters = new Vector2f[]?[0xFFFFFF];
        if(font is null)
            return;
        foreach(((char c, RenderStyle s), Vector2[] arr) in font.characters)
            _characters[(int)s | (c << (sizeof(RenderStyle) * 8))] = arr.Select(SfmlConverters.ToSfmlVector2).ToArray();
    }

    public void RebuildQuads(Vector2f offset) {
        uint index = 0;
        for(int y = 0; y < _size.y; y++) {
            for(int x = 0; x < _size.x; x++) {
                Vector2Int pos = new(x, y);

                // ReSharper disable once ForCanBeConvertedToForeach
                for(int i = 0; i < _globalDrawableEffects.Count; i++)
                    _globalDrawableEffects[i].Draw(pos);

                IDisplayEffect? displayEffect = _displayEffects[y, x];
                if(displayEffect is IDrawableEffect drawableEffect)
                    drawableEffect.Draw(pos);

                if(!_displayUsed[y, x]) {
                    _displayEffects[y, x] = null;
                    continue;
                }
                _displayUsed[y, x] = false;

                RenderCharacter character = _display[y, x];
                Vector2 modPosition = new(x, y);

                if(displayEffect is IModifierEffect modifierEffect)
                    modifierEffect.ApplyModifiers(pos, ref modPosition, ref character);

                // ReSharper disable once ForCanBeConvertedToForeach
                for(int i = 0; i < _globalModEffects.Count; i++)
                    _globalModEffects[i].ApplyModifiers(pos, ref modPosition, ref character);

                Vector2f position = new(modPosition.X * _charWidth + offset.X, modPosition.Y * _charHeight + offset.Y);
                Color background = SfmlConverters.ToSfmlColor(character.background);

                _quads[index].Position = position;
                _quads[index].Color = background;
                _quads[index].TexCoords = _backgroundCharacter[0];

                _quads[index + 1].Position = position + _charBottomRight;
                _quads[index + 1].Color = background;
                _quads[index + 1].TexCoords = _backgroundCharacter[1];

                _quads[index + 2].Position = position + _charTopRight;
                _quads[index + 2].Color = background;
                _quads[index + 2].TexCoords = _backgroundCharacter[2];

                _quads[index + 3].Position = position + _charTopLeft;
                _quads[index + 3].Color = background;
                _quads[index + 3].TexCoords = _backgroundCharacter[3];

                index += 4;

                int charIndex = (int)(character.style & RenderStyle.AllPerFont) |
                    (character.character << (sizeof(RenderStyle) * 8));
                Vector2f[]? texCoords = _characters[charIndex];
                if(texCoords is null)
                    continue;

                bool italic = (character.style & RenderStyle.Italic) != 0;
                Vector2f italicOffset = new(italic.ToByte(), 0f);
                Color foreground = SfmlConverters.ToSfmlColor(character.foreground);

                _quads[index].Position = position + italicOffset;
                _quads[index].Color = foreground;
                _quads[index].TexCoords = texCoords[0];

                _quads[index + 1].Position = position + _charBottomRight + italicOffset;
                _quads[index + 1].Color = foreground;
                _quads[index + 1].TexCoords = texCoords[1];

                _quads[index + 2].Position = position + _charTopRight;
                _quads[index + 2].Color = foreground;
                _quads[index + 2].TexCoords = texCoords[2];

                _quads[index + 3].Position = position + _charTopLeft;
                _quads[index + 3].Color = foreground;
                _quads[index + 3].TexCoords = texCoords[3];

                index += 4;
            }
        }
        _quadCount = index;
    }

    public void DrawQuads(RenderTarget target, BlendMode blendMode, Shader? shader = null) {
        shader?.SetUniform("font", _texture);
        target.Draw(_quads, 0, _quadCount, PrimitiveType.Quads,
            new RenderStates(blendMode, Transform.Identity, _texture, shader));
    }

    public void Dispose() {
        _texture.Dispose();
        GC.SuppressFinalize(this);
    }
}
