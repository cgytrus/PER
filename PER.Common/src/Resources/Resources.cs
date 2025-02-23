using System.Collections.Immutable;
using System.Text.Json;

using JetBrains.Annotations;

using NLog;

using PER.Abstractions.Resources;

namespace PER.Common.Resources;

[PublicAPI]
public class Resources : IResources {
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    public static int currentVersion => 0;

    public bool needsReload { get; private set; }

    protected virtual string resourcesRoot => "resources";
    protected virtual string resourcePackMeta => "metadata.json";
    protected virtual string resourcesInPack => "resources";

    private readonly List<ResourcePack> _loadedPacks = [];
    private readonly List<ResourcePack> _queuedPacks = [];
    private readonly Dictionary<Type, IResource> _resources = [];

    public IReadOnlyList<ResourcePack> loadedPacks => _loadedPacks;

    public IEnumerable<ResourcePack> GetAvailablePacks() {
        if (!Directory.Exists(resourcesRoot)) {
            logger.Warn("Resources directory ({}) missing", Path.GetFullPath(resourcesRoot));
            yield break;
        }

        foreach (string pack in Directory.GetDirectories(resourcesRoot)) {
            if (!TryGetPackData(pack, out ResourcePack data) || data.meta.version != currentVersion)
                continue;
            yield return data;
        }
    }

    public IEnumerable<ResourcePack> GetUnloadedAvailablePacks() {
        IEnumerable<string> loadedPackNames = loadedPacks.Select(packData => packData.name);
        return GetAvailablePacks().Where(data => !loadedPackNames.Contains(data.name));
    }

    public bool TryGetPackData(string pack, out ResourcePack data) {
        string metaPath = Path.Combine(pack, resourcePackMeta);
        data = default(ResourcePack);
        if (!File.Exists(metaPath))
            return false;

        FileStream file = File.OpenRead(metaPath);
        ResourcePack.Meta meta = JsonSerializer.Deserialize<ResourcePack.Meta>(file);
        file.Close();

        data = new ResourcePack(Path.GetFileName(pack), Path.Combine(pack, resourcesInPack), meta);
        return true;
    }

    public void AddPack(ResourcePack data) {
        _queuedPacks.Add(data);
        UpdateNeedsReload();
        logger.Info("Queued pack {}", data.name);
    }

    public void AddPacksByNames(params string[] names) {
        ImmutableDictionary<string, ResourcePack> availablePacks =
            GetAvailablePacks().ToImmutableDictionary(data => data.name);
        foreach (string name in names) {
            if (availablePacks.TryGetValue(name, out ResourcePack data))
                AddPack(data);
        }
    }

    public void RemovePack(ResourcePack data) {
        if (!_queuedPacks.Remove(data))
            return;
        UpdateNeedsReload();
        logger.Info("Unqueued pack {}", data.name);
    }

    public void RemoveAllPacks() {
        _queuedPacks.Clear();
        UpdateNeedsReload();
        logger.Info("Unqueued all packs");
    }

    private void UpdateNeedsReload() => needsReload = _loadedPacks.Count != _queuedPacks.Count ||
        _loadedPacks.Where((t, i) => t != _queuedPacks[i]).Any();

    public void Load() {
        _resources.Clear();
        _loadedPacks.Clear();
        _loadedPacks.AddRange(_queuedPacks);
    }

    public T LazyLoad<T>() where T : struct, IResource<T> {
        if (_resources.TryGetValue(typeof(T), out IResource? resource))
            return (T)resource;
        logger.Info("Loading resource {}", typeof(T).FullName);
        if (Path.IsPathRooted(T.filePath))
            throw new InvalidOperationException($"{nameof(T.filePath)} cannot be rooted.");
        T? res = null;
        string? topPath = null;
        foreach (ResourcePack pack in loadedPacks) {
            string resourcePath = Path.Combine(pack.fullPath, T.filePath);
            if (!resourcePath.StartsWith(pack.fullPath))
                throw new InvalidOperationException($"{nameof(T.filePath)} cannot escape the pack directory.");
            if (!File.Exists(resourcePath))
                continue;
            topPath = resourcePath;
            if (!T.alwaysTop)
                res = res.HasValue ? T.Merge(res.Value, T.Load(topPath)) : T.Load(topPath);
        }
        res ??= topPath is null ? T.Missing() : T.Load(topPath);
        _resources[typeof(T)] = res;
        return res.Value;
    }
}
