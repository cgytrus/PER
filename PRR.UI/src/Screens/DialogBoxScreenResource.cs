using JetBrains.Annotations;

using PER.Abstractions;
using PER.Abstractions.Meta;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Abstractions.Screens;
using PER.Common.Resources;
using PER.Util;

using PRR.UI.Resources;

namespace PRR.UI.Screens;

[PublicAPI]
public abstract class DialogBoxScreenResource(Vector2Int size) : LayoutResource, IScreen, IUpdatable {
    protected Vector2Int size { get; set; } = size;

    protected virtual string foregroundColorId => "dialogBox_fg";
    protected virtual string backgroundColorId => "dialogBox_bg";
    protected virtual IEffect? frameEffect => null;

    private ColorsResource? _colors;
    private DialogBoxPaletteResource? _palette;

    [RequiresBody]
    public virtual void Open() {
        if (!resources.TryGetResource(ColorsResource.GlobalId, out _colors) ||
            !resources.TryGetResource(DialogBoxPaletteResource.GlobalId, out _palette))
            throw new InvalidOperationException("Missing dependency.");
    }

    public virtual void Close() {
        _colors = null;
        _palette = null;
    }

    [RequiresHead]
    public virtual void Update(TimeSpan time) {
        if (_colors is null || _palette is null ||
            !_colors.colors.TryGetValue(backgroundColorId, out Color backgroundColor) ||
            !_colors.colors.TryGetValue(foregroundColorId, out Color foregroundColor))
            return;

        Vector2Int offset = new((renderer.width - size.x) / 2, (renderer.height - size.y) / 2);
        for(int y = 0; y < size.y; y++) {
            for(int x = 0; x < size.x; x++) {
                char c = _palette.Get(x, y, size);
                Color fg = renderer.font.characters.ContainsKey(c) ? foregroundColor : backgroundColor;
                renderer.DrawCharacter(new Vector2Int(offset.x + x, offset.y + y),
                    new RenderCharacter(c, backgroundColor, fg), frameEffect);
            }
        }

        // ReSharper disable once ForCanBeConvertedToForeach
        for(int i = 0; i < elementList.Count; i++)
            elementList[i].Update(time);
    }
}
