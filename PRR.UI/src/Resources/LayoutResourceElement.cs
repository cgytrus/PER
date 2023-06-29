using JetBrains.Annotations;

using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.UI;
using PER.Util;

namespace PRR.UI.Resources;

[PublicAPI]
public abstract record LayoutResourceElement(bool? enabled, Vector2Int position, Vector2Int size) {
    public abstract Element GetElement(LayoutResource resource, IRenderer renderer, IInput input, IAudio audio,
        Dictionary<string, Color> colors, string layoutName, string id);
}
