using PER.Abstractions.Meta;

namespace PER.Abstractions.Resources;

[RequiresBody]
public interface IResource;
public interface IResource<TSelf> : IResource where TSelf : struct, IResource<TSelf> {
    public static virtual bool alwaysTop => false;
    public static abstract string filePath { get; }
    public static abstract TSelf Load(string path);
    public static abstract TSelf Merge(TSelf bottom, TSelf top);
    public static abstract TSelf Missing();
}
public interface IResource<TSelf, out TValue> : IResource<TSelf> where TSelf : struct, IResource<TSelf, TValue> {
    public TValue value { get; }
}

public interface ISingleResource<TSelf, out TRet> : IResource<TSelf, TRet>
    where TSelf : struct, ISingleResource<TSelf, TRet> {
    static bool IResource<TSelf>.alwaysTop => true;
    // doesn't matter but just for completeness
    static TSelf IResource<TSelf>.Merge(TSelf bottom, TSelf top) => top;
}
