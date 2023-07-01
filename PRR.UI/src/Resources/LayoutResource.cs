using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

using JetBrains.Annotations;

using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Abstractions.UI;
using PER.Common.Resources;
using PER.Util;

namespace PRR.UI.Resources;

[PublicAPI]
public abstract class LayoutResource : JsonResource<IDictionary<string, LayoutResourceElement>> {
    [PublicAPI]
    public readonly record struct TextFormatting(string? foregroundColor, string? backgroundColor,
        [property: JsonConverter(typeof(JsonStringEnumConverter))] RenderStyle? style,
        [property: JsonConverter(typeof(JsonStringEnumConverter))] RenderOptions? options, string? effect = null) {
        public Formatting GetFormatting(Dictionary<string, Color> colors, Dictionary<string, IEffect?> effects) {
            Color foregroundColor = Color.white;
            Color backgroundColor = Color.transparent;
            RenderStyle style = RenderStyle.None;
            RenderOptions options = RenderOptions.Default;
            IEffect? effect = null;
            if(this.foregroundColor is not null && colors.TryGetValue(this.foregroundColor, out Color color))
                foregroundColor = color;
            if(this.backgroundColor is not null && colors.TryGetValue(this.backgroundColor, out color))
                backgroundColor = color;
            if(this.style.HasValue) style = this.style.Value;
            if(this.options.HasValue) options = this.options.Value;
            if(this.effect is not null) effects.TryGetValue(this.effect, out effect);
            return new Formatting(foregroundColor, backgroundColor, style, options, effect);
        }
    }
    [PublicAPI]
    public record LayoutResourceText(bool? enabled, Vector2Int position, Vector2Int size, string? text,
        Dictionary<char, TextFormatting>? formatting,
        [property: JsonConverter(typeof(JsonStringEnumConverter))] HorizontalAlignment? align, bool? wrap) :
        LayoutResourceElement(enabled, position, size) {
        public override Element GetElement(LayoutResource resource, IRenderer renderer, IInput input, IAudio audio,
            Dictionary<string, Color> colors, string layoutName, string id) {
            Text element = new(renderer) {
                position = position,
                size = size,
                text = text
            };
            if(text is null && resource.TryGetPath($"{id}.text", out string? filePath))
                element.text = File.ReadAllText(filePath);
            if(enabled.HasValue) element.enabled = enabled.Value;
            if(formatting is not null)
                foreach((char flag, TextFormatting textFormatting) in formatting)
                    element.formatting.Add(flag,
                        textFormatting.GetFormatting(colors, renderer.formattingEffects));
            if(align.HasValue) element.align = align.Value;
            if(wrap.HasValue) element.wrap = wrap.Value;
            element.UpdateColors(colors, layoutName, id, null);
            return element;
        }
    }
    [PublicAPI]
    public record LayoutResourceButton(bool? enabled, Vector2Int position, Vector2Int size, string? text,
        [property: JsonConverter(typeof(JsonStringEnumConverter))] RenderStyle? style, bool? active, bool? toggled) :
        LayoutResourceElement(enabled, position, size) {
        public override Element GetElement(LayoutResource resource, IRenderer renderer, IInput input, IAudio audio,
            Dictionary<string, Color> colors, string layoutName, string id) {
            Button element = new(renderer, input, audio) {
                position = position,
                size = size,
                text = text
            };
            if(enabled.HasValue) element.enabled = enabled.Value;
            if(style.HasValue) element.style = style.Value;
            if(active.HasValue) element.active = active.Value;
            if(toggled.HasValue) element.toggled = toggled.Value;
            element.UpdateColors(colors, layoutName, id, null);
            return element;
        }
    }
    [PublicAPI]
    public record LayoutResourceInputField(bool? enabled, Vector2Int position, Vector2Int size, string? value,
        string? placeholder, bool? wrap, int? cursor, float? blinkRate,
        [property: JsonConverter(typeof(JsonStringEnumConverter))] RenderStyle? style, bool? active) :
        LayoutResourceElement(enabled, position, size) {
        public override Element GetElement(LayoutResource resource, IRenderer renderer, IInput input, IAudio audio,
            Dictionary<string, Color> colors, string layoutName, string id) {
            InputField element = new(renderer, input, audio) {
                position = position,
                size = size,
                value = value,
                placeholder = placeholder
            };
            if(enabled.HasValue) element.enabled = enabled.Value;
            if(wrap.HasValue) element.wrap = wrap.Value;
            if(cursor.HasValue) element.cursor = cursor.Value;
            if(blinkRate.HasValue) element.blinkRate = blinkRate.Value;
            if(style.HasValue) element.style = style.Value;
            if(active.HasValue) element.active = active.Value;
            element.UpdateColors(colors, layoutName, id, null);
            return element;
        }
    }
    [PublicAPI]
    public record LayoutResourceSlider(bool? enabled, Vector2Int position, Vector2Int size, int? width, float? value,
        float? minValue, float? maxValue, bool? active) : LayoutResourceElement(enabled, position, size) {
        public override Element GetElement(LayoutResource resource, IRenderer renderer, IInput input, IAudio audio,
            Dictionary<string, Color> colors, string layoutName, string id) {
            Slider element = new(renderer, input, audio) {
                position = position,
                size = size
            };
            if(enabled.HasValue) element.enabled = enabled.Value;
            if(width.HasValue) element.width = width.Value;
            if(minValue.HasValue) element.minValue = minValue.Value;
            if(maxValue.HasValue) element.maxValue = maxValue.Value;
            if(value.HasValue) element.value = value.Value;
            if(active.HasValue) element.active = active.Value;
            element.UpdateColors(colors, layoutName, id, null);
            return element;
        }
    }
    [PublicAPI]
    public record LayoutResourceProgressBar(bool? enabled, Vector2Int position, Vector2Int size, float? value) :
        LayoutResourceElement(enabled, position, size) {
        public override Element GetElement(LayoutResource resource, IRenderer renderer, IInput input, IAudio audio,
            Dictionary<string, Color> colors, string layoutName, string id) {
            ProgressBar element = new(renderer) {
                position = position,
                size = size
            };
            if(enabled.HasValue) element.enabled = enabled.Value;
            if(value.HasValue) element.value = value.Value;
            element.UpdateColors(colors, layoutName, id, null);
            return element;
        }
    }
    [PublicAPI]
    public record LayoutResourceFilledPanel(bool? enabled, Vector2Int position, Vector2Int size, char? character,
        [property: JsonConverter(typeof(JsonStringEnumConverter))] RenderStyle? style) :
        LayoutResourceElement(enabled, position, size) {
        public override Element GetElement(LayoutResource resource, IRenderer renderer, IInput input, IAudio audio,
            Dictionary<string, Color> colors, string layoutName, string id) {
            FilledPanel element = new(renderer) {
                position = position,
                size = size
            };
            if(enabled.HasValue) element.enabled = enabled.Value;
            if(character.HasValue) element.character = character.Value;
            if(style.HasValue) element.style = style.Value;
            element.UpdateColors(colors, layoutName, id, null);
            return element;
        }
    }
    [PublicAPI]
    public record LayoutResourceScrollablePanel(bool? enabled, Vector2Int position, Vector2Int size) :
        LayoutResourceElement(enabled, position, size) {
        public override Element GetElement(LayoutResource resource, IRenderer renderer, IInput input, IAudio audio,
            Dictionary<string, Color> colors, string layoutName, string id) {
            ScrollablePanel element = new(renderer, input) {
                position = position,
                size = size
            };
            if(enabled.HasValue) element.enabled = enabled.Value;
            element.UpdateColors(colors, layoutName, id, null);
            return element;
        }
    }
    [PublicAPI]
    public record LayoutResourceListBox<TItem>(bool? enabled, Vector2Int position, Vector2Int size, string template) :
        LayoutResourceScrollablePanel(enabled, position, size) {
        public override Element GetElement(LayoutResource resource, IRenderer renderer, IInput input, IAudio audio,
            Dictionary<string, Color> colors, string layoutName, string id) {
            ListBoxTemplateResource<TItem> templateFactory =
                resource.GetDependency<ListBoxTemplateResource<TItem>>($"layouts/templates/{template}");
            ListBox<TItem> element = new(renderer, input, templateFactory) {
                position = position,
                size = size
            };
            if(enabled.HasValue) element.enabled = enabled.Value;
            element.UpdateColors(colors, layoutName, id, null);
            return element;
        }
    }

