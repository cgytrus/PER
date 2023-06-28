using JetBrains.Annotations;

using PER.Util;

namespace PER.Abstractions.Rendering;

[PublicAPI]
public readonly record struct RenderCharacter(char character, Color background, Color foreground,
    RenderStyle style = RenderStyle.None);
