﻿using System;

using JetBrains.Annotations;

namespace PER.Abstractions.Rendering;

// *i totally didn't just steal this from sfml*
[PublicAPI]
public readonly struct BlendMode(
    BlendMode.Factor colorSourceFactor,
    BlendMode.Factor colorDestinationFactor,
    BlendMode.Equation colorBlendEquation,
    BlendMode.Factor alphaSourceFactor,
    BlendMode.Factor alphaDestinationFactor,
    BlendMode.Equation alphaBlendEquation)
    : IEquatable<BlendMode> {
    [PublicAPI]
    public enum Factor {
        Zero, One, SrcColor, OneMinusSrcColor, DstColor, OneMinusDstColor, SrcAlpha, OneMinusSrcAlpha, DstAlpha,
        OneMinusDstAlpha
    }

    [PublicAPI]
    public enum Equation { Add, Subtract, ReverseSubtract, Min, Max }

    public static readonly BlendMode alpha =
        new(Factor.SrcAlpha, Factor.OneMinusSrcAlpha, Equation.Add, Factor.One, Factor.OneMinusSrcAlpha, Equation
            .Add);
    public static readonly BlendMode add =
        new(Factor.SrcAlpha, Factor.One, Equation.Add, Factor.One, Factor.One, Equation.Add);
    public static readonly BlendMode multiply = new(Factor.DstColor, Factor.Zero);
    public static readonly BlendMode min = new(Factor.One, Factor.One, Equation.Min);
    public static readonly BlendMode max = new(Factor.One, Factor.One, Equation.Max);
    public static readonly BlendMode none = new(Factor.One, Factor.Zero);

    public BlendMode(Factor sourceFactor, Factor destinationFactor, Equation blendEquation = Equation.Add)
        : this(sourceFactor, destinationFactor, blendEquation, sourceFactor, destinationFactor, blendEquation) { }

    public static bool operator ==(BlendMode left, BlendMode right) => left.Equals(right);

    public static bool operator !=(BlendMode left, BlendMode right) => !left.Equals(right);

    public override bool Equals(object? obj) => obj is BlendMode mode && Equals(mode);

    public bool Equals(BlendMode other) => colorSrcFactor == other.colorSrcFactor &&
                                           colorDstFactor == other.colorDstFactor &&
                                           colorEquation == other.colorEquation &&
                                           alphaSrcFactor == other.alphaSrcFactor &&
                                           alphaDstFactor == other.alphaDstFactor &&
                                           alphaEquation == other.alphaEquation;

    public override int GetHashCode() => HashCode.Combine(colorSrcFactor, colorDstFactor, colorEquation, alphaSrcFactor,
        alphaDstFactor, alphaEquation);

    public Factor colorSrcFactor { get; } = colorSourceFactor;
    public Factor colorDstFactor { get; } = colorDestinationFactor;
    public Equation colorEquation { get; } = colorBlendEquation;
    public Factor alphaSrcFactor { get; } = alphaSourceFactor;
    public Factor alphaDstFactor { get; } = alphaDestinationFactor;
    public Equation alphaEquation { get; } = alphaBlendEquation;
}
