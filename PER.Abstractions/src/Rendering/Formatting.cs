using JetBrains.Annotations;

using PER.Util;

namespace PER.Abstractions.Rendering;

[PublicAPI]
public readonly record struct Formatting(Color foregroundColor, Color backgroundColor,
    RenderStyle style = RenderStyle.None, IEffect? effect = null);
