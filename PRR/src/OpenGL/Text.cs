using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

using PER.Abstractions.Rendering;
using PER.Util;

namespace PRR.OpenGL;

public class Text : IDisposable {
    public int imageWidth { get; }
    public int imageHeight { get; }

    private readonly Vector2Int _size;
    private readonly RenderCharacter[,] _display;
    private readonly bool[,] _displayUsed;
    private readonly IDisplayEffect?[,] _displayEffects;
    private readonly List<IDrawableEffect> _globalDrawableEffects;
    private readonly List<IModifierEffect> _globalModEffects;
    private readonly int _charWidth;
    private readonly int _charHeight;
    private readonly System.Numerics.Vector2 _charBottomRight;
    private readonly System.Numerics.Vector2 _charTopRight;
    private readonly System.Numerics.Vector2 _charTopLeft;
    private readonly Vertex[] _quads;
    private readonly int _font;
    private readonly int _formatting;
    private readonly int _vao;
    private readonly int _vbo;
    private readonly Vector2[]?[] _characters;
    private readonly Vector2[] _emptyCoords = { new(), new(), new(), new() };

    private int _quadCount;

    [StructLayout(LayoutKind.Sequential)]
    private struct Vertex {
        public Vector2 position;
        public Vector4 texCoord;
        public Color4 backgroundColor;
        public Color4 foregroundColor;
    }

