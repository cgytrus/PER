using System;
using System.Runtime.CompilerServices;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using PER.Abstractions.Input;
using PER.Util;
using Color = PER.Util.Color;
using Image = PER.Abstractions.Rendering.Image;
using MouseButton = PER.Abstractions.Input.MouseButton;

namespace PER.Graphics.OpenGL;

public static class Converters {
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Color ToPerColor(Color4 color) => new(color.R, color.G, color.B, color.A);
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Color4 ToOtkColor(Color color) => new(color.r, color.g, color.b, color.a);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Vector2Int ToPerVector2Int(Vector2i vector) => new(vector.X, vector.Y);
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Vector2i ToOtkVector2Int(Vector2Int vector) => new(vector.x, vector.y);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static System.Numerics.Vector2 ToNumericsVector2(Vector2 vector) => new(vector.X, vector.Y);
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Vector2 ToOtkVector2(System.Numerics.Vector2 vector) => new(vector.X, vector.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static BlendMode ToPrrBlendMode(PER.Abstractions.Rendering.BlendMode blendMode) =>
        new(ToOtkFactorSrc(blendMode.colorSrcFactor),
            ToOtkFactorDest(blendMode.colorDstFactor),
            ToOtkEquation(blendMode.colorEquation),
            ToOtkFactorSrc(blendMode.alphaSrcFactor),
            ToOtkFactorDest(blendMode.alphaDstFactor),
            ToOtkEquation(blendMode.alphaEquation));

    private static BlendingFactorSrc ToOtkFactorSrc(PER.Abstractions.Rendering.BlendMode.Factor factor) =>
        factor switch {
            PER.Abstractions.Rendering.BlendMode.Factor.Zero => BlendingFactorSrc.Zero,
            PER.Abstractions.Rendering.BlendMode.Factor.One => BlendingFactorSrc.One,
            PER.Abstractions.Rendering.BlendMode.Factor.SrcColor => BlendingFactorSrc.SrcColor,
            PER.Abstractions.Rendering.BlendMode.Factor.OneMinusSrcColor => BlendingFactorSrc.OneMinusSrcColor,
            PER.Abstractions.Rendering.BlendMode.Factor.DstColor => BlendingFactorSrc.DstColor,
            PER.Abstractions.Rendering.BlendMode.Factor.OneMinusDstColor => BlendingFactorSrc.OneMinusDstColor,
            PER.Abstractions.Rendering.BlendMode.Factor.SrcAlpha => BlendingFactorSrc.SrcAlpha,
            PER.Abstractions.Rendering.BlendMode.Factor.OneMinusSrcAlpha => BlendingFactorSrc.OneMinusSrcAlpha,
            PER.Abstractions.Rendering.BlendMode.Factor.DstAlpha => BlendingFactorSrc.DstAlpha,
            PER.Abstractions.Rendering.BlendMode.Factor.OneMinusDstAlpha => BlendingFactorSrc.OneMinusDstAlpha,
            _ => throw new ArgumentOutOfRangeException(nameof(factor), factor, null)
        };
    private static BlendingFactorDest ToOtkFactorDest(PER.Abstractions.Rendering.BlendMode.Factor factor) =>
        factor switch {
            PER.Abstractions.Rendering.BlendMode.Factor.Zero => BlendingFactorDest.Zero,
            PER.Abstractions.Rendering.BlendMode.Factor.One => BlendingFactorDest.One,
            PER.Abstractions.Rendering.BlendMode.Factor.SrcColor => BlendingFactorDest.SrcColor,
            PER.Abstractions.Rendering.BlendMode.Factor.OneMinusSrcColor => BlendingFactorDest.OneMinusSrcColor,
            PER.Abstractions.Rendering.BlendMode.Factor.DstColor => BlendingFactorDest.DstColor,
            PER.Abstractions.Rendering.BlendMode.Factor.OneMinusDstColor => BlendingFactorDest.OneMinusDstColor,
            PER.Abstractions.Rendering.BlendMode.Factor.SrcAlpha => BlendingFactorDest.SrcAlpha,
            PER.Abstractions.Rendering.BlendMode.Factor.OneMinusSrcAlpha => BlendingFactorDest.OneMinusSrcAlpha,
            PER.Abstractions.Rendering.BlendMode.Factor.DstAlpha => BlendingFactorDest.DstAlpha,
            PER.Abstractions.Rendering.BlendMode.Factor.OneMinusDstAlpha => BlendingFactorDest.OneMinusDstAlpha,
            _ => throw new ArgumentOutOfRangeException(nameof(factor), factor, null)
        };
    private static BlendEquationMode ToOtkEquation(PER.Abstractions.Rendering.BlendMode.Equation factor) =>
        factor switch {
            PER.Abstractions.Rendering.BlendMode.Equation.Add => BlendEquationMode.FuncAdd,
            PER.Abstractions.Rendering.BlendMode.Equation.Subtract => BlendEquationMode.FuncSubtract,
            PER.Abstractions.Rendering.BlendMode.Equation.ReverseSubtract => BlendEquationMode.FuncReverseSubtract,
            PER.Abstractions.Rendering.BlendMode.Equation.Min => BlendEquationMode.Min,
            PER.Abstractions.Rendering.BlendMode.Equation.Max => BlendEquationMode.Max,
            _ => throw new ArgumentOutOfRangeException(nameof(factor), factor, null)
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static OpenTK.Windowing.Common.Input.Image ToOtkImage(Image image) {
        byte[] data = new byte[image.width * image.height * 4];
        for(int y = 0; y < image.height; y++) {
            for(int x = 0; x < image.width; x++) {
                int i = (y * image.width + x) * 4;
                data[i] = (byte)(image[x, y].r * byte.MaxValue);
                data[i + 1] = (byte)(image[x, y].g * byte.MaxValue);
                data[i + 2] = (byte)(image[x, y].b * byte.MaxValue);
                data[i + 3] = (byte)(image[x, y].a * byte.MaxValue);
            }
        }
        return new OpenTK.Windowing.Common.Input.Image(image.width, image.height, data);
    }
}
