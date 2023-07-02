using System.Numerics;

using JetBrains.Annotations;

using PER.Util;

namespace PER.Abstractions.Rendering;

[PublicAPI]
public interface IModifierEffect : IDisplayEffect {
    public void ApplyModifiers(Vector2Int at, ref Vector2 position, ref RenderCharacter character);
}
