using System.Text.Json.Serialization;

using JetBrains.Annotations;

using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Meta;
using PER.Abstractions.Rendering;
using PER.Util;

using PRR.UI.Resources;

namespace PRR.UI;

[PublicAPI]
public class Button : ClickableElement {
    public static readonly Type serializedType = typeof(LayoutResourceButton);

    protected override string type => "button";

    public KeyCode? hotkey { get; set; }

    public string? text { get; set; }
    public RenderStyle style { get; set; } = RenderStyle.None;

    public bool toggled {
        get => toggledSelf;
        set => toggledSelf = value;
    }

    protected override InputReq<bool>? hotkeyPressed {
        [RequiresHead]
        get => hotkey.HasValue ? input.Get<IKeyboard>().GetKey(hotkey.Value) : null;
    }

    private Func<char, Formatting> _formatter;

    public Button() => _formatter = _ => new Formatting(Color.white, Color.transparent, style, effect);

    public static Button Clone(Button template) => new() {
        enabled = template.enabled,
        position = template.position,
        size = template.size,
        effect = template.effect,
        hotkey = template.hotkey,
        text = template.text,
        style = template.style,
        active = template.active,
        toggled = template.toggled,
        inactiveColor = template.inactiveColor,
        idleColor = template.idleColor,
        hoverColor = template.hoverColor,
        clickColor = template.clickColor,
        clickSound = template.clickSound
    };

    public override Element Clone() => Clone(this);

    [RequiresHead]
    protected override void CustomUpdate(TimeSpan time) {
        if (text is null)
            return;
        renderer.DrawText(center, text, _formatter, HorizontalAlignment.Middle);
    }

    [RequiresHead]
    protected override void DrawCharacter(int x, int y, Color backgroundColor, Color foregroundColor) {
        Vector2Int position = new(this.position.x + x, this.position.y + y);
        renderer.DrawCharacter(position, new RenderCharacter('\0', backgroundColor, foregroundColor), effect);
    }

    private record LayoutResourceButton(bool? enabled, Vector2Int position, Vector2Int size, string? text,
        [property: JsonConverter(typeof(JsonStringEnumConverter))] RenderStyle? style, bool? active, bool? toggled) :
        LayoutResource.LayoutResourceElement(enabled, position, size) {
        public override Element GetElement(LayoutResource resource, Dictionary<string, Color> colors,
            List<string> layoutNames, string id) {
            Button element = new() {
                position = position,
                size = size,
                text = text
            };
            if(enabled.HasValue) element.enabled = enabled.Value;
            if(style.HasValue) element.style = style.Value;
            if(active.HasValue) element.active = active.Value;
            if(toggled.HasValue) element.toggled = toggled.Value;
            element.UpdateColors(colors, layoutNames, id, null);
            return element;
        }
    }
}
