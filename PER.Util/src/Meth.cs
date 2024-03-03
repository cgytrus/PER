using System.Runtime.CompilerServices;

using JetBrains.Annotations;

namespace PER.Util;

[PublicAPI]
public static class Meth {
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static double LerpUnclamped(double a, double b, double t) => (a * (1d - t)) + (b * t);
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static float LerpUnclamped(float a, float b, float t) => (a * (1f - t)) + (b * t);
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static double Lerp(double a, double b, double t) => t <= 0d ? a : t >= 1d ? b : LerpUnclamped(a, b, t);
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static float Lerp(float a, float b, float t) => t <= 0f ? a : t >= 1f ? b : LerpUnclamped(a, b, t);

    // https://stackoverflow.com/a/28060018
    // all my homies hate negative numbers
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int FloorDiv(int a, int b) {
        unsafe {
            bool cond = ((a < 0) ^ (b < 0)) && (a % b != 0);
            return a / b - *(byte*)&cond;
        }
    }
    // https://stackoverflow.com/a/1082938
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int Mod(int a, int b) => ((a % b) + b) % b;
}
