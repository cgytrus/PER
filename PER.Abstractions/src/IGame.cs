using JetBrains.Annotations;

namespace PER.Abstractions;

[PublicAPI]
public interface IGame {
    public void Unload();
    public void Load();
    public void Loaded();
    public void Finish();
}
