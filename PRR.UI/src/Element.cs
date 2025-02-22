﻿using JetBrains.Annotations;

using PER.Abstractions;
using PER.Abstractions.Audio;
using PER.Abstractions.Meta;
using PER.Abstractions.Rendering;
using PER.Util;

namespace PRR.UI;

[PublicAPI, RequiresHead]
public abstract class Element : IUpdatable {
    public virtual bool enabled { get; set; } = true;
    public virtual Vector2Int position { get; set; }
    public virtual Vector2Int size { get; set; }

    public virtual Bounds bounds {
        get {
            Vector2Int position = this.position;
            Vector2Int size = this.size;
            return new Bounds(position, new Vector2Int(position.x + size.x - 1, position.y + size.y - 1));
        }
    }

    public virtual Vector2Int center =>
        new(position.x + (int)(size.x / 2f - 0.5f), position.y + (int)(size.y / 2f - 0.5f));

    public IEffect? effect { get; set; }

    public abstract Element Clone();

    public abstract void Input();
    public abstract void Update(TimeSpan time);

    public abstract void UpdateColors(Dictionary<string, Color> colors, List<string> layoutNames, string id,
        string? special);

    protected static bool TryGetColor(Dictionary<string, Color> colors, string type, List<string> layoutNames,
        string id, string colorName, string? special, out Color color) {
        foreach(string layoutName in layoutNames) {
            if(special is not null &&
                colors.TryGetValue($"{type}_{layoutName}.{id}_{colorName}_{special}", out color) ||
                special is not null && colors.TryGetValue($"{type}_@{id}_{colorName}_{special}", out color) ||
                special is not null && colors.TryGetValue($"{type}_{colorName}_{special}", out color) ||
                colors.TryGetValue($"{type}_{layoutName}.{id}_{colorName}", out color) ||
                colors.TryGetValue($"{type}_@{id}_{colorName}", out color) ||
                colors.TryGetValue($"{type}_{colorName}", out color))
                return true;
        }
        color = default(Color);
        return false;
    }
}
