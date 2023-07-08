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

namespace PRR.OpenGL;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static KeyCode ToPerKey(Keys key) => key switch {
        Keys.Space => KeyCode.Space,
        Keys.Apostrophe => KeyCode.Quote,
        Keys.Comma => KeyCode.Comma,
        Keys.Minus => KeyCode.Hyphen,
        Keys.Period => KeyCode.Period,
        Keys.Slash => KeyCode.Slash,
        Keys.D0 => KeyCode.Num0,
        Keys.D1 => KeyCode.Num1,
        Keys.D2 => KeyCode.Num2,
        Keys.D3 => KeyCode.Num3,
        Keys.D4 => KeyCode.Num4,
        Keys.D5 => KeyCode.Num5,
        Keys.D6 => KeyCode.Num6,
        Keys.D7 => KeyCode.Num7,
        Keys.D8 => KeyCode.Num8,
        Keys.D9 => KeyCode.Num9,
        Keys.Semicolon => KeyCode.Semicolon,
        Keys.Equal => KeyCode.Equal,
        Keys.A => KeyCode.A,
        Keys.B => KeyCode.B,
        Keys.C => KeyCode.C,
        Keys.D => KeyCode.D,
        Keys.E => KeyCode.E,
        Keys.F => KeyCode.F,
        Keys.G => KeyCode.G,
        Keys.H => KeyCode.H,
        Keys.I => KeyCode.I,
        Keys.J => KeyCode.J,
        Keys.K => KeyCode.K,
        Keys.L => KeyCode.L,
        Keys.M => KeyCode.M,
        Keys.N => KeyCode.N,
        Keys.O => KeyCode.O,
        Keys.P => KeyCode.P,
        Keys.Q => KeyCode.Q,
        Keys.R => KeyCode.R,
        Keys.S => KeyCode.S,
        Keys.T => KeyCode.T,
        Keys.U => KeyCode.U,
        Keys.V => KeyCode.V,
        Keys.W => KeyCode.W,
        Keys.X => KeyCode.X,
        Keys.Y => KeyCode.Y,
        Keys.Z => KeyCode.Z,
        Keys.LeftBracket => KeyCode.LBracket,
        Keys.Backslash => KeyCode.Backslash,
        Keys.RightBracket => KeyCode.RBracket,
        Keys.GraveAccent => KeyCode.Tilde,
        Keys.Escape => KeyCode.Escape,
        Keys.Enter => KeyCode.Enter,
        Keys.Tab => KeyCode.Tab,
        Keys.Backspace => KeyCode.Backspace,
        Keys.Insert => KeyCode.Insert,
        Keys.Delete => KeyCode.Delete,
        Keys.Right => KeyCode.Right,
        Keys.Left => KeyCode.Left,
        Keys.Up => KeyCode.Up,
        Keys.PageUp => KeyCode.PageUp,
        Keys.PageDown => KeyCode.PageDown,
        Keys.Home => KeyCode.Home,
        Keys.End => KeyCode.End,
        Keys.Pause => KeyCode.Pause,
        Keys.F1 => KeyCode.F1,
        Keys.F2 => KeyCode.F2,
        Keys.F3 => KeyCode.F3,
        Keys.F4 => KeyCode.F4,
        Keys.F5 => KeyCode.F5,
        Keys.F6 => KeyCode.F6,
        Keys.F7 => KeyCode.F7,
        Keys.F8 => KeyCode.F8,
        Keys.F9 => KeyCode.F9,
        Keys.F10 => KeyCode.F10,
        Keys.F11 => KeyCode.F11,
        Keys.F12 => KeyCode.F12,
        Keys.F13 => KeyCode.F13,
        Keys.F14 => KeyCode.F14,
        Keys.F15 => KeyCode.F15,
        Keys.KeyPad0 => KeyCode.Numpad0,
        Keys.KeyPad1 => KeyCode.Numpad1,
        Keys.KeyPad2 => KeyCode.Numpad2,
        Keys.KeyPad3 => KeyCode.Numpad3,
        Keys.KeyPad4 => KeyCode.Numpad4,
        Keys.KeyPad5 => KeyCode.Numpad5,
        Keys.KeyPad6 => KeyCode.Numpad6,
        Keys.KeyPad7 => KeyCode.Numpad7,
        Keys.KeyPad8 => KeyCode.Numpad8,
        Keys.KeyPad9 => KeyCode.Numpad9,
        Keys.KeyPadDecimal => KeyCode.Period,
        Keys.KeyPadDivide => KeyCode.Slash,
        Keys.KeyPadMultiply => KeyCode.Multiply,
        Keys.KeyPadSubtract => KeyCode.Subtract,
        Keys.KeyPadAdd => KeyCode.Add,
        Keys.KeyPadEnter => KeyCode.Enter,
        Keys.KeyPadEqual => KeyCode.Equal,
        Keys.LeftShift => KeyCode.LShift,
        Keys.LeftControl => KeyCode.LControl,
        Keys.LeftAlt => KeyCode.LAlt,
        Keys.LeftSuper => KeyCode.LSystem,
        Keys.RightShift => KeyCode.RShift,
        Keys.RightControl => KeyCode.RControl,
        Keys.RightAlt => KeyCode.RAlt,
        Keys.RightSuper => KeyCode.RSystem,
        Keys.Menu => KeyCode.Menu,
        _ => KeyCode.Unknown
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Keys ToOtkKey(KeyCode key) => key switch {
        KeyCode.Space => Keys.Space,
        KeyCode.Quote => Keys.Apostrophe,
        KeyCode.Comma => Keys.Comma,
        KeyCode.Hyphen => Keys.Minus,
        KeyCode.Period => Keys.Period,
        KeyCode.Slash => Keys.Slash,
        KeyCode.Num0 => Keys.D0,
        KeyCode.Num1 => Keys.D1,
        KeyCode.Num2 => Keys.D2,
        KeyCode.Num3 => Keys.D3,
        KeyCode.Num4 => Keys.D4,
        KeyCode.Num5 => Keys.D5,
        KeyCode.Num6 => Keys.D6,
        KeyCode.Num7 => Keys.D7,
        KeyCode.Num8 => Keys.D8,
        KeyCode.Num9 => Keys.D9,
        KeyCode.Semicolon => Keys.Semicolon,
        KeyCode.Equal => Keys.Equal,
        KeyCode.A => Keys.A,
        KeyCode.B => Keys.B,
        KeyCode.C => Keys.C,
        KeyCode.D => Keys.D,
        KeyCode.E => Keys.E,
        KeyCode.F => Keys.F,
        KeyCode.G => Keys.G,
        KeyCode.H => Keys.H,
        KeyCode.I => Keys.I,
        KeyCode.J => Keys.J,
        KeyCode.K => Keys.K,
        KeyCode.L => Keys.L,
        KeyCode.M => Keys.M,
        KeyCode.N => Keys.N,
        KeyCode.O => Keys.O,
        KeyCode.P => Keys.P,
        KeyCode.Q => Keys.Q,
        KeyCode.R => Keys.R,
        KeyCode.S => Keys.S,
        KeyCode.T => Keys.T,
        KeyCode.U => Keys.U,
        KeyCode.V => Keys.V,
        KeyCode.W => Keys.W,
        KeyCode.X => Keys.X,
        KeyCode.Y => Keys.Y,
        KeyCode.Z => Keys.Z,
        KeyCode.LBracket => Keys.LeftBracket,
        KeyCode.Backslash => Keys.Backslash,
        KeyCode.RBracket => Keys.RightBracket,
        KeyCode.Tilde => Keys.GraveAccent,
        KeyCode.Escape => Keys.Escape,
        KeyCode.Enter => Keys.Enter,
        KeyCode.Tab => Keys.Tab,
        KeyCode.Backspace => Keys.Backspace,
        KeyCode.Insert => Keys.Insert,
        KeyCode.Delete => Keys.Delete,
        KeyCode.Right => Keys.Right,
        KeyCode.Left => Keys.Left,
        KeyCode.Up => Keys.Up,
        KeyCode.PageUp => Keys.PageUp,
        KeyCode.PageDown => Keys.PageDown,
        KeyCode.Home => Keys.Home,
        KeyCode.End => Keys.End,
        KeyCode.Pause => Keys.Pause,
        KeyCode.F1 => Keys.F1,
        KeyCode.F2 => Keys.F2,
        KeyCode.F3 => Keys.F3,
        KeyCode.F4 => Keys.F4,
        KeyCode.F5 => Keys.F5,
        KeyCode.F6 => Keys.F6,
        KeyCode.F7 => Keys.F7,
        KeyCode.F8 => Keys.F8,
        KeyCode.F9 => Keys.F9,
        KeyCode.F10 => Keys.F10,
        KeyCode.F11 => Keys.F11,
        KeyCode.F12 => Keys.F12,
        KeyCode.F13 => Keys.F13,
        KeyCode.F14 => Keys.F14,
        KeyCode.F15 => Keys.F15,
        KeyCode.Numpad0 => Keys.KeyPad0,
        KeyCode.Numpad1 => Keys.KeyPad1,
        KeyCode.Numpad2 => Keys.KeyPad2,
        KeyCode.Numpad3 => Keys.KeyPad3,
        KeyCode.Numpad4 => Keys.KeyPad4,
        KeyCode.Numpad5 => Keys.KeyPad5,
        KeyCode.Numpad6 => Keys.KeyPad6,
        KeyCode.Numpad7 => Keys.KeyPad7,
        KeyCode.Numpad8 => Keys.KeyPad8,
        KeyCode.Numpad9 => Keys.KeyPad9,
        KeyCode.LShift => Keys.LeftShift,
        KeyCode.LControl => Keys.LeftControl,
        KeyCode.LAlt => Keys.LeftAlt,
        KeyCode.LSystem => Keys.LeftSuper,
        KeyCode.RShift => Keys.RightShift,
        KeyCode.RControl => Keys.RightControl,
        KeyCode.RAlt => Keys.RightAlt,
        KeyCode.RSystem => Keys.RightSuper,
        KeyCode.Menu => Keys.Menu,
        _ => Keys.Unknown
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static MouseButton ToPerMouseButton(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton button) =>
        (MouseButton)button;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static OpenTK.Windowing.GraphicsLibraryFramework.MouseButton ToOtkMouseButton(MouseButton button) =>
        (OpenTK.Windowing.GraphicsLibraryFramework.MouseButton)button;
}
