using System;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

using JetBrains.Annotations;

namespace PER.Util;

[PublicAPI]
[method: JsonConstructor]
public readonly struct Bounds(Vector2Int min, Vector2Int max) : IEquatable<Bounds> {
    public Vector2Int min { get; } = min;
    public Vector2Int max { get; } = max;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool Contains(Vector2Int point) => point.InBounds(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool Equals(Bounds other) => min.Equals(other.min) && max.Equals(other.max);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public override bool Equals(object? obj) => obj is Bounds other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public override int GetHashCode() => HashCode.Combine(min, max);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool operator ==(Bounds left, Bounds right) => left.Equals(right);
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool operator !=(Bounds left, Bounds right) => !left.Equals(right);
}
