using System;

using JetBrains.Annotations;

namespace PER.Abstractions.Resources;

[PublicAPI]
public readonly struct ResourcePackData(string name, string fullPath, ResourcePackMeta meta)
    : IEquatable<ResourcePackData> {
    public string name { get; } = name;
    public string fullPath { get; } = fullPath;
    public ResourcePackMeta meta { get; } = meta;

    public bool Equals(ResourcePackData other) => fullPath == other.fullPath;
    public override bool Equals(object? obj) => obj is ResourcePackData other && Equals(other);
    public override int GetHashCode() => fullPath.GetHashCode();
    public static bool operator ==(ResourcePackData left, ResourcePackData right) => left.Equals(right);
    public static bool operator !=(ResourcePackData left, ResourcePackData right) => !left.Equals(right);
}
