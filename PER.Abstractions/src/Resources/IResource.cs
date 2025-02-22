namespace PER.Abstractions.Resources;

public interface IResource;
public interface IResource<TSelf> : IResource where TSelf : struct, IResource<TSelf> {
    public static abstract string filePath { get; }
    public static abstract TSelf Load(string path);
    public static abstract TSelf Merge(TSelf bottom, TSelf top);
    public static abstract TSelf Missing();
}
