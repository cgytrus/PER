﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;
using PER.Abstractions.Meta;

namespace PER.Abstractions.Resources;

[PublicAPI, RequiresBody]
public interface IResources {
    public static IResources? current { get; protected set; }

    public int currentVersion { get; }
    public bool loaded { get; }
    public bool loading { get; }
    public IReadOnlyList<ResourcePackData> loadedPacks { get; }
    public string defaultPackName { get; }

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

    public bool TryAddResource<TResource>(string id, TResource resource) where TResource : IResource;

    public IEnumerable<string> GetAllPaths(string relativePath);
    public bool TryGetResource<TResource>(string id, [NotNullWhen(true)] out TResource? resource)
        where TResource : class, IResource;
}
