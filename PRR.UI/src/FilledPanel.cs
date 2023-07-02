using JetBrains.Annotations;

using PER.Abstractions.Rendering;
using PER.Abstractions.UI;
using PER.Util;

using PRR.UI.Resources;

namespace PRR.UI;

[PublicAPI]
public class FilledPanel : Element {
    public static readonly Type serializedType = typeof(LayoutResource.LayoutResourceFilledPanel);

    public char character { get; set; } = '\0';
    public Color foregroundColor { get; set; } = Color.white;
    public Color backgroundColor { get; set; } = Color.transparent;
    public RenderStyle style { get; set; } = RenderStyle.None;

    public FilledPanel(IRenderer renderer) : base(renderer) { }

    public static FilledPanel Clone(FilledPanel template) => new(template.renderer) {
        enabled = template.enabled,
        position = template.position,
        size = template.size,
        effect = template.effect,
        character = template.character,
        backgroundColor = template.backgroundColor,
        foregroundColor = template.foregroundColor,
        style = template.style
    };

    public override Element Clone() => Clone(this);

    public override void Update(TimeSpan time) {
        if(!enabled)
            return;
        RenderCharacter rc = new(character, backgroundColor, foregroundColor, style);
        for(int y = bounds.min.y; y <= bounds.max.y; y++)
            for(int x = bounds.min.x; x <= bounds.max.x; x++)
                renderer.DrawCharacter(new Vector2Int(x, y), rc, effect);
    }

    public override void UpdateColors(Dictionary<string, Color> colors, string layoutName, string id, string? special) {
        if(TryGetColor(colors, "panel", layoutName, id, "fg", special, out Color color))
            foregroundColor = color;
        if(TryGetColor(colors, "panel", layoutName, id, "bg", special, out color))
            backgroundColor = color;
    }
}
