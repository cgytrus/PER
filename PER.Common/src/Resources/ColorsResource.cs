using System.Text.Json;

using JetBrains.Annotations;

using PER.Abstractions.Resources;
using PER.Util;

namespace PER.Common.Resources;

[PublicAPI]
public readonly struct ColorsResource : IResource<ColorsResource, IReadOnlyDictionary<string, Color>> {
    private Dictionary<string, Color> colors { get; init; }
    private Dictionary<string, string>? refs { get; init; }

    public IReadOnlyDictionary<string, Color> value => colors;
    public static string filePath => "graphics/colors.json";

    public static ColorsResource Load(string path) {
        Dictionary<string, Color> cols = [];
        Dictionary<string, string> refs = [];
        DeserializeJson(path, cols, refs);

        foreach ((string key, string value) in refs)
            if (cols.TryGetValue(value, out Color color))
                cols[key] = color;

        foreach (string key in cols.Keys)
            refs.Remove(key);

        return new ColorsResource { colors = cols, refs = refs.Count == 0 ? null : refs };
    }

    private static void DeserializeJson(string path, Dictionary<string, Color> cols, Dictionary<string, string> refs) {
        Dictionary<string, JsonElement>? elements;
        using (FileStream file = File.OpenRead(path)) {
            elements = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(file);
        }

        if (elements is null)
            return;

        foreach ((string? key, JsonElement element) in elements) {
            if (cols.ContainsKey(key) || refs.ContainsKey(key))
                continue;
            switch (element.ValueKind) {
                case JsonValueKind.Array when element.GetArrayLength() is 3 or 4:
                    cols.Add(key, new Color(element[0].GetByte(), element[1].GetByte(),
                        element[2].GetByte(), element.GetArrayLength() == 4 ? element[3].GetByte() : (byte)255));
                    break;
                case JsonValueKind.String:
                    refs.Add(key, element.GetString() ?? "");
                    break;
                case JsonValueKind.Undefined:
                case JsonValueKind.Null:
                    continue;
                case JsonValueKind.Object:
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                default:
                    throw new InvalidOperationException("Invalid color data.");
            }
        }
    }

    public static ColorsResource Merge(ColorsResource bottom, ColorsResource top) {
        Dictionary<string, Color> cols = [];
        foreach ((string key, Color color) in bottom.colors)
            cols[key] = color;
        foreach ((string key, Color color) in top.colors)
            cols[key] = color;
        if (top.refs is null)
            return new ColorsResource { colors = cols };
        foreach ((string key, string value) in top.refs) {
            if (cols.TryGetValue(value, out Color color))
                cols[key] = color;
        }
        return new ColorsResource { colors = cols };
    }

    public static ColorsResource Missing() => new() { colors = new Dictionary<string, Color> {
        { "transparent", new Color(0, 0, 255) }
    } };
}
