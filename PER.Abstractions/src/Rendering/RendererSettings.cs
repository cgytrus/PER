using JetBrains.Annotations;

namespace PER.Abstractions.Rendering;

[PublicAPI]
public readonly struct RendererSettings {
    public bool fullscreen { get; init; }
    public IFont font { get; init; }
    public Image? icon { get; init; }
}
