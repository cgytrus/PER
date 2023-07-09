using JetBrains.Annotations;

namespace PER.Abstractions.Rendering;

[PublicAPI]
public readonly struct RendererSettings {
    public string title { get; init; }
    public int width { get; init; }
    public int height { get; init; }
    public bool verticalSync { get; init; }
    public bool fullscreen { get; init; }
    public IFont font { get; init; }
    public string? icon { get; init; }
}
