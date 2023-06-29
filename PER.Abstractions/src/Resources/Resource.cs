using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

using JetBrains.Annotations;

namespace PER.Abstractions.Resources;

[PublicAPI]
public abstract class Resource {
    private Dictionary<string, Resource> _dependencies = new();
    private Dictionary<string, IEnumerable<string>> _fullPaths = new();

    public int GetPathsHash() {
        StringBuilder builder = new();
        foreach((_, IEnumerable<string> paths) in _fullPaths)
            foreach(string path in paths)
                builder.Append(path);
        return builder.ToString().GetHashCode();
    }

    public abstract void Preload(IResources resources);
    public abstract void Load(string id);
    public abstract void Unload(string id);

    public void PostUnload() {
        _dependencies.Clear();
        _fullPaths.Clear();
    }

    public bool HasDependency(string id) => _dependencies.ContainsKey(id);

    protected void AddDependency<T>(IResources resources, string id) {
        if(!resources.loading)
            throw new InvalidOperationException("Cannot add dependencies while resources are not loading");
        if(_dependencies.ContainsKey(id))
            throw new InvalidOperationException($"Dependency {id} already registered.");
        if(!resources.TryGetResource(id, out Resource? dependency))
            throw new InvalidOperationException($"Resource {id} does not exist.");
        if(dependency is not T)
            throw new InvalidOperationException($"Resource {id} is not {typeof(T).Name}.");
        _dependencies.Add(id, dependency);
    }

    protected void AddPath(IResources resources, string id, string path) {
        if(!resources.loading)
            throw new InvalidOperationException("Cannot add paths while resources are not loading");
        if(_fullPaths.ContainsKey(id))
            throw new InvalidOperationException($"File with ID {id} already registered.");
        _fullPaths.Add(id, resources.GetAllPaths(Path.Combine(path.Split('/'))));
    }

    protected Resource GetDependency(string id) {
        if(!_dependencies.TryGetValue(id, out Resource? dependency))
            throw new InvalidOperationException($"Resource {id} is not registered as a dependency.");
        return dependency;
    }

    protected T GetDependency<T>(string id) where T : Resource {
        Resource dependency = GetDependency(id);
        if(dependency is not T typedDependency)
            throw new InvalidOperationException($"Resource {id} is not {nameof(T)}.");

        return typedDependency;
    }

    protected bool TryGetPaths(string id, [NotNullWhen(true)] out IEnumerable<string>? fullPaths) =>
        _fullPaths.TryGetValue(id, out fullPaths);

    protected IEnumerable<string> GetPaths(string id) {
        if(!TryGetPaths(id, out IEnumerable<string>? fullPaths))
            throw new InvalidOperationException($"File with ID {id} is not registered.");
        return fullPaths;
    }

    protected bool TryGetPath(string id, [NotNullWhen(true)] out string? fullPath) {
        if(!TryGetPaths(id, out IEnumerable<string>? fullPaths)) {
            fullPath = null;
            return false;
        }
        fullPath = fullPaths.FirstOrDefault((string?)null);
        return fullPath is not null;
    }

    protected string GetPath(string id) {
        if(!TryGetPath(id, out string? fullPath))
            throw new FileNotFoundException(null, id);
        return fullPath;
    }
}
