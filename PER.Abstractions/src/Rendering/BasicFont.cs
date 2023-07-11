using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;

using JetBrains.Annotations;

using PER.Util;

namespace PER.Abstractions.Rendering;

[PublicAPI]
public abstract class BasicFont : IFont {
    public IReadOnlyDictionary<char, Vector2Int> characters => _characters;
    public Vector2Int size { get; }
    public Image image { get; private set; }
    public Image formattingImage { get; private set; }
    public string mappings { get; }

    private readonly bool[] _drawable = new bool[0xFFFF];

    private readonly Dictionary<char, Vector2Int> _characters = new();

    protected BasicFont(string imagePath, string mappingsPath) {
        string[] fontMappingsLines = File.ReadAllLines(mappingsPath);
        string[] fontSizeStr = fontMappingsLines[0].Split(',');
        mappings = fontMappingsLines[1];
        size = new Vector2Int(int.Parse(fontSizeStr[0], CultureInfo.InvariantCulture),
            int.Parse(fontSizeStr[1], CultureInfo.InvariantCulture));

        Setup(imagePath);
    }

    protected BasicFont(Image image, string mappings, Vector2Int size) {
        this.image = image;
        this.size = size;
        this.mappings = mappings;
        Setup();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool IsCharacterDrawable(char character) => _drawable[character];

    protected abstract Image ReadImage(string path);

    private void Setup(string imagePath) {
        image = ReadImage(imagePath);
        Setup();
    }

    private void Setup() {
        _characters.Clear();
        int index = 0;
        for(int y = 0; y < image.height; y += size.y)
            for(int x = 0; x < image.width; x += size.x)
                AddCharacter(x, y, ref index);
        formattingImage = new Image(4, size.y);
        AddUnderline(formattingImage, 1, size.y);
        AddStrikethrough(formattingImage, 2, size.y);
        AddUnderline(formattingImage, 3, size.y);
        AddStrikethrough(formattingImage, 4, size.y);
    }

    private void AddCharacter(int x, int y, ref int index) {
        if(mappings.Length <= index)
            index = 0;

        char character = mappings[index];
        _drawable[character] = true;

        if(IsCharacterEmpty(image, x, y, size)) {
            index++;
            return;
        }

        _characters.Add(character, new Vector2Int(x, y));

        index++;
    }

    private static void AddUnderline(Image image, int x, int height) {
        for(int i = 0; i < height / 10; i++)
            image[x, height - 1 - i] = Color.white;
    }

    private static void AddStrikethrough(Image image, int x, int height) {
        for(int i = 0; i < height / 10; i++)
            image[x, height * 9 / 20 + i] = Color.white;
    }

    private static bool IsCharacterEmpty(Image image, int startX, int startY, Vector2Int characterSize) {
        for(int y = startY; y < startY + characterSize.y; y++)
            for(int x = startX; x < startX + characterSize.x; x++)
                if(image[x, y].a != 0f)
                    return false;
        return true;
    }
}
