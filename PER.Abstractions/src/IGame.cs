using JetBrains.Annotations;
using PER.Abstractions.Meta;

namespace PER.Abstractions;

[PublicAPI, RequiresBody]
public interface IGame {
    public void Unload();
    public void Load();
    public void Loaded();
    public void Finish();
}
