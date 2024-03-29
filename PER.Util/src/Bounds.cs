﻿using System;
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
    // https://stackoverflow.com/a/100165/10484146
    public bool IntersectsLine(Vector2Int point0, Vector2Int point1) {
        // Find min and max X for the segment
        int minX = Math.Min(point0.x, point1.x);
        int maxX = Math.Max(point0.x, point1.x);

        // Find the intersection of the segment's and rectangle's X-projections
        if(maxX > max.x) maxX = max.x;
        if(minX < min.x) minX = min.x;

        // If their projections do not intersect return false
        if(minX > maxX) return false;

        // Find corresponding min and max Y for min and max X we found before
        int minY = point0.y;
        int maxY = point1.y;

        int dx = point1.x - point0.x;
        if(Math.Abs(dx) > 0) {
            int a = (point1.y - point0.y) / dx;
            int b = point0.y - a * point0.x;
            minY = a * minX + b;
            maxY = a * maxX + b;
        }

        if(minY > maxY) (maxY, minY) = (minY, maxY);

        if(maxY > max.y) maxY = max.y;
        if(minY < min.y) minY = min.y;

        // If Y-projections do not intersect return false
        return minY <= maxY;
    }

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