    protected abstract IRenderer renderer { get; }
    protected abstract IInput input { get; }
    protected abstract IAudio audio { get; }

    protected virtual string layoutsPath => "layouts";
    protected abstract string layoutName { get; }

    protected ColorsResource colors { get; private set; } = new();

    private Dictionary<string, Type> _elementTypes = new();

    protected IEnumerable<KeyValuePair<string, Element>> elements => _elements;
    protected IReadOnlyList<Element> elementList => _elementList;
    private Dictionary<string, Element> _elements = new();
    private List<Element> _elementList = new();

    public override void Preload() {
        _elementTypes.Clear();
        AddDependency<ColorsResource>(ColorsResource.GlobalId);
        AddPath("layout", $"{layoutsPath}/{layoutName}.json");
    }

    public override void Load(string id) {
        colors = GetDependency<ColorsResource>(ColorsResource.GlobalId);

        Dictionary<string, LayoutResourceElement> layoutElements = new(_elementTypes.Count);
        DeserializeAllJson("layout", layoutElements, () => layoutElements.Count == _elementTypes.Count);

        // didn't load all the elements
        if(layoutElements.Count != _elementTypes.Count)
            throw new InvalidOperationException("Not all elements were loaded.");

        _elements.Clear();
        _elementList.Clear();
        foreach((string elementId, LayoutResourceElement layoutElement) in layoutElements) {
            Element element =
                layoutElement.GetElement(this, renderer, input, audio, colors.colors, layoutName, elementId);
            _elements.Add(elementId, element);
            _elementList.Add(element);
        }
        _elementTypes.Clear();
    }

