namespace PER.Abstractions.Resources;

public interface IResource<TSelf> where TSelf : struct, IResource<TSelf> {
    public static abstract string filePath { get; }
    public static abstract TSelf Load(string path);
    public static abstract TSelf Merge(TSelf top, TSelf bottom);
}
