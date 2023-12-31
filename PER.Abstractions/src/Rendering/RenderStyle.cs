﻿using System;

using JetBrains.Annotations;

namespace PER.Abstractions.Rendering;

[PublicAPI]
[Flags]
public enum RenderStyle : byte {
    None = 0,
    All = 0b1111,
    Bold = 0b1,
    Underline = 0b10,
    Strikethrough = 0b100,
    Italic = 0b1000
}
