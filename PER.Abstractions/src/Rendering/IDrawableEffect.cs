using JetBrains.Annotations;

using PER.Util;

namespace PER.Abstractions.Rendering;

[PublicAPI]
public interface IDrawableEffect : IEffect {
    public void Draw(Vector2Int position);
}
