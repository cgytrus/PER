using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;
using PER.Abstractions.Meta;

namespace PER.Abstractions.Resources;

[PublicAPI, RequiresBody]
public interface IResources {
    public static abstract int currentVersion { get; }

    public bool needsReload { get; }

    public IReadOnlyList<ResourcePack> loadedPacks { get; }

    public IEnumerable<ResourcePack> GetAvailablePacks();
    public IEnumerable<ResourcePack> GetUnloadedAvailablePacks();
    public bool TryGetPackData(string pack, out ResourcePack data);

    public void AddPack(ResourcePack data);
    public void AddPacksByNames(params string[] names);
    public void RemovePack(ResourcePack data);
    public void RemoveAllPacks();

    public void Load();

    public void Preload<T>() where T : struct, IResource<T>;
    public T LazyLoad<T>() where T : struct, IResource<T>;
}
