using JetBrains.Annotations;
using PER.Abstractions.Meta;

namespace PER.Abstractions;

[PublicAPI, RequiresBody]
public interface IGame {
    public void PreLoad();
    public void Load();
    public void Unload();
    public void Finish();
}
