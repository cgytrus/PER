using JetBrains.Annotations;

using PER.Abstractions.Rendering;

namespace PER.Abstractions;

[PublicAPI]
public interface IGame {
    public void Unload();
    public void Load();
    public RendererSettings Loaded();
    public void Finish();
}
