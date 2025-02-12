using System;

namespace PER.Abstractions.Input;

[Flags]
public enum ModifierKey {
    None = 0,
    Cmd = 0b0001,
    Shift = 0b0010,
    Ctrl = 0b0100,
    Alt = 0b1000
}