    public Text(IFont? font, Vector2Int size, RenderCharacter[,] display, bool[,] displayUsed,
        IDisplayEffect?[,] displayEffects, List<IDrawableEffect> globalDrawableEffects,
        List<IModifierEffect> globalModEffects) {
        _size = size;
        _display = display;
        _displayUsed = displayUsed;
        _displayEffects = displayEffects;
        _globalDrawableEffects = globalDrawableEffects;
        _globalModEffects = globalModEffects;

        _characters = new Vector2[]?[0xFFFF];

        _font = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _font);
        if(font is null)
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 0, 0, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, Array.Empty<byte>());
        else {
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, font.image.width, font.image.height, 0,
                PixelFormat.Rgba, PixelType.Float,
                font.image.pixels.Cast<Color>().Select(Converters.ToOtkColor).ToArray());
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            foreach((char c, System.Numerics.Vector2[] arr) in font.characters)
                _characters[c] =
                    arr.Select(x => Converters.ToOtkVector2(x) / new Vector2(font.image.width, font.image.height))
                        .ToArray();
        }
        GL.BindTexture(TextureTarget.Texture2D, 0);

        _formatting = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _formatting);
        if(font is null)
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 0, 0, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, Array.Empty<byte>());
        else {
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, font.formattingImage.width,
                font.formattingImage.height, 0, PixelFormat.Rgba, PixelType.Float,
                font.formattingImage.pixels.Cast<Color>().Select(Converters.ToOtkColor).ToArray());
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        }
        GL.BindTexture(TextureTarget.Texture2D, 0);

        _charWidth = font?.size.x ?? 0;
        _charHeight = font?.size.y ?? 0;
        _charBottomRight = new System.Numerics.Vector2(_charWidth, 0f);
        _charTopRight = new System.Numerics.Vector2(_charWidth, _charHeight);
        _charTopLeft = new System.Numerics.Vector2(0f, _charHeight);
        imageWidth = size.x * _charWidth;
        imageHeight = size.y * _charHeight;

        _quads = new Vertex[6 * size.x * size.y];

        // TODO: use mapping

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        GL.BindVertexArray(_vao);

        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * 14 * _quads.Length, _quads, BufferUsageHint.StreamDraw);

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, sizeof(float) * 14, 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, sizeof(float) * 14, sizeof(float) * 2);
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, sizeof(float) * 14, sizeof(float) * 6);
        GL.EnableVertexAttribArray(3);
        GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, sizeof(float) * 14, sizeof(float) * 10);

        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
    }

    public void RebuildQuads() {
        _quadCount = 0;
        for(int y = 0; y < _size.y; y++)
            for(int x = 0; x < _size.x; x++)
                BuildQuad(new Vector2Int(x, y));

        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, sizeof(float) * 14 * _quadCount, _quads);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
    }

    private void BuildQuad(Vector2Int pos) {
        // ReSharper disable once ForCanBeConvertedToForeach
        for(int i = 0; i < _globalDrawableEffects.Count; i++)
            _globalDrawableEffects[i].Draw(pos);

        IDisplayEffect? displayEffect = _displayEffects[pos.y, pos.x];
        if(displayEffect is IDrawableEffect drawableEffect)
            drawableEffect.Draw(pos);

        if(!_displayUsed[pos.y, pos.x]) {
            _displayEffects[pos.y, pos.x] = null;
            return;
        }
        _displayUsed[pos.y, pos.x] = false;

        RenderCharacter character = _display[pos.y, pos.x];
        System.Numerics.Vector2 modPosition = new(pos.x, pos.y);

        if(displayEffect is IModifierEffect modifierEffect)
            modifierEffect.ApplyModifiers(pos, ref modPosition, ref character);

        // ReSharper disable once ForCanBeConvertedToForeach
        for(int i = 0; i < _globalModEffects.Count; i++)
            _globalModEffects[i].ApplyModifiers(pos, ref modPosition, ref character);

        System.Numerics.Vector2 position = new(modPosition.X * _charWidth, modPosition.Y * _charHeight);
        Color4 background = Converters.ToOtkColor(character.background);

        bool bold = (character.style & RenderStyle.Bold) != 0;
        bool underline = (character.style & RenderStyle.Underline) != 0;
        bool strikethrough = (character.style & RenderStyle.Strikethrough) != 0;
        bool italic = (character.style & RenderStyle.Italic) != 0;
        System.Numerics.Vector2 italicOffset = new(italic.ToByte(), 0f);

        _quads[_quadCount].position = Converters.ToOtkVector2(position + italicOffset);
        _quads[_quadCount + 1].position = Converters.ToOtkVector2(position + _charTopLeft);
        _quads[_quadCount + 2].position = Converters.ToOtkVector2(position + _charBottomRight + italicOffset);
        _quads[_quadCount + 3].position = _quads[_quadCount + 1].position;
        _quads[_quadCount + 4].position = _quads[_quadCount + 2].position;
        _quads[_quadCount + 5].position = Converters.ToOtkVector2(position + _charTopRight);

        _quads[_quadCount].backgroundColor = background;
        _quads[_quadCount + 1].backgroundColor = background;
        _quads[_quadCount + 2].backgroundColor = background;
        _quads[_quadCount + 3].backgroundColor = background;
        _quads[_quadCount + 4].backgroundColor = background;
        _quads[_quadCount + 5].backgroundColor = background;

        Vector2[] texCoords = _characters[character.character] ?? _emptyCoords;

        Color4 foreground = Converters.ToOtkColor(character.foreground);

        Vector2 styleCoords = new(underline ? 0.5f : 0f, strikethrough ? 0.5f : 0f);

        _quads[_quadCount].texCoord = new Vector4(texCoords[0].X, texCoords[0].Y, styleCoords.X, styleCoords.Y);
        _quads[_quadCount + 1].texCoord = new Vector4(texCoords[3].X, texCoords[3].Y, styleCoords.X, styleCoords.Y + 0.5f);
        _quads[_quadCount + 2].texCoord = new Vector4(texCoords[1].X, texCoords[1].Y, styleCoords.X + 0.5f, styleCoords.Y);
        _quads[_quadCount + 3].texCoord = new Vector4(texCoords[3].X, texCoords[3].Y, styleCoords.X, styleCoords.Y + 0.5f);
        _quads[_quadCount + 4].texCoord = new Vector4(texCoords[1].X, texCoords[1].Y, styleCoords.X + 0.5f, styleCoords.Y);
        _quads[_quadCount + 5].texCoord = new Vector4(texCoords[2].X, texCoords[2].Y, styleCoords.X + 0.5f, styleCoords.Y + 0.5f);

        _quads[_quadCount].foregroundColor = foreground;
        _quads[_quadCount + 1].foregroundColor = foreground;
        _quads[_quadCount + 2].foregroundColor = foreground;
        _quads[_quadCount + 3].foregroundColor = foreground;
        _quads[_quadCount + 4].foregroundColor = foreground;
        _quads[_quadCount + 5].foregroundColor = foreground;

        if(!bold) {
            _quadCount += 6;
            return;
        }

        _quads[_quadCount + 6].position = _quads[_quadCount].position + new Vector2(1f, 0f);
        _quads[_quadCount + 7].position = _quads[_quadCount + 1].position + new Vector2(1f, 0f);
        _quads[_quadCount + 8].position = _quads[_quadCount + 2].position + new Vector2(1f, 0f);
        _quads[_quadCount + 9].position = _quads[_quadCount + 3].position + new Vector2(1f, 0f);
        _quads[_quadCount + 10].position = _quads[_quadCount + 4].position + new Vector2(1f, 0f);
        _quads[_quadCount + 11].position = _quads[_quadCount + 5].position + new Vector2(1f, 0f);

        _quads[_quadCount + 6].backgroundColor = background;
        _quads[_quadCount + 7].backgroundColor = background;
        _quads[_quadCount + 8].backgroundColor = background;
        _quads[_quadCount + 9].backgroundColor = background;
        _quads[_quadCount + 10].backgroundColor = background;
        _quads[_quadCount + 11].backgroundColor = background;

        _quads[_quadCount + 6].texCoord = _quads[_quadCount].texCoord;
        _quads[_quadCount + 7].texCoord = _quads[_quadCount + 1].texCoord;
        _quads[_quadCount + 8].texCoord = _quads[_quadCount + 2].texCoord;
        _quads[_quadCount + 9].texCoord = _quads[_quadCount + 3].texCoord;
        _quads[_quadCount + 10].texCoord = _quads[_quadCount + 4].texCoord;
        _quads[_quadCount + 11].texCoord = _quads[_quadCount + 5].texCoord;

        _quads[_quadCount + 6].foregroundColor = foreground;
        _quads[_quadCount + 7].foregroundColor = foreground;
        _quads[_quadCount + 8].foregroundColor = foreground;
        _quads[_quadCount + 9].foregroundColor = foreground;
        _quads[_quadCount + 10].foregroundColor = foreground;
        _quads[_quadCount + 11].foregroundColor = foreground;

        _quadCount += 12;
    }

    public void DrawQuads(BlendMode blendMode) {
        blendMode.Use();
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _font);
        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2D, _formatting);
        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, _quadCount);
    }

    public void Dispose() {
        GL.DeleteVertexArray(_vao);
        GL.DeleteBuffer(_vbo);
        GL.DeleteTexture(_font);
        GC.SuppressFinalize(this);
    }
}
