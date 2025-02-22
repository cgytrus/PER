﻿using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using JetBrains.Annotations;

using NLog;

using PER.Abstractions.Resources;

namespace PER.Common.Resources;

[PublicAPI]
public class Resources : IResources {
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    public int currentVersion => 0;
    public bool loaded { get; private set; }
    public bool loading { get; private set; }
    public IReadOnlyList<ResourcePackData> loadedPacks => _loadedPacks;
    public virtual string defaultPackName => "Default";
    protected virtual string resourcesRoot => "resources";
    protected virtual string resourcePackMeta => "metadata.json";
    protected virtual string resourcesInPack => "resources";

    private readonly List<ResourcePackData> _loadedPacks = [];
    private readonly List<ResourcePackData> _futureLoadedPacks = [];
    private readonly Dictionary<string, IResource> _resources = new();
    private readonly Dictionary<string, int> _resourcePathHashes = new();

    public void Load() {
        if(loading)
            throw new InvalidOperationException("Already loading.");
        if(loaded)
            return;
        loading = true;

        logger.Info("Loading resources");

        _loadedPacks.Clear();
        _loadedPacks.AddRange(_futureLoadedPacks);

        foreach((string id, IResource resource) in _resources) {
            logger.Info("Loading resource {Id}", id);
            IResources.current = this;
            resource.Preload();
            IResources.current = null;
            resource.Load(id);
            _resourcePathHashes.Add(id, resource.GetPathsHash());
        }

        loading = false;
        loaded = true;

        logger.Info("Resources loaded");
    }

    public void Unload() {
        if(!loaded)
            return;

        logger.Info("Unloading resources");

        foreach((string id, IResource resource) in _resources) {
            logger.Info("Unloading resource {Id}", id);
            resource.Unload(id);
            resource.PostUnload();
        }

        _loadedPacks.Clear();
        _futureLoadedPacks.Clear();
        _resources.Clear();
        _resourcePathHashes.Clear();

        loaded = false;

        logger.Info("Resources unloaded");
    }

    public void SoftReload() {
        if(loading)
            throw new InvalidOperationException("Already loading.");
        if(!loaded)
            return;
        loading = true;

        logger.Info("Reloading resources");

        _loadedPacks.Clear();
        _loadedPacks.AddRange(_futureLoadedPacks);

        logger.Info("Searching for changed resources");

        HashSet<(string, IResource, int)> resourcesToNotReloadYet = new();
        Dictionary<string, int> resourcesToReload = new();

        foreach((string id, IResource resource) in _resources) {
            int newHash = resource.GetPathsHash();
            if(_resourcePathHashes.TryGetValue(id, out int prevHash) && prevHash == newHash) {
                resourcesToNotReloadYet.Add((id, resource, newHash));
                continue;
            }
            resourcesToReload.Add(id, newHash);
        }

        FindIndirectResourcesToReload(resourcesToNotReloadYet, resourcesToReload);

        foreach((string id, IResource resource) in _resources) {
            if(!resourcesToReload.TryGetValue(id, out int hash))
                continue;

            logger.Info("Reloading resource {Id}", id);
            resource.Unload(id);
            resource.PostUnload();
            IResources.current = this;
            resource.Preload();
            IResources.current = null;
            resource.Load(id);
            _resourcePathHashes[id] = hash;
        }

        loading = false;

        logger.Info("Resources reloaded");
    }

    private static void FindIndirectResourcesToReload(HashSet<(string, IResource, int)> resourcesToNotReloadYet,
        Dictionary<string, int> resourcesToReload) {
        foreach((string id, IResource resource, int hash) in resourcesToNotReloadYet) {
            bool reload = false;
            foreach((string reloadId, _) in resourcesToReload) {
                if(!resource.HasDependency(reloadId))
                    continue;
                reload = true;
                break;
            }
            if(reload)
                resourcesToReload.Add(id, hash);
        }
    }

    public IEnumerable<ResourcePackData> GetAvailablePacks() {
        if(!Directory.Exists(resourcesRoot)) {
            logger.Warn("Resources directory ({Directory}) missing", Path.GetFullPath(resourcesRoot));
            yield break;
        }

        foreach(string pack in Directory.GetDirectories(resourcesRoot)) {
            if(!TryGetPackData(pack, out ResourcePackData data) || data.meta.version != currentVersion)
                continue;
            yield return data;
        }
    }

    public IEnumerable<ResourcePackData> GetUnloadedAvailablePacks() {
        IEnumerable<string> loadedPackNames = loadedPacks.Select(packData => packData.name);
        return GetAvailablePacks().Where(data => !loadedPackNames.Contains(data.name));
    }

    public bool TryGetPackData(string pack, out ResourcePackData data) {
        string metaPath = Path.Combine(pack, resourcePackMeta);
        data = default(ResourcePackData);
        if(!File.Exists(metaPath))
            return false;

        FileStream file = File.OpenRead(metaPath);
        ResourcePackMeta meta = JsonSerializer.Deserialize<ResourcePackMeta>(file);
        file.Close();

        data = new ResourcePackData(Path.GetFileName(pack), Path.Combine(pack, resourcesInPack), meta);
        return true;
    }

    public bool TryAddPack(ResourcePackData data) {
        if(loading)
            return false;
        _futureLoadedPacks.Add(data);
        logger.Info("Enabled pack {Name}", data.name);
        return true;
    }

    public bool TryAddPacksByNames(params string[] names) {
        bool success = true;
        ImmutableDictionary<string, ResourcePackData> availablePacks =
            GetAvailablePacks().ToImmutableDictionary(data => data.name);
        foreach(string name in names) {
            if(!availablePacks.TryGetValue(name, out ResourcePackData data))
                continue;
            success &= TryAddPack(data);
        }
        return success;
    }

    public bool TryRemovePack(ResourcePackData data) {
        if(loading || !_futureLoadedPacks.Remove(data))
            return false;
        logger.Info("Disabled pack {Name}", data.name);
        return true;
    }

    public void RemoveAllPacks() => _futureLoadedPacks.Clear();

    public bool TryAddResource<TResource>(string id, TResource resource) where TResource : IResource =>
        !loaded && _resources.TryAdd(id, resource);

    public IEnumerable<string> GetAllPaths(string relativePath) {
        if(!loaded && !loading)
            yield break;

        for(int i = loadedPacks.Count - 1; i >= 0; i--) {
            string resourcePath = Path.Combine(loadedPacks[i].fullPath, relativePath);
            if(File.Exists(resourcePath))
                yield return resourcePath;
        }
    }

    public bool TryGetResource<TResource>(string id, [NotNullWhen(true)] out TResource? resource)
        where TResource : class, IResource {
        resource = null;
        if(!_resources.TryGetValue(id, out IResource? cachedResource) ||
           cachedResource is not TResource actualResource)
            return false;

        resource = actualResource;
        return true;
    }
}
