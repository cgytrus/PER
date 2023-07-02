using JetBrains.Annotations;

using PER.Util;

namespace PER.Abstractions.Environment;

[PublicAPI]
public interface IMovable {
    public void Moved(Vector2Int from);
}
