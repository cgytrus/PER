using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

using PER.Abstractions.Rendering;
using PER.Util;

namespace PRR.Ogl;

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
    private readonly int _texture;
    private readonly int _vao;
    private readonly int _vbo;
    private readonly Vector2[]?[] _characters;

    private int _quadCount;

    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 12)]
    private struct Vertex {
        public Vector2 position;
        public Vector2 texCoord;
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

        _characters = new Vector2[]?[0xFFFFFF];

        _texture = GL.GenTexture();
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _texture);
        if(font is null)
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 0, 0, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, Array.Empty<byte>());
        else {
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, font.image.width, font.image.height, 0,
                PixelFormat.Rgba, PixelType.Float,
                font.image.pixels.Cast<Color>().Select(Converters.ToOtkColor).ToArray());
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            foreach(((char c, RenderStyle s), System.Numerics.Vector2[] arr) in font.characters) {
                _characters[(int)s | (c << (sizeof(RenderStyle) * 8))] = arr.Select(x =>
                    Converters.ToOtkVector2(x) / new Vector2(font.image.width, font.image.height)).ToArray();
            }
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
        GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * 12 * _quads.Length, _quads, BufferUsageHint.StreamDraw);

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, sizeof(float) * 12, 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, sizeof(float) * 12, sizeof(float) * 2);
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, sizeof(float) * 12, sizeof(float) * 4);
        GL.EnableVertexAttribArray(3);
        GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, sizeof(float) * 12, sizeof(float) * 8);

        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
    }

    public void RebuildQuads() {
        _quadCount = 0;
        for(int y = 0; y < _size.y; y++)
            for(int x = 0; x < _size.x; x++)
                BuildQuad(new Vector2Int(x, y));

        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, sizeof(float) * 12 * _quadCount, _quads);
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

        int charIndex = (int)(character.style & RenderStyle.AllPerFont) |
            (character.character << (sizeof(RenderStyle) * 8));
        Vector2[]? texCoords = _characters[charIndex];
        if(texCoords is null) {
            _quads[_quadCount].foregroundColor.A = 0f;
            _quads[_quadCount + 1].foregroundColor.A = 0f;
            _quads[_quadCount + 2].foregroundColor.A = 0f;
            _quads[_quadCount + 3].foregroundColor.A = 0f;
            _quads[_quadCount + 4].foregroundColor.A = 0f;
            _quads[_quadCount + 5].foregroundColor.A = 0f;
            _quadCount += 6;
            return;
        }

        Color4 foreground = Converters.ToOtkColor(character.foreground);

        _quads[_quadCount].texCoord = texCoords[0];
        _quads[_quadCount + 1].texCoord = texCoords[3];
        _quads[_quadCount + 2].texCoord = texCoords[1];
        _quads[_quadCount + 3].texCoord = texCoords[3];
        _quads[_quadCount + 4].texCoord = texCoords[1];
        _quads[_quadCount + 5].texCoord = texCoords[2];

        _quads[_quadCount].foregroundColor = foreground;
        _quads[_quadCount + 1].foregroundColor = foreground;
        _quads[_quadCount + 2].foregroundColor = foreground;
        _quads[_quadCount + 3].foregroundColor = foreground;
        _quads[_quadCount + 4].foregroundColor = foreground;
        _quads[_quadCount + 5].foregroundColor = foreground;

        _quadCount += 6;
    }

    public void DrawQuads(BlendMode blendMode) {
        blendMode.Use();
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _texture);
        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, _quadCount);
    }

    public void Dispose() {
        GL.DeleteVertexArray(_vao);
        GL.DeleteBuffer(_vbo);
        GL.DeleteTexture(_texture);
        GC.SuppressFinalize(this);
    }
}
