using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace PER.Abstractions.Resources;

[PublicAPI]
public readonly record struct ResourcePack(string name, string fullPath, ResourcePack.Meta meta) {
    [PublicAPI]
    [method: JsonConstructor]
    public readonly record struct Meta(string description, int version, bool major);

    public string name { get; } = name;
    public string fullPath { get; } = fullPath;
    public Meta meta { get; } = meta;

    public bool Equals(ResourcePack other) => fullPath == other.fullPath;
    public override int GetHashCode() => fullPath.GetHashCode();
}
