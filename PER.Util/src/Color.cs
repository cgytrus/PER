using System;
using System.Numerics;
using System.Runtime.CompilerServices;

using JetBrains.Annotations;

namespace PER.Util;

[PublicAPI]
public readonly struct Color : IEquatable<Color> {
    public static Color transparent => new(0f, 0f, 0f, 0f);
    public static Color black => new(0f, 0f, 0f, 1f);
    public static Color white => new(1f, 1f, 1f, 1f);

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

    public float a {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        get => _vec.W;
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        init => _vec.W = value;
    }

    private readonly Vector4 _vec;

    public Color(Vector4 vec) => _vec = vec;
    public Color(float r, float g, float b, float a = 1f) => _vec = new Vector4(r, g, b, a);
    public Color(byte r, byte g, byte b, byte a = 255) => _vec = new Vector4(r / 255f, g / 255f, b / 255f, a / 255f);
    public Color(Color3 rgb, float a = 1f) => _vec = new Vector4(rgb.r, rgb.g, rgb.b, a);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Color Blend(Color bottom, Color top) {
        // i've spent SO MUCH time fixing this bug_
        // so, when i tried drawing something over a character which previously had a transparent background,
        // the background was transparent, even tho i wasn't drawing it with a transparent background.
        // i tried *everything*, and when i finally decided to actually debug it,
        // it turned out the the RGB of the color was NaN for some reason.
        // i immediately realized i was dividing by 0 somewhere.
        // i went here, and the only place that had division was... yep, it's here, RGB of the color.
        // when i drew transparent over transparent, both bottom.a and top.a were 0,
        // which caused a to be 0, which caused a division by 0, which caused the RGB of the color be NaN,NaN,NaN,
        // which caused any other operation with that color return NaN, which was displaying as if it was black...
        // i wanna f---ing die.
        if(bottom.a == 0f)
            return top;
        if(top.a == 0f)
            return bottom;

        float t = (1f - top.a) * bottom.a;
        float a = t + top.a;

        float r = (t * bottom.r + top.a * top.r) / a;
        float g = (t * bottom.g + top.a * top.g) / a;
        float b = (t * bottom.b + top.a * top.b) / a;

        return new Color(r, g, b, a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Color Blend(Color top) => Blend(this, top);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Color LerpColors(Color a, Color b, float t) =>
        t <= 0f ? a : t >= 1f ? b : new Color(Vector4.Lerp(a._vec, b._vec, t));

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Color operator +(Color left, Color right) => new(left._vec + right._vec);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Color operator -(Color left, Color right) => new(left._vec - right._vec);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Color operator *(Color left, Color right) => new(left._vec * right._vec);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Color operator *(Color left, float right) => new(left._vec * right);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Color operator *(float left, Color right) => right * left;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Color operator /(Color left, float right) => new(left._vec / right);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Color operator /(Color left, Color right) => new(left._vec / right._vec);

    public bool Equals(Color other) => _vec.Equals(other._vec);

    public override bool Equals(object? obj) => obj is Color other && Equals(other);

    public override int GetHashCode() => _vec.GetHashCode();

    public static bool operator ==(Color left, Color right) => left.Equals(right);
    public static bool operator !=(Color left, Color right) => !left.Equals(right);
}
