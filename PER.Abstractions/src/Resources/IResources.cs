using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;
using PER.Abstractions.Meta;

namespace PER.Abstractions.Resources;

[PublicAPI, RequiresBody]
public interface IResources {
    public static abstract int currentVersion { get; }

    public IReadOnlyList<ResourcePackData> loadedPacks { get; }

    public void Load();
    public void Unload();
    public void SoftReload();

    public IEnumerable<ResourcePackData> GetAvailablePacks();
    public IEnumerable<ResourcePackData> GetUnloadedAvailablePacks();
    public bool TryGetPackData(string pack, out ResourcePackData data);

    public bool TryAddPack(ResourcePackData data);
    public bool TryAddPacksByNames(params string[] names);
    public bool TryRemovePack(ResourcePackData data);
    public void RemoveAllPacks();

    public void Preload<T>() where T : struct, IResource<T>;
    public T LazyLoad<T>() where T : struct, IResource<T>;

    public IEnumerable<string> GetAllPaths(string relativePath);
}
