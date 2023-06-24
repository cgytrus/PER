using System;
using System.Runtime.CompilerServices;

using PER.Abstractions.Input;

using Color = PER.Util.Color;

namespace PRR.Console;

public static class ConsoleConverters {
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Color ToPerColor(ConsoleColor color) {
        return color switch {
            ConsoleColor.Black => Color.black,
            ConsoleColor.DarkBlue => new Color(0f, 0f, 0.545f, 1f),
            ConsoleColor.DarkGreen => new Color(0f, 0.4f, 0f, 1f),
            ConsoleColor.DarkCyan => new Color(0f, 0.545f, 0.545f, 1f),
            ConsoleColor.DarkRed => new Color(0.545f, 0f, 0f, 1f),
            ConsoleColor.DarkMagenta => new Color(0.545f, 0f, 0.545f, 1f),
            ConsoleColor.DarkYellow => new Color(0.545f, 0.545f, 0f, 1f),

            // wtf
            ConsoleColor.Gray => new Color(0.5f, 0.5f, 0.5f, 1f),
            ConsoleColor.DarkGray => new Color(0.66f, 0.66f, 0.66f, 1f),

            ConsoleColor.Blue => new Color(0f, 0f, 1f, 1f),
            ConsoleColor.Green => new Color(0f, 1f, 0f, 1f),
            ConsoleColor.Cyan => new Color(0f, 1f, 1f, 1f),
            ConsoleColor.Red => new Color(1f, 0f, 0f, 1f),
            ConsoleColor.Magenta => new Color(1f, 0f, 1f, 1f),
            ConsoleColor.Yellow => new Color(1f, 1f, 0f, 1f),
            ConsoleColor.White => Color.white,
            _ => throw new ArgumentOutOfRangeException(nameof(color), color, "wtf")
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static ConsoleColor ToConsoleColor(Color color) {
        System.Drawing.Color c = System.Drawing.Color.FromArgb((int)Math.Clamp(color.a * 255f, 0f, 255f),
            (int)Math.Clamp(color.r * 255f, 0f, 255f), (int)Math.Clamp(color.g * 255f, 0f, 255f),
            (int)Math.Clamp(color.b * 255f, 0f, 255f));
        float h = c.GetHue();
        float s = c.GetSaturation();
        float l = c.GetBrightness();
        if(l <= 0.2f)
            return ConsoleColor.Black;
        if(l >= 0.8)
            return ConsoleColor.White;
        if(s <= 0.2f)
            return l < 0.5f ? ConsoleColor.Gray : ConsoleColor.DarkGray;
        ConsoleColor tempColor = h switch {
            <= 30f => ConsoleColor.Red,
            <= 90f => ConsoleColor.Yellow,
            <= 150f => ConsoleColor.Green,
            <= 210f => ConsoleColor.Cyan,
            <= 270f => ConsoleColor.Blue,
            <= 330 => ConsoleColor.Magenta,
            _ => ConsoleColor.Red
        };
        if(l > 0.3f)
            return tempColor;
        // roslyn is fucking stupid so if i fix this warning (switch is not exhaustive)
        // rider kicks in and tells me that any other cases are heuristically unreachable
#pragma warning disable CS8509
        return tempColor switch {
#pragma warning restore CS8509
            ConsoleColor.Red => ConsoleColor.DarkRed,
            ConsoleColor.Yellow => ConsoleColor.DarkYellow,
            ConsoleColor.Green => ConsoleColor.DarkGreen,
            ConsoleColor.Cyan => ConsoleColor.DarkCyan,
            ConsoleColor.Blue => ConsoleColor.DarkBlue,
            ConsoleColor.Magenta => ConsoleColor.DarkMagenta
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static KeyCode ToPerKey(ConsoleKey key) => key switch {
        ConsoleKey.Backspace => KeyCode.Backspace,
        ConsoleKey.Tab => KeyCode.Tab,
        ConsoleKey.Enter => KeyCode.Enter,
        ConsoleKey.Pause => KeyCode.Pause,
        ConsoleKey.Escape => KeyCode.Escape,
        ConsoleKey.Spacebar => KeyCode.Space,
        ConsoleKey.PageUp => KeyCode.PageUp,
        ConsoleKey.PageDown => KeyCode.PageDown,
        ConsoleKey.End => KeyCode.End,
        ConsoleKey.Home => KeyCode.Home,
        ConsoleKey.LeftArrow => KeyCode.Left,
        ConsoleKey.UpArrow => KeyCode.Up,
        ConsoleKey.RightArrow => KeyCode.Right,
        ConsoleKey.DownArrow => KeyCode.Down,
        ConsoleKey.Insert => KeyCode.Insert,
        ConsoleKey.Delete => KeyCode.Delete,
        ConsoleKey.D0 => KeyCode.Num0,
        ConsoleKey.D1 => KeyCode.Num1,
        ConsoleKey.D2 => KeyCode.Num2,
        ConsoleKey.D3 => KeyCode.Num3,
        ConsoleKey.D4 => KeyCode.Num4,
        ConsoleKey.D5 => KeyCode.Num5,
        ConsoleKey.D6 => KeyCode.Num6,
        ConsoleKey.D7 => KeyCode.Num7,
        ConsoleKey.D8 => KeyCode.Num8,
        ConsoleKey.D9 => KeyCode.Num9,
        ConsoleKey.A => KeyCode.A,
        ConsoleKey.B => KeyCode.B,
        ConsoleKey.C => KeyCode.C,
        ConsoleKey.D => KeyCode.D,
        ConsoleKey.E => KeyCode.E,
        ConsoleKey.F => KeyCode.F,
        ConsoleKey.G => KeyCode.G,
        ConsoleKey.H => KeyCode.H,
        ConsoleKey.I => KeyCode.I,
        ConsoleKey.J => KeyCode.J,
        ConsoleKey.K => KeyCode.K,
        ConsoleKey.L => KeyCode.L,
        ConsoleKey.M => KeyCode.M,
        ConsoleKey.N => KeyCode.N,
        ConsoleKey.O => KeyCode.O,
        ConsoleKey.P => KeyCode.P,
        ConsoleKey.Q => KeyCode.Q,
        ConsoleKey.R => KeyCode.R,
        ConsoleKey.S => KeyCode.S,
        ConsoleKey.T => KeyCode.T,
        ConsoleKey.U => KeyCode.U,
        ConsoleKey.V => KeyCode.V,
        ConsoleKey.W => KeyCode.W,
        ConsoleKey.X => KeyCode.X,
        ConsoleKey.Y => KeyCode.Y,
        ConsoleKey.Z => KeyCode.Z,
        ConsoleKey.LeftWindows => KeyCode.LSystem,
        ConsoleKey.RightWindows => KeyCode.RSystem,
        ConsoleKey.NumPad0 => KeyCode.Numpad0,
        ConsoleKey.NumPad1 => KeyCode.Numpad1,
        ConsoleKey.NumPad2 => KeyCode.Numpad2,
        ConsoleKey.NumPad3 => KeyCode.Numpad3,
        ConsoleKey.NumPad4 => KeyCode.Numpad4,
        ConsoleKey.NumPad5 => KeyCode.Numpad5,
        ConsoleKey.NumPad6 => KeyCode.Numpad6,
        ConsoleKey.NumPad7 => KeyCode.Numpad7,
        ConsoleKey.NumPad8 => KeyCode.Numpad8,
        ConsoleKey.NumPad9 => KeyCode.Numpad9,
        ConsoleKey.F1 => KeyCode.F1,
        ConsoleKey.F2 => KeyCode.F2,
        ConsoleKey.F3 => KeyCode.F3,
        ConsoleKey.F4 => KeyCode.F4,
        ConsoleKey.F5 => KeyCode.F5,
        ConsoleKey.F6 => KeyCode.F6,
        ConsoleKey.F7 => KeyCode.F7,
        ConsoleKey.F8 => KeyCode.F8,
        ConsoleKey.F9 => KeyCode.F9,
        ConsoleKey.F10 => KeyCode.F10,
        ConsoleKey.F11 => KeyCode.F11,
        ConsoleKey.F12 => KeyCode.F12,
        ConsoleKey.F13 => KeyCode.F13,
        ConsoleKey.F14 => KeyCode.F14,
        ConsoleKey.F15 => KeyCode.F15,
        _ => KeyCode.Unknown
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static ConsoleKey ToConsoleKey(KeyCode key) => key switch {
        KeyCode.Backspace => ConsoleKey.Backspace,
        KeyCode.Tab => ConsoleKey.Tab,
        KeyCode.Enter => ConsoleKey.Enter,
        KeyCode.Pause => ConsoleKey.Pause,
        KeyCode.Escape => ConsoleKey.Escape,
        KeyCode.Space => ConsoleKey.Spacebar,
        KeyCode.PageUp => ConsoleKey.PageUp,
        KeyCode.PageDown => ConsoleKey.PageDown,
        KeyCode.End => ConsoleKey.End,
        KeyCode.Home => ConsoleKey.Home,
        KeyCode.Left => ConsoleKey.LeftArrow,
        KeyCode.Up => ConsoleKey.UpArrow,
        KeyCode.Right => ConsoleKey.RightArrow,
        KeyCode.Down => ConsoleKey.DownArrow,
        KeyCode.Insert => ConsoleKey.Insert,
        KeyCode.Delete => ConsoleKey.Delete,
        KeyCode.Num0 => ConsoleKey.D0,
        KeyCode.Num1 => ConsoleKey.D1,
        KeyCode.Num2 => ConsoleKey.D2,
        KeyCode.Num3 => ConsoleKey.D3,
        KeyCode.Num4 => ConsoleKey.D4,
        KeyCode.Num5 => ConsoleKey.D5,
        KeyCode.Num6 => ConsoleKey.D6,
        KeyCode.Num7 => ConsoleKey.D7,
        KeyCode.Num8 => ConsoleKey.D8,
        KeyCode.Num9 => ConsoleKey.D9,
        KeyCode.A => ConsoleKey.A,
        KeyCode.B => ConsoleKey.B,
        KeyCode.C => ConsoleKey.C,
        KeyCode.D => ConsoleKey.D,
        KeyCode.E => ConsoleKey.E,
        KeyCode.F => ConsoleKey.F,
        KeyCode.G => ConsoleKey.G,
        KeyCode.H => ConsoleKey.H,
        KeyCode.I => ConsoleKey.I,
        KeyCode.J => ConsoleKey.J,
        KeyCode.K => ConsoleKey.K,
        KeyCode.L => ConsoleKey.L,
        KeyCode.M => ConsoleKey.M,
        KeyCode.N => ConsoleKey.N,
        KeyCode.O => ConsoleKey.O,
        KeyCode.P => ConsoleKey.P,
        KeyCode.Q => ConsoleKey.Q,
        KeyCode.R => ConsoleKey.R,
        KeyCode.S => ConsoleKey.S,
        KeyCode.T => ConsoleKey.T,
        KeyCode.U => ConsoleKey.U,
        KeyCode.V => ConsoleKey.V,
        KeyCode.W => ConsoleKey.W,
        KeyCode.X => ConsoleKey.X,
        KeyCode.Y => ConsoleKey.Y,
        KeyCode.Z => ConsoleKey.Z,
        KeyCode.LSystem => ConsoleKey.LeftWindows,
        KeyCode.RSystem => ConsoleKey.RightWindows,
        KeyCode.Numpad0 => ConsoleKey.NumPad0,
        KeyCode.Numpad1 => ConsoleKey.NumPad1,
        KeyCode.Numpad2 => ConsoleKey.NumPad2,
        KeyCode.Numpad3 => ConsoleKey.NumPad3,
        KeyCode.Numpad4 => ConsoleKey.NumPad4,
        KeyCode.Numpad5 => ConsoleKey.NumPad5,
        KeyCode.Numpad6 => ConsoleKey.NumPad6,
        KeyCode.Numpad7 => ConsoleKey.NumPad7,
        KeyCode.Numpad8 => ConsoleKey.NumPad8,
        KeyCode.Numpad9 => ConsoleKey.NumPad9,
        KeyCode.F1 => ConsoleKey.F1,
        KeyCode.F2 => ConsoleKey.F2,
        KeyCode.F3 => ConsoleKey.F3,
        KeyCode.F4 => ConsoleKey.F4,
        KeyCode.F5 => ConsoleKey.F5,
        KeyCode.F6 => ConsoleKey.F6,
        KeyCode.F7 => ConsoleKey.F7,
        KeyCode.F8 => ConsoleKey.F8,
        KeyCode.F9 => ConsoleKey.F9,
        KeyCode.F10 => ConsoleKey.F10,
        KeyCode.F11 => ConsoleKey.F11,
        KeyCode.F12 => ConsoleKey.F12,
        KeyCode.F13 => ConsoleKey.F13,
        KeyCode.F14 => ConsoleKey.F14,
        KeyCode.F15 => ConsoleKey.F15,
        _ => ConsoleKey.NoName // idk
    };

    //// ??
    //[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    //public static MouseButton ToPerMouseButton(Mouse.Button button) => (MouseButton)button;
    //[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    //public static Mouse.Button ToConsoleMouseButton(MouseButton button) => (Mouse.Button)button;
}