    protected override void DeserializeJson(string path, IDictionary<string, LayoutResourceElement> deserialized) {
        FileStream file = File.OpenRead(path);
        Dictionary<string, JsonElement>? layout = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(file);
        file.Close();

        if(layout is null)
            return;

        foreach((string elementId, Type elementType) in _elementTypes) {
            if(!layout.TryGetValue(elementId, out JsonElement jsonElement))
                throw new InvalidOperationException($"Element {elementId} is missing.");
            if(elementType.GetField("serializedType")?.GetValue(null) is not Type type)
                throw new InvalidOperationException($"Failed to deserialize {elementId}.");
            if(jsonElement.Deserialize(type) is not LayoutResourceElement layoutElement)
                throw new InvalidOperationException($"Failed to deserialize {elementId} as {type.Name}.");
            if(!deserialized.ContainsKey(elementId))
                deserialized.Add(elementId, layoutElement);
        }
    }

    public override void Unload(string id) {
        _elements.Clear();
        _elementList.Clear();
    }

    protected void AddElement<T>(string id) where T : Element {
        if(IResources.current is null || !IResources.current.loading)
            throw new InvalidOperationException("Cannot add elements while resources are not loading");
        if(_elementTypes.ContainsKey(id))
            throw new InvalidOperationException($"Element with ID {id} already registered.");
        _elementTypes.Add(id, typeof(T));
    }

    protected bool TryGetElement(string id, [NotNullWhen(true)] out Element? element) =>
        _elements.TryGetValue(id, out element);

    protected bool TryGetElement<T>(string id, [NotNullWhen(true)] out T? element) where T : Element {
        element = null;
        if(!TryGetElement(id, out Element? untypedElement) || untypedElement is not T typedElement)
            return false;
        element = typedElement;
        return true;
    }

    protected Element GetElement(string id) {
        if(!TryGetElement(id, out Element? element))
            throw new InvalidOperationException($"Element {id} does not exist.");
        return element;
    }

    protected T GetElement<T>(string id) where T : Element {
        if(GetElement(id) is not T typedElement)
            throw new InvalidOperationException($"Element {id} is not {nameof(T)}.");
        return typedElement;
    }
}
