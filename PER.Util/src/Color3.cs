using System;
using System.Numerics;
using System.Runtime.CompilerServices;

using JetBrains.Annotations;

namespace PER.Util;

[PublicAPI]
public readonly struct Color3 : IEquatable<Color3> {
    public static Color3 black => new(0f, 0f, 0f);
    public static Color3 white => new(1f, 1f, 1f);

    public float r {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        get => _vec.X;
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        init => _vec.X = value;
    }

    public float g {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        get => _vec.Y;
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        init => _vec.Y = value;
    }

    public float b {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        get => _vec.Z;
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        init => _vec.Z = value;
    }

    private readonly Vector3 _vec;

    public Color3(Vector3 vec) => _vec = vec;
    public Color3(float r, float g, float b) => _vec = new Vector3(r, g, b);
    public Color3(byte r, byte g, byte b) => _vec = new Vector3(r / 255f, g / 255f, b / 255f);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Color3 LerpColors(Color3 a, Color3 b, float t) =>
        t <= 0f ? a : t >= 1f ? b : new Color3(Vector3.Lerp(a._vec, b._vec, t));

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Color3 operator +(Color3 left, Color3 right) => new(left._vec + right._vec);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Color3 operator -(Color3 left, Color3 right) => new(left._vec - right._vec);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Color3 operator *(Color3 left, Color3 right) => new(left._vec * right._vec);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Color3 operator *(Color3 left, float right) => new(left._vec * right);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Color3 operator *(float left, Color3 right) => right * left;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Color3 operator /(Color3 left, float right) => new(left._vec / right);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Color3 operator /(Color3 left, Color3 right) => new(left._vec / right._vec);

    public static implicit operator Color(Color3 x) => new(x.r, x.g, x.b, 1f);
    public static explicit operator Color3(Color x) => new(x.r, x.g, x.b);

    public bool Equals(Color3 other) => _vec.Equals(other._vec);

    public override bool Equals(object? obj) => obj is Color3 other && Equals(other);

    public override int GetHashCode() => _vec.GetHashCode();

    public static bool operator ==(Color3 left, Color3 right) => left.Equals(right);
    public static bool operator !=(Color3 left, Color3 right) => !left.Equals(right);
}
