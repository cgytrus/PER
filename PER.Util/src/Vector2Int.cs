﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

using JetBrains.Annotations;

namespace PER.Util;

[PublicAPI]
[JsonConverter(typeof(JsonConverter))]
[method: JsonConstructor]
public readonly struct Vector2Int(int x, int y) : IEquatable<Vector2Int> {
    public int x { get; } = x;
    public int y { get; } = y;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool InBounds(int minX, int minY, int maxX, int maxY) => x >= minX && x <= maxX && y >= minY && y <= maxY;
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool InBounds(Vector2Int min, Vector2Int max) => InBounds(min.x, min.y, max.x, max.y);
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool InBounds(Bounds bounds) => InBounds(bounds.min, bounds.max);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Vector2Int operator +(Vector2Int left, Vector2Int right) =>
        new(left.x + right.x, left.y + right.y);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Vector2Int operator -(Vector2Int left, Vector2Int right) =>
        new(left.x - right.x, left.y - right.y);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Vector2Int operator -(Vector2Int right) => new(-right.x, -right.y);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Vector2Int operator *(Vector2Int left, int right) =>
        new(left.x * right, left.y * right);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Vector2Int operator *(int left, Vector2Int right) => right * left;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Vector2Int operator /(Vector2Int left, int right) =>
        new(left.x / right, left.y / right);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool Equals(Vector2Int other) => x == other.x && y == other.y;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public override bool Equals(object? obj) => obj is Vector2Int other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public override int GetHashCode() => HashCode.Combine(x, y);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool operator ==(Vector2Int left, Vector2Int right) => left.Equals(right);
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool operator !=(Vector2Int left, Vector2Int right) => !left.Equals(right);

    public override string ToString() => ToString("G", CultureInfo.CurrentCulture);
    public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format) =>
        ToString(format, CultureInfo.CurrentCulture);
    public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format,
        IFormatProvider? formatProvider) {
        string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
        return $"<{x.ToString(format, formatProvider)}{separator} {y.ToString(format, formatProvider)}>";
    }

    public class JsonConverter : JsonConverter<Vector2Int> {
        public override Vector2Int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            bool isObject = true;
            if(reader.TokenType != JsonTokenType.Number) isObject = reader.TokenType == JsonTokenType.StartObject;
            reader.Read();
            int x = 0;
            int y = 0;
            if(isObject)
                for(int i = 0; i < 2; i++) {
                    string? propertyType = reader.GetString();
                    reader.Read();
                    switch(propertyType) {
                        case nameof(Vector2Int.x):
                            x = reader.GetInt32();
                            break;
                        case nameof(Vector2Int.y):
                            y = reader.GetInt32();
                            break;
                    }
                    reader.Read();
                }
            else {
                x = reader.GetInt32();
                reader.Read();
                y = reader.GetInt32();
            }
            return new Vector2Int(x, y);
        }

        public override void Write(Utf8JsonWriter writer, Vector2Int value, JsonSerializerOptions options) {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.x);
            writer.WriteNumberValue(value.y);
            writer.WriteEndArray();
        }
    }
}
