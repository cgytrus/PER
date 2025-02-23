using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;

using JetBrains.Annotations;

using PER.Util;

namespace PER.Abstractions.Rendering;

[PublicAPI]
public class Font : IFont {
    public IReadOnlyDictionary<char, Vector2Int> characters => _characters;
    public Vector2Int size { get; }
    public Image image { get; private set; }
    public Image formattingImage { get; private set; }
    public string mappings { get; }

    private readonly bool[] _drawable = new bool[0xFFFF];

    private readonly Dictionary<char, Vector2Int> _characters = [];

    public Font(Image image, string mappings, Vector2Int size) {
        this.size = size;
        this.mappings = mappings;

        // TODO: compress better
        int index = 0;
        int maxX = 0;
        int maxY = 0;
        for (int y = 0; y < image.height; y += size.y) {
            for (int x = 0; x < image.width; x += size.x)
                AddCharacter(image, x, y, ref index, ref maxX, ref maxY);
        }
        this.image = new Image(maxX + size.x, maxY + size.y);
        this.image.DrawImage(new Vector2Int(), image);

        formattingImage = new Image(4, size.y);
        AddUnderline(formattingImage, 1, size.y);
        AddStrikethrough(formattingImage, 2, size.y);
        AddUnderline(formattingImage, 3, size.y);
        AddStrikethrough(formattingImage, 4, size.y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool IsCharacterDrawable(char character) => _drawable[character];

    private void AddCharacter(Image img, int x, int y, ref int index, ref int maxX, ref int maxY) {
        char character = mappings[index];
        index = (index + 1) % mappings.Length;

        _drawable[character] = true;

        if (IsCharacterEmpty(img, x, y, size))
            return;

        if (_characters.TryGetValue(character, out Vector2Int pos)) {
            for(int y1 = 0; y1 < size.y; y1++) {
                for (int x1 = 0; x1 < size.x; x1++)
                    img[pos.x + x1, pos.y + y1] = img[pos.x + x1, pos.y + y1].Blend(img[x + x1, y + y1]);
            }
            return;
        }

        _characters[character] = new Vector2Int(x, y);
        if (x > maxX)
            maxX = x;
        if (y > maxY)
            maxY = y;
    }

    private static void AddUnderline(Image image, int x, int height) {
        for (int i = 0; i < height / 10; i++)
            image[x, height - 1 - i] = Color.white;
    }

    private static void AddStrikethrough(Image image, int x, int height) {
        for (int i = 0; i < height / 10; i++)
            image[x, height * 9 / 20 + i] = Color.white;
    }

    private static bool IsCharacterEmpty(Image image, int startX, int startY, Vector2Int characterSize) {
        for (int y = 0; y < characterSize.y; y++) {
            for (int x = 0; x < characterSize.x; x++) {
                if (image[startX + x, startY + y].a != 0f)
                    return false;
            }
        }
        return true;
    }
}
