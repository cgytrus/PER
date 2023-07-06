using PER.Util;

namespace PER.Abstractions.Environment;

public interface ILight {
    public Color3 color { get; }
    public byte emission { get; }
    public byte reveal => 0;
}
