namespace PER.Abstractions.Environment;

public interface ILight {
    public float brightness { get; }
    public byte emission { get; }
    public byte visibility => 0;
}
