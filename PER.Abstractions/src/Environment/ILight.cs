namespace PER.Abstractions.Environment;

public interface ILight {
    public float brightness { get; }
    public byte emission { get; }
    public byte reveal => 0;
}
