using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

using JetBrains.Annotations;
using PER.Abstractions.Meta;

namespace PER.Abstractions.Resources;

public interface IResource {
    public int GetPathsHash();
    public void Preload();
    public void Load(string id);
    public void Unload(string id);
    public void PostUnload();
    public bool HasDependency(string id);
}

[RequiresBody]
public abstract class BodyResource : Resource, IResource {
    [RequiresBody]
    public abstract void Preload();
    [RequiresBody]
    public abstract void Load(string id);
    [RequiresBody]
    public abstract void Unload(string id);
}

[RequiresHead]
public abstract class HeadResource : Resource, IResource {
    [RequiresHead]
    public abstract void Preload();
    [RequiresHead]
    public abstract void Load(string id);
    [RequiresHead]
    public abstract void Unload(string id);
}

[RequiresBody, RequiresHead]
public abstract class UniversalResource : Resource, IResource {
    [RequiresBody, RequiresHead]
    public abstract void Preload();
    [RequiresBody, RequiresHead]
    public abstract void Load(string id);
    [RequiresBody, RequiresHead]
    public abstract void Unload(string id);
}

[PublicAPI]
public abstract class Resource {
    private Dictionary<string, IResource> _dependencies = new();
    private Dictionary<string, IEnumerable<string>> _fullPaths = new();

    public int GetPathsHash() {
        StringBuilder builder = new();
        foreach((_, IEnumerable<string> paths) in _fullPaths)
            foreach(string path in paths)
                builder.Append(path);
        return builder.ToString().GetHashCode();
    }

    public void PostUnload() {
        _dependencies.Clear();
        _fullPaths.Clear();
    }

    public bool HasDependency(string id) => _dependencies.ContainsKey(id);

    protected void AddDependency<T>(string id) {
        if(IResources.current is null || !IResources.current.loading)
            throw new InvalidOperationException("Cannot add dependencies while resources are not loading");
        if(_dependencies.ContainsKey(id))
            throw new InvalidOperationException($"Dependency {id} already registered.");
        if(!IResources.current.TryGetResource(id, out IResource? dependency))
            throw new InvalidOperationException($"Resource {id} does not exist.");
        if(dependency is not T)
            throw new InvalidOperationException($"Resource {id} is not {typeof(T).Name}.");
        _dependencies.Add(id, dependency);
    }

    protected void AddPath(string id, string path) {
        if(IResources.current is null || !IResources.current.loading)
            throw new InvalidOperationException("Cannot add paths while resources are not loading");
        IEnumerable<string> newPaths = IResources.current.GetAllPaths(Path.Combine(path.Split('/')));
        if(_fullPaths.TryGetValue(id, out IEnumerable<string>? currentPaths))
            _fullPaths[id] = newPaths.Concat(currentPaths);
        else
            _fullPaths.Add(id, newPaths);
    }

    protected IResource GetDependency(string id) {
        if(!_dependencies.TryGetValue(id, out IResource? dependency))
            throw new InvalidOperationException($"Resource {id} is not registered as a dependency.");
        return dependency;
    }

    protected T GetDependency<T>(string id) where T : IResource {
        IResource dependency = GetDependency(id);
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
