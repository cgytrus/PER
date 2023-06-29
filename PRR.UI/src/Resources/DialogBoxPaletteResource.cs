using JetBrains.Annotations;

using PER.Abstractions.Resources;
using PER.Util;

namespace PRR.UI.Resources;

[PublicAPI]
public class DialogBoxPaletteResource : Resource {
    public const string GlobalId = "layouts/dialogBoxPalette";

    // ReSharper disable once MemberCanBePrivate.Global
    public string palette { get; private set; } = "                ";

    public override void Preload() {
        AddPath("palette", "layouts/dialogBox.txt");
    }

    public override void Load(string id) {
        if(!TryGetPath("palette", out string? palettePath))
            return;
        string palette = File.ReadAllText(palettePath);
        if(palette.Length < 16)
            throw new InvalidDataException("Dialog box palette should be at least 16 characters long");
        this.palette = palette;
    }

    public override void Unload(string id) { }

    public char Get(int x, int y, Vector2Int size) =>
        Get(x == 0, x == size.x - 1, y == 0, y == size.y - 1);

    // ReSharper disable once MemberCanBePrivate.Global
    // haha micro optimization go brrrr
    public char Get(bool isStartX, bool isEndX, bool isStartY, bool isEndY) {
        unsafe {
            return BitConverter.IsLittleEndian ?
                palette[*(byte*)&isStartX << 3 | *(byte*)&isEndX << 2 | *(byte*)&isStartY << 1 | *(byte*)&isEndY] :
                palette[*(byte*)&isEndY << 3 | *(byte*)&isStartY << 2 | *(byte*)&isEndX << 1 | *(byte*)&isStartX];
        }
    }
}
