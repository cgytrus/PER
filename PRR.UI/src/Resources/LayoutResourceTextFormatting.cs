using System.Text.Json.Serialization;

using JetBrains.Annotations;

using PER.Abstractions.Rendering;
using PER.Util;

namespace PRR.UI.Resources;

[PublicAPI]
public readonly record struct LayoutResourceTextFormatting(string? foregroundColor, string? backgroundColor,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] RenderStyle? style, string? effect = null) {
    public Formatting GetFormatting(Dictionary<string, Color> colors, Dictionary<string, IDisplayEffect?> effects) {
        Color foregroundColor = Color.white;
        Color backgroundColor = Color.transparent;
        RenderStyle style = RenderStyle.None;
        IDisplayEffect? effect = null;
        if(this.foregroundColor is not null && colors.TryGetValue(this.foregroundColor, out Color color))
            foregroundColor = color;
        if(this.backgroundColor is not null && colors.TryGetValue(this.backgroundColor, out color))
            backgroundColor = color;
        if(this.style.HasValue) style = this.style.Value;
        if(this.effect is not null) effects.TryGetValue(this.effect, out effect);
        return new Formatting(foregroundColor, backgroundColor, style, effect);
    }
}
