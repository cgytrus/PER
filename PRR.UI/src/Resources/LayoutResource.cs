using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using JetBrains.Annotations;

using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Common.Resources;
using PER.Util;

namespace PRR.UI.Resources;

[PublicAPI]
public abstract class LayoutResource : HeadResource {
    [PublicAPI]
    public abstract record LayoutResourceElement(bool? enabled, Vector2Int position, Vector2Int size) {
        public abstract Element GetElement(LayoutResource resource, Dictionary<string, Color> colors,
            List<string> layoutNames, string id);

        protected static bool TryGetPath(LayoutResource resource, string id,
            [NotNullWhen(true)] out string? fullPath) => resource.TryGetPath(id, out fullPath);

        protected static T GetDependency<T>(LayoutResource resource, string id) where T : IResource =>
            resource.GetDependency<T>(id);
    }

    protected virtual string layoutsPath => "layouts";

    protected ColorsResource colors { get; private set; } = new();

    private List<string> _layoutNames = new();
    private Dictionary<string, Type> _elementTypes = new();

    protected IEnumerable<KeyValuePair<string, Element>> elements => _elements;
    protected IReadOnlyList<Element> elementList => _elementList;
    private Dictionary<string, Element> _elements = new();
    private List<Element> _elementList = new();

    public override void Preload() {
        _elementTypes.Clear();
        AddDependency<ColorsResource>(ColorsResource.GlobalId);
    }

    public override void Load(string id) {
        colors = GetDependency<ColorsResource>(ColorsResource.GlobalId);

        Dictionary<string, LayoutResourceElement> layoutElements = new(_elementTypes.Count);
        foreach (string path in GetPaths("layout"))
            DeserializeJson(path, layoutElements);

        List<string> missing = new();
        foreach((string elementId, Type _) in _elementTypes)
            if(!layoutElements.ContainsKey(elementId))
                missing.Add(elementId);
        if(missing.Count > 0)
            throw new InvalidDataException(
                $"Missing elements in layout {id}: {string.Join(", ", missing)}.");

        _elements.Clear();
        _elementList.Clear();
        foreach((string elementId, LayoutResourceElement layoutElement) in layoutElements) {
            Element element =
                layoutElement.GetElement(this, colors.colors, _layoutNames, elementId);
            _elements.Add(elementId, element);
            _elementList.Add(element);
        }
        _elementTypes.Clear();
    }

    protected void DeserializeJson(string path, IDictionary<string, LayoutResourceElement> deserialized) {
        FileStream file = File.OpenRead(path);
        Dictionary<string, JsonElement>? layout = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(file);
        file.Close();

        if(layout is null)
            return;

        foreach((string elementId, JsonElement jsonElement) in layout) {
            if(!_elementTypes.TryGetValue(elementId, out Type? elementType))
                throw new InvalidOperationException($"Element {elementId} is extra.");
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

    protected void AddLayout(string name) {
        AddPath("layout", $"{layoutsPath}/{name}.json");
        _layoutNames.Insert(0, name);
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
