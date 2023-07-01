using JetBrains.Annotations;

namespace PER.Abstractions.Environment;

[PublicAPI]
public interface IMovable {
    public void Moved();
}
