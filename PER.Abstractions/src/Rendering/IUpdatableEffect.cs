using JetBrains.Annotations;

namespace PER.Abstractions.Rendering;

[PublicAPI]
public interface IUpdatableEffect : IEffect {
    public void Update();
}
