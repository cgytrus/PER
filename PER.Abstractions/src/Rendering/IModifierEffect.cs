using JetBrains.Annotations;
using PER.Abstractions.Meta;
using PER.Util;

namespace PER.Abstractions.Rendering;

[PublicAPI]
public interface IModifierEffect : IEffect {
    [RequiresHead]
    public void ApplyModifiers(Vector2Int at, ref Vector2Int offset, ref RenderCharacter character);
}
