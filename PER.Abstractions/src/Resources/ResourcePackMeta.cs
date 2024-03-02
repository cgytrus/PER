using System.Text.Json.Serialization;

using JetBrains.Annotations;

namespace PER.Abstractions.Resources;

[PublicAPI]
[method: JsonConstructor]
public readonly struct ResourcePackMeta(string description, int version, bool major) {
    public string description { get; } = description;
    public int version { get; } = version;
    public bool major { get; } = major;
}
