using JetBrains.Annotations;

namespace PER.Abstractions.Environment;

[PublicAPI]
public interface IRemovable {
    public void Removed();
}
