﻿using System.Text.Json.Serialization;

using JetBrains.Annotations;

using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Meta;
using PER.Abstractions.Rendering;
using PER.Util;

using PRR.UI.Resources;

namespace PRR.UI;

[PublicAPI]
public class Text : Element {
    public static readonly Type serializedType = typeof(LayoutResourceText);

    public string? text { get; set; }
    public Dictionary<char, Formatting> formatting { get; set; } = new();
    public HorizontalAlignment align { get; set; } = HorizontalAlignment.Left;
    public bool wrap { get; set; }

    private readonly Func<char, Formatting> _formatter;

    public Text() => _formatter = flag => formatting[flag];

    public static Text Clone(Text template) => new() {
        enabled = template.enabled,
        position = template.position,
        size = template.size,
        effect = template.effect,
        text = template.text,
        formatting = new Dictionary<char, Formatting>(template.formatting),
        align = template.align
    };

    public override Element Clone() => Clone(this);

    public override void Input() { }

    public override void Update(TimeSpan time) {
        if (!enabled || text is null)
            return;
        if (formatting.Count == 0)
            formatting.Add('\0',
                new Formatting(Color.white, Color.transparent, RenderStyle.None, effect));
        renderer.DrawText(position, text, _formatter, align, wrap ? size.x : 0);
    }

    public override void UpdateColors(Dictionary<string, Color> colors, List<string> layoutNames, string id,
        string? special) {
        Color foregroundColor = Color.white;
        Color backgroundColor = Color.transparent;
        if(TryGetColor(colors, "text", layoutNames, id, "fg", special, out Color color))
            foregroundColor = color;
        if(TryGetColor(colors, "text", layoutNames, id, "bg", special, out color))
            backgroundColor = color;
        formatting['\0'] = formatting.TryGetValue('\0', out Formatting oldFormatting) ?
            oldFormatting with { foregroundColor = foregroundColor, backgroundColor = backgroundColor } :
            new Formatting(foregroundColor, backgroundColor);
    }

    private record LayoutResourceText(bool? enabled, Vector2Int position, Vector2Int size, string? text,
        Dictionary<char, LayoutResourceTextFormatting>? formatting,
        [property: JsonConverter(typeof(JsonStringEnumConverter))] HorizontalAlignment? align, bool? wrap) :
        LayoutResource.LayoutResourceElement(enabled, position, size) {
        public override Element GetElement(LayoutResource resource, Dictionary<string, Color> colors,
            List<string> layoutNames, string id) {
            Text element = new() {
                position = position,
                size = size,
                text = text
            };
            if(text is null && TryGetPath(resource, $"{id}.text", out string? filePath))
                element.text = File.ReadAllText(filePath);
            if(enabled.HasValue) element.enabled = enabled.Value;
            if(formatting is not null)
                foreach((char flag, LayoutResourceTextFormatting textFormatting) in formatting)
                    element.formatting.Add(flag,
                        textFormatting.GetFormatting(colors, renderer.formattingEffects));
            if(align.HasValue) element.align = align.Value;
            if(wrap.HasValue) element.wrap = wrap.Value;
            element.UpdateColors(colors, layoutNames, id, null);
            return element;
        }
    }
}
